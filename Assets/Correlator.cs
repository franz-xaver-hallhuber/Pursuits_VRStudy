using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Meta.Numerics.Statistics;
using System.Threading;

public class Correlator : MonoBehaviour {
    public class TimePoint
    {
        public float timestamp { get; set; }
        public Vector3 pos { get; set; }

        public TimePoint(float time, Vector3 currentPos)
        {
            this.timestamp = time;
            this.pos = currentPos;
        }
    }

    public class MovingObject
    {
        GameObject go;
        public List<TimePoint> trajectory { get; set; }

        public MovingObject(GameObject go)
        {
            this.go = go;
            trajectory = new List<TimePoint>();
        }

        public void addNew()
        {
            trajectory.Add(new TimePoint(Time.time, go.transform.position));
            if (trajectory[trajectory.Count-1].timestamp < Time.time - Correlator.corrDurationSec) trajectory.RemoveAt(trajectory.Count-1);
        }

        public void addNewGaze(float timeOfCapture, Vector3 gazePoint)
        {
            trajectory.Add(new TimePoint(timeOfCapture, gazePoint));
            if (trajectory[trajectory.Count - 1].timestamp < timeOfCapture - corrDurationSec) trajectory.RemoveAt(trajectory.Count - 1);
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
    }

    List<MovingObject> sceneObjects;
    MovingObject gazeTrajectory;
    public static float corrDurationSec = 0.5f;
    Thread RelateThread;

    private volatile bool _shouldStop;

    // Use this for initialization
    void Start () {
        sceneObjects = new List<MovingObject>();
        gazeTrajectory = new MovingObject(null);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void register(GameObject go)
    {
        sceneObjects.Add(new MovingObject(go));
    }

    public void correlate(float timestampFromCapture, Vector3 gaze)
    {
        gazeTrajectory.addNewGaze(timestampFromCapture, gaze);

        foreach (MovingObject mo in sceneObjects)
        {
            mo.addNew();
            BivariateSample bvx = new BivariateSample();
            BivariateSample bvy = new BivariateSample();
            bvx.Add(gazeTrajectory.getXPoints(), mo.getXPoints());
            bvy.Add(gazeTrajectory.getYPoints(), mo.getYPoints());

            if (bvx.CorrelationCoefficient > 0.5) mo.activate(true);
            else mo.activate(false);
            
        }
    }
}
