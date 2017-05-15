using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
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

    public enum trajectoryType
    {
        linear,
        circular,
        none
    }

    public class MovingObject : ICloneable, IEquatable<MovingObject>
    {
        GameObject go;
        private StreamWriter positionWriter;
        public Vector3 _current { get; set; }
        private List<TimeSample> movingCorr;
        private Queue<TimePoint> tpBuffer;
        public int counter = 0;

        public GameObject getGameObject
        {
            get
            {
                return go;
            }
        }

        static bool copyinprogress, updateinprogess;

        public string name { get; set; }
        public float speed
        {
            get
            {
                if (go.GetComponent<LinearMovement>() != null)
                {
                    List<LinearMovement> atm = new List<LinearMovement>(go.GetComponents<LinearMovement>());
                    return atm.Find(x => x.mainMovementAxis).speed;
                }
                else if (go.GetComponent<CircularMovement>() != null) return go.GetComponent<CircularMovement>().degPerSec;
                else return 0;
            }
        }
        public List<TimePoint> trajectory { get; set; }

        public trajectoryType myTrajectory;

        public MovingObject(GameObject go, int id, int trial, string path)
        {
            this.go = go;
            trajectory = new List<TimePoint>();
            movingCorr = new List<TimeSample>();

            myTrajectory = new trajectoryType();

            if (go != null)
            {
                name = go.name;
                if (id >= 0 && id <= 10)
                {
                    Material mat = go.GetComponent<Renderer>().material;
                    mat.mainTexture = CreateNumberTexture.getNumberTexture(id,true);
                    //mat.color = Color.blue;
                    name = id+"";
                    go.name = name;
                    }

                // determine type of trajectory
                if (go.GetComponent<LinearMovement>()) myTrajectory = trajectoryType.linear;
                else if (go.GetComponent<CircularMovement>()) myTrajectory = trajectoryType.circular;
                else myTrajectory = trajectoryType.none;
            }
            else name = "gaze";
            
            tpBuffer = new Queue<TimePoint>();

            positionWriter = new StreamWriter(path + @"\log_" + name + "_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            positionWriter.WriteLine(getName() + "Timestamp;" + getName() + "XPos;" + getName() + "YPos;" + getName() + "newCorr;" + getName() + "smoothCorr");
            //positionWriter.WriteLine("nowTS;nowX;lastTS;lastX;pupilTS;scale");
            
        }

        public void startMoving()
        {
            foreach (LinearMovement lm in go.GetComponents<LinearMovement>()) lm._shouldStart = true;
            foreach (CircularMovement cm in go.GetComponents<CircularMovement>()) cm.shouldStart = true;
            go.GetComponent<MeshRenderer>().enabled = true;
        }

        public string getName()
        {
            return myTrajectory + name;
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
        
        public void setAimAuto()
        {
            go.GetComponent<Renderer>().material.color = Color.red;
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

        public void flush()
        {
            trajectory.Clear();
            counter = 0;
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
            // filter invalid correlation values
            if (double.IsNaN(s)) s = 0;
            if (s > 1 || s < -1) s = 0;

            movingCorr.Add(new TimeSample(ts, s));

            // average sample values
            List<double> coefficients = new List<double>();
            cleanUpCorr(y);
            foreach (TimeSample sample in movingCorr) coefficients.Add(sample.sample);
            double _average = (coefficients.Count > 0 ? coefficients.Average() : 0);
            positionWriter.WriteLine(ts.TotalSeconds + ";;;" + s + ";" + _average);
            //Debug.Log("log:" + coefficients.Average());
            return _average;
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

        private void whatsMySize()
        {
            ObjectToPx opx = GameObject.Find("Camera (eye)").GetComponent<ObjectToPx>();
            
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
                try
                {
                    float scale = (float)((n - timeDelay - _last.timestamp.TotalSeconds) / (n - _last.timestamp.TotalSeconds));
                    Vector3 _correctedPos = (_last.pos + Vector3.Scale(_current - _last.pos, new Vector3(scale, scale, scale)));
                    if (copyinprogress)
                    {
                        tpBuffer.Enqueue(new TimePoint(n - timeDelay, _correctedPos));
                        // Debug.Log("Objects Enqueued " + tpBuffer.Count);
                    }

                    else
                    {
                        updateinprogess = true;
                        while (tpBuffer.Count > 0) trajectory.Add(tpBuffer.Dequeue());
                        trajectory.Add(new TimePoint(n - timeDelay, _correctedPos));
                        cleanUpTraj(w);
                        updateinprogess = false;
                    }
                    
                } catch (Exception e)
                {
                    Debug.LogError("timeDelay:" + timeDelay + " _last:" + _last.timestamp.TotalSeconds + " n: " + n);
                }
                
            }
            else
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
            if (copyinprogress)
            {
                tpBuffer.Enqueue(tp);
            } else
            {
                while (tpBuffer.Count > 0) trajectory.Add(tpBuffer.Dequeue());
                trajectory.Add(tp);
                positionWriter.WriteLine(tp.timestamp.TotalSeconds + ";" + gazePoint.x + ";" + gazePoint.y);
                cleanUpTraj(w);
            }
        }

        public List<double> getXPoints()
        {
            List<double> ret = new List<double>();
            foreach (TimePoint tp in trajectory) if (tp != null) ret.Add(tp.pos.x);
            return ret;
        }

        public List<double> getYPoints()
        {
            List<double> ret = new List<double>();
            foreach (TimePoint tp in trajectory) if (tp != null) ret.Add(tp.pos.y);
            return ret;
        }

        public void activate(bool active)
        {
            //Renderer r = this.go.GetComponent<Renderer>();
            // Halo Behavior must be added to GameObjects manually as there's no way doing this by script
            Behaviour halo = (Behaviour)go.GetComponent("Halo"); 
            if (active)
            {
                //r.material.SetColor("_Color", Color.cyan);
                halo.enabled = true;
            }
            else
            {
                //r.material.SetColor("_Color", Color.gray);
                halo.enabled = false;
            }
        }

        public int length()
        {
            return trajectory.Count;
        }

        public void killMe()
        {
            positionWriter.Close();
            copyinprogress = false;
            UnityEngine.Object.Destroy(go);
        }

        public object Clone()
        {
            while (updateinprogess) {  }
            copyinprogress = true; // prevents trajectory list from being altered while creating copies
            MovingObject newMo = (MovingObject)this.MemberwiseClone();
            
            newMo.trajectory = new List<TimePoint>(this.trajectory);
            foreach (TimePoint tp in newMo.trajectory)
            {
                
                // if (tp != null) positionWriter.WriteLine(tp.timestamp.TotalSeconds + ";" + tp.pos.x + ";" + tp.pos.y + ";;");
                
            }
            copyinprogress = false;
            return newMo;
        }

        public void visible(bool visible)
        {
            go.GetComponent<MeshRenderer>().enabled = visible;
        }

        public bool Equals(MovingObject other)
        {
            return other.name == name;
        }

        public bool Equals (String other)
        {
            return name == other;
        }
    }
}