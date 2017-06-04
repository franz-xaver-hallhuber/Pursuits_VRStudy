using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using Correlation;
using Assets.Scripts;
using UnityEngine.SceneManagement;
using System.Text;

public class CorrelatorA : MonoBehaviour {
    
    public PupilGazeTracker.GazeSource Gaze;
    public double threshold;
    // w: time window for the correlation algorithm, corrWindow: time window in which correlation coefficients are averaged
    // timeframeCounter: duration in ms after which an object is selected in case of applyTimeframeCounter. 
    public int w, corrWindow, timeframeCounter;
    public float corrFrequency;
    public enum CorrelationMethod { Pearson, Spearman };
    public CorrelationMethod Coefficient;

    // cube appearance
    public bool transparent = false, enableHalo = true, startRightAway = false;
    public Color cubeBase;

    public int trialNo;
    // list, in which all trackable objects in the scene are stored
    List<MovingObject> sceneObjects;

    // list, in which the gaze trajectory is stored
    MovingObject gazeTrajectory;

    public volatile bool _shouldStop;

    // logfiles
    private StreamWriter correlationWriter, selectionWriter;
    private String logFolder = "Logfiles", selection = "";

    // TODO: während die objekte für die korrelation geklont werden, keine punkte hinzufügen, um inkonsistenzen zu vermeiden
    private bool _cloningInProgress, _spearmanIsRunning, _pearsonIsRunning;

    // which object is the participant told to look at?
    public int lookAt = 0;

    // Timespan to measure the duration of calculating a correlation factor for all objects in sceneObjects
    TimeSpan calcDur = new TimeSpan();

    Coroutine pearson, spearman;
    public bool waitForInit = true; //set this to true when the Correlator object is created by script to allow further amendments befor routines are started

    public bool selectAimAuto = false;
    private TimeSpan startOfTrial;

    VisualDegrees vd;

