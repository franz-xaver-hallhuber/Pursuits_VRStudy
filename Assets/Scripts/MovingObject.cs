using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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
            positionWriter.WriteLine(name + "Timestamp;" + name + "XPos;" + name + "YPos");
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
            movingCorr.Add(new TimeSample(ts, s));
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
                trajectory.Add(new TimePoint(n - timeDelay, _correctedPos));
                // nowTS;nowX;lastTS;lastX;pupilTS;scale
                positionWriter.WriteLine(n - timeDelay + ";" + _correctedPos.x + ";" + _correctedPos.y);
                //positionWriter.WriteLine(n + ";" + _current.x + ";" + _last.timestamp.TotalSeconds + ";" + _last.pos.x + ";" + timeDelay + ";" + scale);
                cleanUpTraj(w);
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
            trajectory.Add(tp);
            positionWriter.WriteLine(tp.timestamp.TotalSeconds + ";" + gazePoint.x + ";" + gazePoint.y);
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
            MovingObject newMo = (MovingObject)this.MemberwiseClone();
            newMo.trajectory = new List<TimePoint>(this.trajectory);
            return newMo;
        }

        public bool Equals(MovingObject other)
        {
            return other.name == name;
        }
    }
}