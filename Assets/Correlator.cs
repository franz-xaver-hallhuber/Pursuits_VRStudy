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
        private StreamWriter write;

        public string name { get; set; }
        public List<TimePoint> trajectory { get; set; }

        

        public MovingObject(GameObject go)
        {
            this.go = go;
            trajectory = new List<TimePoint>();
            if (go != null) name = go.name;
            else name = "gaze";
            write = new StreamWriter("log_" + name + "_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        }

        public string getName()
        {
            return go.name;
        }

        void cleanUp()
        {
            if (trajectory.Count > 120) trajectory.RemoveAt(0);
        }

        public void addNew()
        {
            trajectory.Add(new TimePoint(Time.time, go.transform.localPosition));
            write.WriteLine(Time.time + ";" + go.transform.localPosition.x);
            cleanUp();
        }

        public void addNewGaze(float timeOfCapture, Vector3 gazePoint)
        {
            trajectory.Add(new TimePoint(timeOfCapture, gazePoint));
            write.WriteLine(timeOfCapture + ";" + gazePoint.x);
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
    }

    List<MovingObject> sceneObjects;
    MovingObject gazeTrajectory;
    public static float corrDurationSec = 0.1f;
    public PupilGazeTracker.GazeSource Gaze;
    List<string> activeObjects;

    private volatile bool _shouldStop;

    Thread performCalc;
    private StreamWriter write;

    // Use this for initialization
    void Start () {
        sceneObjects = new List<MovingObject>();
        gazeTrajectory = new MovingObject(null);
        activeObjects = new List<string>();
        write = new StreamWriter("log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");

        // search for objects tagged 'Trackable'
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Trackable")) register(go);

        StartCoroutine(CalculatePearson());
        StartCoroutine(CheckForResult());
	}
	
	// Update is called once per frame
	void Update () {
        foreach (MovingObject mo in sceneObjects) mo.addNew();
        Vector3 newgaze = PupilGazeTracker.Instance.GetEyeGaze(Gaze);
        Debug.Log(newgaze.ToString());
        gazeTrajectory.addNewGaze(newgaze.z, newgaze);
    }

    public void register(GameObject go)
    {
        sceneObjects.Add(new MovingObject(go));
    }



    IEnumerator CalculatePearson()
    {
        while (!_shouldStop)
        {
            foreach (MovingObject mo in sceneObjects)
            {
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

                write.WriteLine(Time.time + ";" + coeff);

                if (coeff > 0.5) activeObjects.Add(mo.name);
                else activeObjects.Remove(mo.name);
            }
            yield return null;
        }
    }

    IEnumerator CheckForResult()
    {
        while(true)
        {
            foreach (MovingObject mo in sceneObjects)
            {
                if (activeObjects.Contains(mo.getName())) mo.activate(true);
                else mo.activate(false);
                yield return null;
            }
        }
    }

    private void OnDestroy()
    {
        _shouldStop = true;
    }
}