    // Use this for initialization
    void Start () {
        // Debug.Log("Start");
        

        if (!waitForInit)
        {
            sceneObjects = new List<MovingObject>();
            //trialNo = doLoggingStuff();
            logFolder = logFolder + @"\Participant" + trialNo + @"\" + SceneManager.GetActiveScene().name;
            Directory.CreateDirectory(logFolder);
            
            gazeTrajectory = new MovingObject(null, 0, trialNo, logFolder);
            correlationWriter = new StreamWriter(logFolder + @"\log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            selectionWriter = new StreamWriter(logFolder + @"\log_Selection_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            // trajectoryWriter = new StreamWriter("log_Trajectories_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");

            // logfile for correlator: name of GameObject; timestamp; corr.value x; corr.value y; w; correlation frequency;
            // selected correlation (Pearson/Spearman); source for gaze data (left/right/both);
            correlationWriter.WriteLine("Gameobject;Timestamp;rx;ry;w;corrWindow;corrFreq;corrMethod;eye;");

            // comparison of what is selected vs what the participant is told to look at
            selectionWriter.WriteLine("Timestamp;SelectionTime;intendedObj;selectedObj;correct?;smoothCorrel;speed;objWidthInDeg;objHeightInDeg;radiusInDeg;distanceToIntendedDeg;correlationToIntendedObject;corrThreshold;w;corrAverageWindow;corrFrequency");

            // search for objects tagged 'Trackable', give them an ID and add them to the list
            int _newid = 1;
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Trackable"))
            {
                register(go, _newid++);
            }

            vd = new VisualDegrees();
            vd.Init(Convert.ToInt32(trialNo), GameObject.Find("Camera (eye)").GetComponent<Camera>());

            // Set listener for new gaze points
            PupilGazeTracker.OnEyeGaze += new PupilGazeTracker.OnEyeGazeDeleg(UpdateTrajectories);

            

            // start the selected correlation coroutine
            startCoroutine();
        }
	}

    public void selectAim()
    {
        lookAt = UnityEngine.Random.Range(1, sceneObjects.Count);
        sceneObjects[lookAt - 1].setAimAuto(); //color the aim red
    }

    public string Init(string foldername)
    {
        sceneObjects = new List<MovingObject>();
        logFolder = foldername + @"\Participant" + trialNo;
        Directory.CreateDirectory(logFolder);
        
        gazeTrajectory = new MovingObject(null, 0, trialNo, logFolder);
        //correlationWriter = new StreamWriter(logFolder + @"\log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv",true);
        selectionWriter = new StreamWriter(logFolder + @"\log_Selection_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv", true);
        // trajectoryWriter = new StreamWriter("log_Trajectories_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");

        // logfile for correlator: name of GameObject; timestamp; corr.value x; corr.value y; w; correlation frequency;
        // selected correlation (Pearson/Spearman); source for gaze data (left/right/both);
        //correlationWriter.WriteLine("Gameobject;Timestamp;rx;ry;w;corrWindow;corrFreq;corrMethod;eye;");

        // comparison of what is selected vs what the participant is told to look at
        selectionWriter.WriteLine("Timestamp;SelectionTime;selectedObj;smoothCorrel;speedInDegPerS;radiusInUnits;radiusInDeg;walking;corrThreshold;w;corrAverageWindow;corrFrequency");

        // search for objects tagged 'Trackable', give them an ID and add them to the list
        // do that in studymaster

        // Set listener for new gaze points
        PupilGazeTracker.OnEyeGaze += new PupilGazeTracker.OnEyeGazeDeleg(UpdateTrajectories);

        vd = new VisualDegrees();
        vd.Init(Convert.ToInt32(trialNo), GameObject.Find("Camera (eye)").GetComponent<Camera>());

        // start the selected correlation coroutine
        startCoroutine();

        return logFolder;
    }

    public void registerNewObjectsAndSetAim()
    {
        int _newid = 1;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Trackable"))
            register(go, _newid++);
        selectAim();
    }

    /// <summary>
    /// Clears all object and gaze trajectories
    /// </summary>
    /// <returns>The last selected object is returned</returns>
    public string clearGazeAndRemoveObjects()
    {
        //StopAllCoroutines();
        // Debug.Log("Stop Coroutines");
        string _ret = selection;

        gazeTrajectory.flush();
        foreach (MovingObject mo in sceneObjects)
        {
            mo.killMe();
        }
        sceneObjects.Clear();

        selection = "";

        //startCoroutine();
        return _ret; // make sure object names are only numbers
    }

    public void startCoroutine()
    {
        // start object movements depending on startRightAway
        if (startRightAway) foreach (MovingObject mo in sceneObjects) mo.startMoving();
        else StartCoroutine(startMovementOnReturn());

        switch (Coefficient)
        {
            case (CorrelationMethod.Pearson):
                pearson = StartCoroutine(CalculatePearson());
                break;
            case (CorrelationMethod.Spearman):
                spearman = StartCoroutine(CalculateSpearman());
                break;
        }

        startOfTrial = PupilGazeTracker.Instance._globalTime;
    }

    // Update is called once per frame
    void Update () {
        if (!_shouldStop)
        {
            foreach (MovingObject mo in sceneObjects)
            {
                mo.updatePosition();
                if (!selectAimAuto)
                {
                    if (Input.anyKeyDown)
                    {
                        int _temp;
                        Int32.TryParse(Input.inputString, out _temp);
                        if (_temp > 0 && _temp < 10) lookAt = _temp;
                    }
                }
            }
        }
    }
        
    /// <summary>
    /// Convert GameObject to a MovingObject and add it to the list of traced Objects
    /// </summary>
    /// <param name="go">GameObject to be traced</param>
    public void register(GameObject go, int id)
    {
        sceneObjects.Add(new MovingObject(go,id,trialNo, logFolder));
    }

    /// <summary>
    /// Gets called if PupilGazeTracker receives a new gazepoint. 
    /// Initiates addition of a new TimePoint to the trajectories of all MovingObjects
    /// and to the gazeTrajectory
    /// </summary>
    /// <param name="manager"></param>
    void UpdateTrajectories(PupilGazeTracker manager)
    {
        if (!_shouldStop)
        {
            // receive new gaze point. z.Value is the corrected timestamp
            Vector3 newgaze = PupilGazeTracker.Instance.GetEyeGaze(Gaze);

            // add new gaze point to the trajectory
            gazeTrajectory.addNewGaze(newgaze.z, newgaze, w);

            // add positions at the moment of _correctedTs to all MovingObjects' trajectories
            foreach (MovingObject mo in sceneObjects)
                mo.addNewPosition(newgaze.z, w);
        }
    }

    IEnumerator startMovementOnReturn()
    {
        while (!startRightAway)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                startRightAway = true;
                foreach (MovingObject mo in sceneObjects) mo.startMoving();
            }
            yield return null;
        }
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

