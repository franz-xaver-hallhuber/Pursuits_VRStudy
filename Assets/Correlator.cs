using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;
using System.IO;

public class Correlator : MonoBehaviour {
    public class TimePoint
    {
        public DateTime timestamp { get; set; }
        public Vector3 pos { get; set; }

        public TimePoint(DateTime time, Vector3 currentPos)
        {
            this.timestamp = time;
            this.pos = currentPos;
        }
    }

    public class MovingObject
    {
        GameObject go;
        private StreamWriter positionWriter;
        private Vector3 _current;

        public string name { get; set; }
        public List<TimePoint> trajectory { get; set; }
        
        public MovingObject(GameObject go)
        {
            this.go = go;
            trajectory = new List<TimePoint>();
            if (go != null) name = go.name;
            else name = "gaze";
            positionWriter = new StreamWriter("log_" + name + "_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            positionWriter.WriteLine(name+"Timestamp;"+name+"XPos");
        }

        public string getName()
        {
            return go.name;
        }

        /// <summary>
        /// if the oldest timestamp was more than w ticks ago, remove the oldest
        /// </summary>
        void cleanUp()
        {
            //if (trajectory.Count > 120) trajectory.RemoveAt(0);
            if (trajectory.Count > 0)
            {
                if ((DateTime.Now - trajectory[0].timestamp).TotalMilliseconds > Correlator.w) trajectory.RemoveAt(0);
                if (trajectory.Count > 0)
                    if ((DateTime.Now - trajectory[0].timestamp).TotalMilliseconds > Correlator.w) cleanUp();
            }
            
        }

        /// <summary>
        /// Adds the current position of a MovingObject to its trajectory
        /// </summary>
        public void addNewPosition()
        {
            trajectory.Add(new TimePoint(DateTime.Now, _current));
            positionWriter.WriteLine((DateTime.Now - Correlator.startTime).TotalMilliseconds+ ";" + _current.x);
            cleanUp();
        }

        /// <summary>
        /// Interpolates between the last recorded position and the current position to determine the position at a given time
        /// </summary>
        /// <param name="atTime">the timestamp for which the position should be calulated</param>
        public void addNewPosition(DateTime atTime)
        {
            if (trajectory.Count > 0)
            {
                TimePoint _last = trajectory[trajectory.Count - 1];
                float scale = (float)((atTime - _last.timestamp).TotalMilliseconds / (DateTime.Now - _last.timestamp).TotalMilliseconds);
                trajectory.Add(new TimePoint(atTime, _current + Vector3.Scale(_current - _last.pos, new Vector3(scale, scale, scale))));
            } else
            {
                // in case trajectory is empty
                addNewPosition();
            }
        }

        /// <summary>
        /// to be called by the Update() method so that threads and coroutines can work with GameObject positions
        /// </summary>
        public void updatePosition()
        {
            _current = go.transform.localPosition;
        }

        public void addNewGaze(DateTime timeOfCapture, Vector3 gazePoint)
        {
            trajectory.Add(new TimePoint(timeOfCapture, gazePoint));
            positionWriter.WriteLine(timeOfCapture + ";" + gazePoint.x);
            cleanUp();
        }

        public List<double> getXPoints()
        {
            List<double> ret = new List<double>();
            foreach (TimePoint tp in trajectory) ret.Add(tp.pos.x);
            return ret;
        }

        public List<double> getYPoints()
        {
            List<double> ret = new List<double>();
            foreach (TimePoint tp in trajectory) ret.Add(tp.pos.y);
            return ret;
        }

        public void activate(bool active)
        {
            Renderer r = this.go.GetComponent<Renderer>();
            if (active) r.material.SetColor("_Color", Color.red);
            else r.material.SetColor("_Color", Color.gray);
        }

        public int length()
        {
            return trajectory.Count;
        }

        public void killMe()
        {
            positionWriter.Close();
        }
    }

    List<MovingObject> sceneObjects;
    MovingObject gazeTrajectory;
    public PupilGazeTracker.GazeSource Gaze;
    List<string> activeObjects;

    public static long w;
    public float correlationDurationMs;

    private volatile bool _shouldStop;
    
    private StreamWriter correlationWriter;

    private bool _calcInProgress;

    public static DateTime startTime;

    //public delegate void GazeAction();
    //public static event GazeAction OnNewGaze;

