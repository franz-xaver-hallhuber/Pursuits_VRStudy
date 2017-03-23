using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using Correlation;
using Assets.Scripts;


public class Correlator : MonoBehaviour {
    
    public PupilGazeTracker.GazeSource Gaze;
    public double threshold;
    // w: time window for the correlation algorithm, corrWindow: time window in which correlation coefficients are averaged
    public int w, corrWindow;
    public float corrFrequency;
    public enum CorrelationMethod { Pearson, Spearman };
    public CorrelationMethod Coefficient;
    
    // list, in which all trackable objects in the scene are stored
    List<MovingObject> sceneObjects;

    // list, in which the gaze trajectory is stored
    MovingObject gazeTrajectory;

    private volatile bool _shouldStop;

    // logfiles
    private StreamWriter correlationWriter;

    //TODO: während die objekte für die korrelation geklont werden, keine punkte hinzufügen, um inkonsistenzen zu vermeiden
    private bool _cloningInProgress, _spearmanIsRunning, _pearsonIsRunning;

    // Timespan to measure the duration of calculating a correlation factor for all objects in sceneObjects
    TimeSpan calcDur = new TimeSpan();

    Coroutine pearson, spearman;

    // Use this for initialization
    void Start () {
        Debug.Log("Start");

        sceneObjects = new List<MovingObject>();
        gazeTrajectory = new MovingObject(null,0);
        correlationWriter = new StreamWriter("log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        // trajectoryWriter = new StreamWriter("log_Trajectories_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        correlationWriter.WriteLine("Gameobject;Timestamp;rx;ry;t");
        //trajectoryWriter.WriteLine("Timestamp;xCube;xGaze;r");

        // search for objects tagged 'Trackable' and add them to the list
        int _newid = 1;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Trackable")) register(go, _newid++);

        // Set listener for new gaze points
        PupilGazeTracker.OnEyeGaze += new PupilGazeTracker.OnEyeGazeDeleg(UpdateTrajectories);

        // start the selected correlation coroutine
        startCoroutine();
	}

    private void startCoroutine()
    {
        switch (Coefficient)
        {
            case (CorrelationMethod.Pearson):
                pearson = StartCoroutine(CalculatePearson());
                break;
            case (CorrelationMethod.Spearman):
                spearman = StartCoroutine(CalculateSpearman());
                break;
        }
    }

    // Update is called once per frame
    void Update () {
        foreach (MovingObject mo in sceneObjects) mo.updatePosition();        
    }

    
    
    /// <summary>
    /// Convert GameObject to a MovingObject and add it to the list of traced Objects
    /// </summary>
    /// <param name="go">GameObject to be traced</param>
    public void register(GameObject go, int id)
    {
        sceneObjects.Add(new MovingObject(go,id));
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

        // add new gaze point to the trajectory
        gazeTrajectory.addNewGaze(newgaze.z, newgaze, w);

        // add positions at the moment of _correctedTs to all MovingObjects' trajectories
        foreach (MovingObject mo in sceneObjects)
            mo.addNewPosition(newgaze.z, w);
    }

    IEnumerator CalculateSpearman()
    {
        while (!_shouldStop)
        {
            _spearmanIsRunning = true;

            TimeSpan calcStart = new TimeSpan();
            calcStart = PupilGazeTracker.Instance._globalTime;

            List<MovingObject> _tempObjects = new List<MovingObject>();
            _cloningInProgress = true;
            foreach (MovingObject mo in sceneObjects) _tempObjects.Add((MovingObject)mo.Clone()); //work on a copy to (hopefully) improve performance

            MovingObject _tempGaze = (MovingObject)gazeTrajectory.Clone();
            _cloningInProgress = false;
            List<double> _tempXPgaze = new List<double>(_tempGaze.getXPoints());
            List<double> _tempYPgaze = new List<double>(_tempGaze.getYPoints());

            // write gaze points in logfile
            //foreach (double dx in _tempXPgaze) trajectoryWriter.WriteLine("gazex;" + calcStart.TotalSeconds + dx);
            //foreach (double dy in _tempYPgaze) trajectoryWriter.WriteLine("gazey;" + calcStart.TotalSeconds + dy);

            List<float> results = new List<float>();

            foreach (MovingObject mo in _tempObjects)
            {
                // temporary list for not having to generate a new one at every loop
                List<double> _tempXPObj = new List<double>(mo.getXPoints());
                List<double> _tempYPObj = new List<double>(mo.getYPoints());

                // write object coordinates to logfile
                //foreach (double dx in _tempXPObj) trajectoryWriter.WriteLine(mo.name + ";" + calcStart.TotalSeconds + dx);
                //foreach (double dy in _tempXPObj) trajectoryWriter.WriteLine(mo.name + ";" + calcStart.TotalSeconds + dy);

                // surround calculation with try/catch block or else coroutine will end if something is divided by zero
                try
                {
                    double coeffX = Spearman.calculateSpearman(_tempXPgaze, _tempXPObj);
                    double coeffY = Spearman.calculateSpearman(_tempYPgaze, _tempYPObj);

                    correlationWriter.WriteLine(mo.name + ";" + calcStart.TotalSeconds + ";" + coeffX + ";" + coeffY);

                    // add result to the MovingObject in the original list
                    results.Add((float)sceneObjects.Find(x => x.Equals(mo)).addSample(calcStart, (coeffX + coeffY) / 2, corrWindow));
                }
                catch (Exception e)
                {
                    Debug.LogError(e.StackTrace);
                }
            }

            //activate only one item at a time
            for (int i = 0; i < results.Count; i++)
            {
                // activate the object with the highest correlation value only if it's above threshold
                if (results[i].CompareTo(results.Max()) == 0 && results[i] > threshold / 2)
                    _tempObjects[i].activate(true); //doesn't matter if original or clone list is used as both refer to the same GameObject
                else
                    _tempObjects[i].activate(false);
            }

            calcDur = PupilGazeTracker.Instance._globalTime - calcStart;

            yield return new WaitForSeconds(corrFrequency - (float)calcDur.TotalSeconds); // calculation should take place every x seconds
        }
    }

    IEnumerator CalculatePearson()
    {
        while (!_shouldStop)
        {
            _pearsonIsRunning = true;

            TimeSpan calcStart = new TimeSpan();
            calcStart = PupilGazeTracker.Instance._globalTime;

            List<MovingObject> _tempObjects = new List<MovingObject>();

            // work with copies to (hopefully) improve performance
            _cloningInProgress = true;
            foreach (MovingObject mo in sceneObjects) _tempObjects.Add((MovingObject)mo.Clone()); //

            MovingObject _tempGaze = (MovingObject) gazeTrajectory.Clone();
            _cloningInProgress = false;

            List<double> _tempXPgaze = new List<double>(_tempGaze.getXPoints());
            List<double> _tempYPgaze = new List<double>(_tempGaze.getYPoints());

            List<float> results = new List<float>();

            foreach (MovingObject mo in _tempObjects)
            {
                // temporary list for not having to generate a new one at every loop
                List<double> _tempXPObj = new List<double>(mo.getXPoints());
                List<double> _tempYPObj = new List<double>(mo.getYPoints());

                // surround calculation with try/catch block or else coroutine will end if something is divided by zero
                try
                {
                    double coeffX = Pearson.calculatePearson(_tempXPgaze, _tempXPObj);
                    double coeffY = Pearson.calculatePearson(_tempYPgaze, _tempYPObj);

                    // in cases where an object only moves along one axis, replace NaN with 0
                    if (double.IsNaN(coeffX)) { coeffX = 0; }
                    if (double.IsNaN(coeffY)) { coeffY = 0; }

                    // add result to the original list
                    results.Add((float)sceneObjects.Find(x => x.Equals(mo)).addSample(calcStart, (coeffX + coeffY) / 2, corrWindow));

                    correlationWriter.WriteLine(mo.name + ";" + calcStart.TotalSeconds + ";" + coeffX + ";" + coeffY);
                }
                catch (Exception e)
                {
                    Debug.LogError("Out of bounds:" + e.StackTrace);
                }
            }

            //activate only one item at a time
            for (int i = 0; i < results.Count; i++)
            {
                // activate the object with the highest correlation value only if it's above pearsonThreshold
                if (results[i].CompareTo(results.Max()) == 0 && results[i] > threshold / 2)
                    _tempObjects[i].activate(true); //doesn't matter if original or clone list is used as both refer to the same GameObject
                else
                    _tempObjects[i].activate(false);
            }

            calcDur = PupilGazeTracker.Instance._globalTime - calcStart;

            yield return new WaitForSeconds(corrFrequency - (float) calcDur.TotalSeconds); // calculation should take place every x seconds
        }
    }
    
    private void OnDestroy()
    {
        _shouldStop = true;
        foreach (MovingObject mo in sceneObjects) mo.killMe();
        correlationWriter.Close();
        gazeTrajectory.killMe();
        //trajectoryWriter.Close();
    }

    private void OnGUI()
    {
        if (sceneObjects.Count > 0)
        {
            string str = "Watched Objects: " + sceneObjects.Count;
            str += "\nTraj. Length: " + sceneObjects[0].trajectory.Count;
            str += "\nCorr per sec: " + (1 / calcDur.TotalSeconds);
            GUI.TextArea(new Rect(200, 0, 200, 80), str);
        }
    }
}