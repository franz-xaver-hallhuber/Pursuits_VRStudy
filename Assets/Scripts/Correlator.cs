using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    public class TimeSample
    {
        public TimeSpan timestamp { get; set; }
        public double sample;

        public TimeSample(TimeSpan ts, double s)
        {
            this.timestamp = ts;
            this.sample = s;
        }
    }
    

    public class MovingObject : ICloneable, IEquatable<MovingObject>
    {
        GameObject go;
        private StreamWriter positionWriter;
        private Vector3 _current;
        private List<TimeSample> movingCorr;

        public string name { get; set; }
        public List<TimePoint> trajectory { get; set; }

        public MovingObject(GameObject go)
        {
            this.go = go;
            trajectory = new List<TimePoint>();
            movingCorr = new List<TimeSample>();
            if (go != null) name = go.name;
            else name = "gaze";
            positionWriter = new StreamWriter("log_" + name + "_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            positionWriter.WriteLine(name+"Timestamp;"+name+"XPos");
            //positionWriter.WriteLine("nowTS;nowX;lastTS;lastX;pupilTS;scale");

        }
        public string getName()
        {
            return go.name;
        }

        /// <summary>
        /// if the oldest timestamp was more than w ticks ago, remove the oldest
        /// </summary>
        void cleanUpTraj(int w)
        {
            //if (trajectory.Count > 120) trajectory.RemoveAt(0);
            if (trajectory.Count > 0)
            {
                if ((PupilGazeTracker.Instance._globalTime - trajectory[0].timestamp).TotalMilliseconds > w)
                {
                    trajectory.RemoveAt(0);
                    cleanUpTraj(w);
                }
            }
        }

        void cleanUpCorr(int y)
        {
            if (movingCorr.Count > 0)
            {
                if ((PupilGazeTracker.Instance._globalTime - movingCorr[0].timestamp).TotalMilliseconds > y)
                {
                    movingCorr.RemoveAt(0);
                    cleanUpTraj(y);
                }
            }
        }

        /// <summary>
        /// Adds a new Correlation value to the list
        /// </summary>
        /// <param name="ts">Timestamp of when the calculation of the coefficient started</param>
        /// <param name="s">Value of the coefficient</param>
        /// <param name="y">Time window after which correlation values are to be deleted from the list</param>
        /// <returns>the average of all coefficients within <paramref name="y"/> Milliseconds</returns>
        public double addSample(TimeSpan ts, double s, int y)
        {
            movingCorr.Add(new TimeSample(ts,s));
            List<double> coefficients = new List<double>();
            foreach (TimeSample sample in movingCorr) coefficients.Add(sample.sample);
            cleanUpCorr(y);
            return coefficients.Average();
        }

        /// <summary>
        /// Adds the current position of a MovingObject to its trajectory
        /// </summary>
        public void addNewPosition(int w)
        {
            trajectory.Add(new TimePoint(now(), _current));
            cleanUpTraj(w);
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
                Vector3 _correctedPos = (_last.pos + Vector3.Scale(_current - _last.pos, new Vector3(scale, scale, scale)));
                trajectory.Add(new TimePoint(n-timeDelay, _correctedPos));
                // nowTS;nowX;lastTS;lastX;pupilTS;scale
                positionWriter.WriteLine(n - timeDelay + ";" + _correctedPos.x);
                //positionWriter.WriteLine(n + ";" + _current.x + ";" + _last.timestamp.TotalSeconds + ";" + _last.pos.x + ";" + timeDelay + ";" + scale);
                cleanUpTraj(w);
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
            TimePoint tp = new TimePoint(now() - timeDelay, gazePoint);
            trajectory.Add(tp);
            positionWriter.WriteLine(tp.timestamp.TotalSeconds + ";" + gazePoint.x);
            cleanUpTraj(w);
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

        public object Clone()
        {
            MovingObject newMo = (MovingObject) this.MemberwiseClone();
            newMo.trajectory = new List<TimePoint>(this.trajectory);
            return newMo;
        }

        public bool Equals(MovingObject other)
        {
            return other.name == name;
        }
    }

    // list, in which all trackable objects in the scene are stored
    List<MovingObject> sceneObjects;
    // list, in which the gaze trajectory is stored
    MovingObject gazeTrajectory;
    public PupilGazeTracker.GazeSource Gaze;
    public double pearsonThreshold = 0.8;
    // w: time window for the correlation algorithm, corrWindow: time window in which correlation coefficients are averaged
    public int w, corrWindow;

    private volatile bool _shouldStop;
    // logfiles
    private StreamWriter correlationWriter, trajectoryWriter;
    //TODO: während die objekte für die korrelation geklont werden, keine punkte hinzufügen, um inkonsistenzen zu vermeiden
    private bool _cloningInProgress; 
    
    // Use this for initialization
    void Start () {
        sceneObjects = new List<MovingObject>();
        gazeTrajectory = new MovingObject(null);
        correlationWriter = new StreamWriter("log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        trajectoryWriter = new StreamWriter("log_Trajectories_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        correlationWriter.WriteLine("Gameobject;Timestamp;rx;ry;t");
        //trajectoryWriter.WriteLine("Timestamp;xCube;xGaze;r");

        // search for objects tagged 'Trackable' and add them to the list
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Trackable")) register(go);

        PupilGazeTracker.OnEyeGaze += new PupilGazeTracker.OnEyeGazeDeleg(UpdateTrajectories);

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
        foreach (MovingObject mo in sceneObjects)
            mo.addNewPosition(newgaze.z, w);
        //Debug.Log("New Gaze");
    }

    IEnumerator CalculatePearson()
    {
        while (!_shouldStop)
        {
            TimeSpan calcStart = new TimeSpan();
            TimeSpan calcDur = new TimeSpan();

            List<MovingObject> _tempObjects = new List<MovingObject>();
            _cloningInProgress = true;
            foreach (MovingObject mo in sceneObjects) _tempObjects.Add((MovingObject)mo.Clone()); //work on a copy to (hopefully) improve performance

            MovingObject _tempGaze = (MovingObject) gazeTrajectory.Clone();
            _cloningInProgress = false;
            List<double> _tempXPgaze = new List<double>(_tempGaze.getXPoints());
            List<double> _tempYPgaze = new List<double>(_tempGaze.getYPoints());

            List<float> results = new List<float>();

            foreach (MovingObject mo in _tempObjects)
            {
                calcStart = PupilGazeTracker.Instance._globalTime;
                double zaehlerX = 0, nennerX = 0, nenner1X = 0, nenner2X = 0, coeffX = 0;
                double zaehlerY = 0, nennerY = 0, nenner1Y = 0, nenner2Y = 0, coeffY = 0;

                // temporary list for not having to generate a new one at every loop
                List<double> _tempXPObj = new List<double>(mo.getXPoints());
                List<double> _tempYPObj = new List<double>(mo.getYPoints());

                // surround calculation with try/catch block or else coroutine will end if something is divided by zero
                try
                {
                    for (int i = 0; i < Math.Min(_tempGaze.length(), mo.length()); i++)
                    {
                        // x correlation
                        zaehlerX += (_tempXPgaze[i] - _tempXPgaze.Average()) * (_tempXPObj[i] - _tempXPObj.Average()); // (_tempXPObj[i] - _tempXPObj.Average() is 0 when object is only moving along x-axis
                        nenner1X += Math.Pow((_tempXPgaze[i] - _tempXPgaze.Average()), 2);
                        nenner2X += Math.Pow((_tempXPObj[i] - _tempXPObj.Average()), 2);

                        //y correlation
                        zaehlerY += (_tempYPgaze[i] - _tempYPgaze.Average()) * (_tempYPObj[i] - _tempYPObj.Average());
                        nenner1Y += Math.Pow((_tempYPgaze[i] - _tempYPgaze.Average()), 2);
                        nenner2Y += Math.Pow((_tempYPObj[i] - _tempYPObj.Average()), 2);

                        // Gameobject; TimestampList; TimestampPoint ; x
                        // trajectoryWriter.WriteLine(mo.trajectory[i].timestamp.TotalSeconds + ";" +  mo.trajectory[i].pos.x + ";;");
                        // trajectoryWriter.WriteLine(_tempGaze.trajectory[i].timestamp.TotalSeconds + ";;" + _tempGaze.trajectory[i].pos.x + ";"); // remove when >1 objects in the scene
                    }

                    nennerX = nenner1X * nenner2X;
                    nennerX = Math.Sqrt(nennerX);

                    nennerY = nenner1Y * nenner2Y;
                    nennerY = Math.Sqrt(nennerY);

                    coeffX = zaehlerX / nennerX;
                    coeffY = zaehlerY / nennerY;
                    
                    // in cases where an onject only moves along one axis
                    if (double.IsNaN(coeffX)) { coeffX = coeffY; }
                    if (double.IsNaN(coeffY)) { coeffY = coeffX; }

                    // add result to the original list
                    results.Add((float)sceneObjects.Find(x => x.Equals(mo)).addSample(calcStart, (coeffX + coeffY) / 2, corrWindow));
                    
                    calcDur = PupilGazeTracker.Instance._globalTime - calcStart;
                    correlationWriter.WriteLine(mo.name + ";" + PupilGazeTracker.Instance._globalTime.TotalSeconds + ";" + coeffX + ";" + coeffY + ";" + calcDur.TotalSeconds);
                    // trajectoryWriter.WriteLine(calcTime + ";;;" + coeffX);
                }
                catch (Exception e)
                {
                    Debug.LogError("Out of bounds:" + e.StackTrace);
                }

                //activate only one item at a time
                for (int i = 0; i < results.Count; i++)
                {
                    // activate the object with the highest correlation value only if it's above pearsonThreshold
                    if (results[i].CompareTo(results.Max()) == 0 && results[i] > pearsonThreshold)
                        _tempObjects[i].activate(true); //doesn't matter if original or clone list is used as both refer to the same GameObject
                    else
                        _tempObjects[i].activate(false);
                }

                //if (results.Max() > pearsonThreshold) _tempObjects[results.IndexOf(results.Max())].activate(true);
            }
            yield return new WaitForSeconds(0.25f - (float) calcDur.TotalSeconds); // calculation should take place every x seconds
        }
    }

    private void OnDestroy()
    {
        _shouldStop = true;
        foreach (MovingObject mo in sceneObjects) mo.killMe();
        correlationWriter.Close();
        gazeTrajectory.killMe();
        trajectoryWriter.Close();
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