                    correlationWriter.WriteLine(mo.name + ";" + calcStart.TotalSeconds + ";" + coeffX + ";" + coeffY + ";" + w + ";" + corrWindow + ";" + corrFrequency + ";" + Coefficient + ";" + Gaze);

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
                if (results[i].CompareTo(results.Max()) == 0 && results[i] > threshold)
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
                    // Debug.Log("adding to results list: " + mo.name + "," + calcStart + "," + coeffX + "," + coeffY);
                    //correlationWriter.WriteLine(mo.name + ";" + calcStart.TotalSeconds + ";" + coeffX + ";" + coeffY + ";" + w + ";" + corrWindow + ";" + corrFrequency + ";" + Coefficient + ";" + Gaze);
                }
                catch (Exception e)
                {
                    Debug.LogError("Out of bounds:" + e.StackTrace);
                }
            }

            MovingObject intention = _tempObjects.Find(x => x.Equals(lookAt + ""));
            selection = "";

            //activate only one item at a time
            for (int i = 0; i < results.Count; i++)
            {
                // activate the object with the highest correlation value only if it's above threshold
                if (results[i].CompareTo(results.Max()) == 0 && results[i] > threshold)
                {
                    if (_tempObjects[i].name == (lookAt + "")) //only finish if the right object is selected
                    {
                        if (enableHalo) _tempObjects[i].activate(true); //doesn't matter if original or clone list is used as both refer to the same GameObject
                                                                        // if the wrong object is detected, calculate the correlation between the false object and the intended object
                                                                        //if (lookAt != 0)
                                                                        //{
                                                                        //selectionwriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";" + _tempObjects[i].name + ";" + lookAt + (_tempObjects[i].name.EndsWith(lookAt + "")
                                                                        //                        ? ";"
                                                                        //                        : ";" + resemblance(_tempObjects[i], _tempObjects.Find(x => x.Equals(lookAt + "")))));
                                                                        //}
                                                                        // else selectionwriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";" + _tempObjects[i].name + ";" + lookAt);
                        selection = _tempObjects[i].name;
                        _shouldStop = true;

                        TimeSpan totalTime = PupilGazeTracker.Instance._globalTime - startOfTrial;
                        CircularMovement cm = _tempObjects[i].getGameObject.GetComponent<CircularMovement>();
                        float _rad = _tempObjects[i].getGameObject.GetComponent<CircularMovement>().radius;

                        selectionWriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";" +
                            + totalTime.TotalSeconds + ";"
                            + selection + ";"
                            + results[i] + ";" 
                            + _tempObjects[i].speed + ";"
                            + cm.radius + ";"
                            + vd.radiusWidthInDeg(new Vector3(cm.localCenter.x - _rad, cm.localCenter.y, cm.localCenter.z), new Vector3(cm.localCenter.x + _rad, cm.localCenter.y, cm.localCenter.z)) + ";"
                            + threshold + ";" + w + ";" + corrWindow + ";" + corrFrequency
                            );
                    }
                    else
                    if (enableHalo) _tempObjects[i].activate(false);

                }
            }
            _pearsonIsRunning = false;
            
            calcDur = PupilGazeTracker.Instance._globalTime - calcStart;

            yield return new WaitForSeconds(corrFrequency - (float) calcDur.TotalSeconds); // calculation should take place every x seconds
           
        }
    }
    
    private double resemblance(MovingObject wrong, MovingObject correct)
    {
        double pearsonX = Pearson.calculatePearson(wrong.getXPoints(), correct.getXPoints());
        double pearsonY = Pearson.calculatePearson(wrong.getYPoints(), correct.getYPoints());

        if (double.IsNaN(pearsonX)) { pearsonX = 0; }
        if (double.IsNaN(pearsonY)) { pearsonY = 0; }

        return ((pearsonX + pearsonY) / 2);
    }

    private Vector2 resemblanceXY(MovingObject wrong, MovingObject correct)
    {
        double pearsonX = Pearson.calculatePearson(wrong.getXPoints(), correct.getXPoints());
        double pearsonY = Pearson.calculatePearson(wrong.getYPoints(), correct.getYPoints());

        if (double.IsNaN(pearsonX)) { pearsonX = 0; }
        if (double.IsNaN(pearsonY)) { pearsonY = 0; }

        return new Vector2((float)pearsonX, (float)pearsonY);
    }

    public string endTrial()
    {
        foreach (MovingObject mo in sceneObjects) mo.killMe();
        gazeTrajectory.killMe();

        //correlationWriter.Close();
        selectionWriter.Close();
        gazeTrajectory.killMe();
        return selection;
    }

    private void OnDestroy()
    {
        endTrial();
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