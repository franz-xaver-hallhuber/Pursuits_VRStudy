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
        public TimeSpan timestamp { get; set; }
        public Vector3 pos { get; set; }

        public TimePoint(double time, Vector3 currentPos)
        {
            this.timestamp = TimeSpan.FromSeconds(time);
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
        void cleanUp(int w)
        {
            //if (trajectory.Count > 120) trajectory.RemoveAt(0);
            if (trajectory.Count > 0)
            {
                if ((PupilGazeTracker.Instance._globalTime - trajectory[0].timestamp).TotalMilliseconds > w)
                {
                    trajectory.RemoveAt(0);
                    cleanUp(w);
                }
            }
        }

        /// <summary>
        /// Adds the current position of a MovingObject to its trajectory
        /// </summary>
        public void addNewPosition(int w)
        {
            trajectory.Add(new TimePoint(PupilGazeTracker.Instance._globalTime.TotalSeconds, _current));
            cleanUp(w);
        }

        /// <summary>
        /// Get current timestamp from PupilGazeTracker
        /// </summary>
        /// <returns>Current Global Timestamp</returns>
        private double now()
        {
            return PupilGazeTracker.Instance._globalTime.TotalSeconds;
        }

        /// <summary>
        /// Interpolates between the last recorded position and the current position to determine the position at a given time
        /// </summary>
        /// <param name="timeDelay">The time delay it took to transfer the gaze data</param>
        public void addNewPosition(float timeDelay, int w)
        {
            if (trajectory.Count > 0)
            {
                double n = now();
                TimePoint _last = trajectory[trajectory.Count - 1];
                float scale = (float)((n - timeDelay - _last.timestamp.TotalSeconds) / (n - _last.timestamp.TotalSeconds));
                trajectory.Add(new TimePoint(n, _current + Vector3.Scale(_current - _last.pos, new Vector3(scale, scale, scale))));
                positionWriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";" + _current.x);
                cleanUp(w);
            } else
            {
                // in case trajectory is empty
                addNewPosition(w);
            }
        }

        /// <summary>
        /// to be called by the Update() method so that threads and coroutines can work with GameObject positions
        /// </summary>
        public void updatePosition()
        {
            _current = go.transform.localPosition;
        }

        public void addNewGaze(float timeDelay, Vector3 gazePoint, int w)
        {
            TimePoint tp = new TimePoint(PupilGazeTracker.Instance._globalTime.TotalSeconds - timeDelay, gazePoint);
            trajectory.Add(tp);
            positionWriter.WriteLine(tp.timestamp.TotalSeconds + ";" + gazePoint.x);
            cleanUp(w);
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

    public int w;

    private volatile bool _shouldStop;
    
    private StreamWriter correlationWriter;

    //private bool _calcInProgress;

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
        // receive new gaze point. z.Value already is the corrected timestamp
        Vector3 newgaze = PupilGazeTracker.Instance.GetEyeGaze(Gaze);
        // calculate the time at which the gaze was probably recorded
        //DateTime _correctedTs = new DateTime(DateTime.Now.Ticks - (long)newgaze.z);
        // add new gaze point to the trajectory
        gazeTrajectory.addNewGaze(newgaze.z, newgaze, w);
        // add positions at the moment of _correctedTs to all MovingObjects' trajectories
        foreach (MovingObject mo in sceneObjects) mo.addNewPosition(newgaze.z, w);
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

                    correlationWriter.WriteLine(mo.name + ";" + PupilGazeTracker.Instance._globalTime.TotalSeconds + ";" + coeff + ";" + (DateTime.Now-calcTime).TotalSeconds);

                    if (coeff > 0.5) mo.activate(true);
                    else mo.activate(false);
                } catch
                {
                    Debug.LogError("Out of bounds");
                }
                
            }
            yield return new WaitForSeconds(0.005f); //wait for x seconds before the next calculation
        }
    }

    private void OnDestroy()
    {
        _shouldStop = true;
        foreach (MovingObject mo in sceneObjects) mo.killMe();
        correlationWriter.Close();
        gazeTrajectory.killMe();
    }

    private void OnGUI()
    {
        if (sceneObjects.Count > 0)
        {
            string str = "Watched Objects=" + sceneObjects.Count;
            str += "\nTraj. Length:" + sceneObjects[0].trajectory.Count;
            GUI.TextArea(new Rect(200, 0, 200, 80), str);
        }
    }
}