    // Use this for initialization
    void Start () {

        sceneObjects = new List<MovingObject>();
        gazeTrajectory = new MovingObject(null);
        activeObjects = new List<string>();
        correlationWriter = new StreamWriter("log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        correlationWriter.WriteLine("Gameobject;Timestamp;r;t");

        // search for objects tagged 'Trackable' and add them to the list
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Trackable")) register(go);

        // set time window for the correlation
        w = (long)(correlationDurationMs * Math.Pow(10, 6)); //ms to ns

        PupilGazeTracker.OnEyeGaze += new PupilGazeTracker.OnEyeGazeDeleg(UpdateTrajectories);
        startTime = DateTime.Now;

        //StartCoroutine(UpdateTrajectories()); // Coroutine to update the trajectories
        StartCoroutine(CalculatePearson());
        //StartCoroutine(CheckForResult());
	}
	
	// Update is called once per frame
	void Update () {
        foreach (MovingObject mo in sceneObjects) mo.updatePosition();        
    }

    public void register(GameObject go)
    {
        sceneObjects.Add(new MovingObject(go));
    }

    /// <summary>
    /// Gets called if PupilGazeTracker receives a new gazepoint. 
    /// Initiates addition of a new TimePoint to the trajectories of all MovingObjects
    /// and to the gazeTrajectory
    /// </summary>
    /// <param name="manager"></param>
    void UpdateTrajectories(PupilGazeTracker manager)
    {
        // receive new gaze point
        Vector3 newgaze = PupilGazeTracker.Instance.GetEyeGaze(Gaze);
        // calculate the time at which the gaze was probably recorded
        DateTime _correctedTs = new DateTime(DateTime.Now.Ticks - (long)newgaze.z);
        // add new gaze point to the trajectory
        gazeTrajectory.addNewGaze(_correctedTs, newgaze);
        // add positions at the moment of _correctedTs to all MovingObjects' trajectories
        foreach (MovingObject mo in sceneObjects) mo.addNewPosition(_correctedTs);
        //Debug.Log("New Gaze");
    }


    IEnumerator CalculatePearson()
    {
        while (!_shouldStop)
        {
            DateTime calcTime;
            MovingObject[] _tempObjects = new MovingObject[sceneObjects.Count];
            sceneObjects.CopyTo(_tempObjects); //work on a copy to (hopefully) improve performance
            //List<MovingObject> _tempObjects = sceneObjects.; 
            MovingObject _tempGaze = gazeTrajectory;

            foreach (MovingObject mo in _tempObjects)
            {
                try
                {
                    calcTime = DateTime.Now;
                    double zaehler = 0, nenner = 0, nenner1 = 0, nenner2 = 0, coeff = 0;
                    for (int i = 0; i < Math.Min(gazeTrajectory.length(), mo.length()); i++)
                    {
                        zaehler += (gazeTrajectory.getXPoints()[i] - gazeTrajectory.getXPoints().Average()) * (mo.getXPoints()[i] - mo.getXPoints().Average());
                        nenner1 += Math.Pow((gazeTrajectory.getXPoints()[i] - gazeTrajectory.getXPoints().Average()), 2);
                        nenner2 += Math.Pow((mo.getXPoints()[i] - mo.getXPoints().Average()), 2);
                    }
                    nenner = nenner1 * nenner2;
                    nenner = Math.Sqrt(nenner);
                    coeff = zaehler / nenner;

                    correlationWriter.WriteLine(mo.name + ";" + (DateTime.Now-startTime).TotalMilliseconds + ";" + coeff + ";" + (DateTime.Now-calcTime).TotalMilliseconds);

                    if (coeff > 0.5) mo.activate(true);
                    else mo.activate(false);
                } catch (IndexOutOfRangeException ie)
                {
                    Debug.LogError("Out of bounds");
                }
                
            }
            yield return new WaitForSeconds(0.005f); //wait for x seconds before the next correlation
        }
    }

    //IEnumerator CheckForResult()
    //{
    //    while(true)
    //    {
    //        foreach (MovingObject mo in sceneObjects)
    //        {
    //            if (activeObjects.Contains(mo.getName())) mo.activate(true);
    //            else mo.activate(false);
    //            yield return null;
    //        }
    //    }
    //}

    private void OnDestroy()
    {
        _shouldStop = true;
        foreach (MovingObject mo in sceneObjects) mo.killMe();
        correlationWriter.Close();
        gazeTrajectory.killMe();
    }
}
