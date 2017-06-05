using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using Correlation;
using Assets.Scripts;
using UnityEngine.SceneManagement;

public class Correlator2A : MonoBehaviour {
    
    public PupilGazeTracker.GazeSource Gaze;

    // above which threshold should a object be triggered?
    public double threshold;

    // w: time window for the correlation algorithm, corrWindow: time window in which correlation coefficients are averaged
    // timeframeCounter: duration in ms after which an object is selected in case of applyTimeframeCounter. 
    public int w, corrWindow, counterTimespan;

    // correlation should be calculated every x milliseconds
    public float corrFrequency;

    // correlation method to be used
    public enum CorrelationMethod { Pearson, Spearman };
    public CorrelationMethod Coefficient;

    // cube appearance: transparent (use texture with transparent background to apply on solid color), enableHalo (when object is fixated), startRightAway (if false wait for spacebar)
    public bool transparent = false, enableHalo = true, startRightAway = false;

    // base color for cube if transparent texture is used
    public Color cubeBase;

    // participant id
    public int participantID;

    // if false, all coroutines terminate
    public volatile bool _shouldStop;

    // if justCount: threshold after which an object is selected
    public int counterThreshold = 15;

    // which object is the participant told to look at
    public int lookAt = 0;

    //set this to true when the Correlator object is created by script to allow further amendments befor routines are started
    public bool waitForInit = true; 

    // if selectAimAuto: select one object randomly and color it in red, works only with transparent = true
    // if justCount: increase the MovingObject's counter if it meets activation criteria
    public bool selectAimAuto = false, justCount = false;

    // if object has been detected
    public bool weHaveAWinner;

    // variables to check whether a certain action is being performed
    private bool _cloningInProgress, _spearmanIsRunning, _pearsonIsRunning;

    // name of the current selected object
    public string selection;

    // list, in which all trackable objects in the scene are stored
    List<MovingObject> sceneObjects;

    // object, in which the gaze trajectory is stored
    MovingObject gazeTrajectory;

    // logfiles
    private StreamWriter correlationWriter, selectionWriter, counterWriter;
    public String logFolder = "Logfiles";
    
    // Timespan to measure the duration of calculating a correlation factor for all objects in sceneObjects
    TimeSpan calcDur = new TimeSpan();

    Coroutine correlationCoroutine;

    VisualDegrees vg;

    int currentDiff = 0;

    public double MaxTimeLimitSec;

    // Use this for initialization
    void Start () {
        if (!waitForInit)
        {
            sceneObjects = new List<MovingObject>();
            logFolder = logFolder + @"\Participant" + participantID + @"\" + SceneManager.GetActiveScene().name;
            Directory.CreateDirectory(logFolder);
            
            gazeTrajectory = new MovingObject(null, 0, participantID, logFolder);
            correlationWriter = new StreamWriter(logFolder + @"\log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            selectionWriter = new StreamWriter(logFolder + @"\log_Selection_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            counterWriter = new StreamWriter(logFolder + @"\log_Counters_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
            // trajectoryWriter = new StreamWriter("log_Trajectories_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");

            // logfile for correlator: name of GameObject; timestamp; corr.value x; corr.value y; w; correlation frequency;
            // selected correlation (Pearson/Spearman); source for gaze data (left/right/both);
            correlationWriter.WriteLine("Gameobject;Timestamp;rx;ry;w;corrWindow;corrFreq;corrMethod;eye;");

            // comparison of what is selected vs what the participant is told to look at
            selectionWriter.WriteLine("Timestamp;SelectionTime;intendedObj;selectedObj;correct?;smoothCorrel;speed;objWidthInDeg;objHeightInDeg;radiusInDeg;distanceToIntendedDeg;correlationToIntendedObjectX;correlationToIntendedObjectY;corrThreshold;w;corrAverageWindow;corrFrequency");

            // search for objects tagged 'Trackable', give them an ID and add them to the list
            int _newid = 1;
            

            // Set listener for new gaze points
            PupilGazeTracker.OnEyeGaze += new PupilGazeTracker.OnEyeGazeDeleg(UpdateTrajectories);

            // start the selected correlation coroutine
            startCoroutine();
        }
	}
    

    public void registerObjectsAndSelectAim()
    {
        selection = "";
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Trackable")) register(go, Convert.ToInt32(go.name));
        lookAt = UnityEngine.Random.Range(1, sceneObjects.Count);
        sceneObjects[lookAt - 1].setAimAuto(); //color the aim red
    }

    /// <summary>
    /// Initialize the Correlator programatically
    /// </summary>
    /// <param name="foldername">Name of the folder in which logfiles are stored. Folder structure will be <paramref name="foldername"/>\Participant{trialNo}\{SceneName}\lox_{xxx}</param>
    public string Init(string foldername)
    {
        sceneObjects = new List<MovingObject>();
        logFolder = foldername + @"\Participant" + participantID;
        Directory.CreateDirectory(logFolder); 
        
        // gaze object    
        gazeTrajectory = new MovingObject(null, 0, participantID, logFolder);

        // logger
        correlationWriter = new StreamWriter(logFolder + @"\log_Correlator_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        selectionWriter = new StreamWriter(logFolder + @"\log_Selection_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        counterWriter = new StreamWriter(logFolder + @"\log_Counters_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");

        // logfile for correlator: name of GameObject; timestamp; corr.value x; corr.value y; w; coefficient average window; correlation frequency;
        // selected correlation (Pearson/Spearman); source for gaze data (left/right/both);
        correlationWriter.WriteLine("Gameobject;Timestamp;rx;ry;w;corrWindow;corrFreq;corrMethod;eye;");

        // comparison of what is selected vs what the participant is told to look at
        selectionWriter.WriteLine("Timestamp;SelectionTime;aim;selection;correct?;smoothCorrelation;speedInDeg;widthInDeg;radiusInDeg;counterDifference;differenceAtSelection;pearsonThreshold;w;averageWindow;correlationFrequency");

        // search for objects tagged 'Trackable', give them an ID and add them to the list
        // int _newid = 0;#
        // IMPORTANT: give the object its number as its name

        // Set listener for new gaze points
        PupilGazeTracker.OnEyeGaze += new PupilGazeTracker.OnEyeGazeDeleg(UpdateTrajectories);

        // start the selected correlation coroutine
        // now done by StudyMaster2001

        vg = new VisualDegrees();
        vg.Init(participantID, GameObject.Find("Camera (eye)").GetComponent<Camera>());

        return logFolder;
    }
    

    public void startCoroutine()
    {
        _shouldStop = false;
        // start object movements depending on startRightAway
        foreach (MovingObject mo in sceneObjects) mo.startMoving();

        switch (Coefficient)
        {
            case (CorrelationMethod.Pearson):
                correlationCoroutine = StartCoroutine(CalculatePearson());
                break;
            case (CorrelationMethod.Spearman):
                correlationCoroutine = StartCoroutine(CalculateSpearman());
                break;
        }
    }

    // Update is called once per frame
    void Update () {
        if (!_shouldStop)
        {
            foreach (MovingObject mo in sceneObjects)
            {
                mo.updatePosition();
            }
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            counterThreshold++;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            counterThreshold--;
        }
    }
        
    /// <summary>
    /// Convert GameObject to a MovingObject and add it to the list of traced Objects
    /// </summary>
    /// <param name="go">GameObject to be traced</param>
    public void register(GameObject go, int id)
    {
        sceneObjects.Add(new MovingObject(go,id,participantID, logFolder));
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

    /// <summary>
    /// Clears all trajectories and selection
    /// </summary>
    public void clearTrajectories()
    {
        gazeTrajectory.flush();
        foreach (MovingObject mo in sceneObjects)
        {
            mo.flush();
        }

        selection = "";
    }

    public void hideObjects()
    {
        foreach (MovingObject mo in sceneObjects) mo.visible(false);
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
        TimeSpan coroutineStart = PupilGazeTracker.Instance._globalTime;
        while (!_shouldStop)
        {
            _pearsonIsRunning = true;

            TimeSpan calcStart = new TimeSpan();
            calcStart = PupilGazeTracker.Instance._globalTime;
            if (selection == "") // if there is an object selected dont do the math
            {
                List<MovingObject> _tempObjects = new List<MovingObject>();

                // work with copies to (hopefully) improve performance
                _cloningInProgress = true;
                foreach (MovingObject mo in sceneObjects) _tempObjects.Add((MovingObject)mo.Clone());

                MovingObject _tempGaze = (MovingObject)gazeTrajectory.Clone();
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

                        if (_shouldStop) break;

                        // add result to the original list

                        results.Add((float)sceneObjects.Find(x => x.Equals(mo)).addSample(calcStart, (coeffX + coeffY) / 2, corrWindow));
                        //Debug.Log("adding to results list: " + mo.name + "," + calcStart + "," + coeffX + "," + coeffY);
                        //correlationWriter.WriteLine(mo.name + ";" + calcStart.TotalSeconds + ";" + coeffX + ";" + coeffY + ";" + w + ";" + corrWindow + ";" + corrFrequency + ";" + Coefficient + ";" + Gaze);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Out of bounds:" + e.StackTrace);
                    }
                }

                if (_shouldStop) break;

                //MovingObject intention = _tempObjects.Find(x => x.Equals(lookAt + ""));
                selection = "";
                
                float _coeff = 0;

                //if after MaxTimeLimitSec no object meets the above criteria, just take the one with the highest counter
                if ((PupilGazeTracker.Instance._globalTime - coroutineStart).TotalSeconds > MaxTimeLimitSec)
                {
                    List<int> counters = new List<int>();
                    foreach (MovingObject mo in sceneObjects) counters.Add(mo.counter);
                    selection = sceneObjects.Find(x => x.counter == counters.Max()).getGameObject.name;
                    //Debug.Log("Timeout. Using Object " + selection + "?" + (Convert.ToInt32(selection) - 1));
                    _coeff = results[Convert.ToInt32(selection) - 1];
                }

                //analyse results
                for (int i = 0; i < results.Count; i++)
                {
                    // activate the object with the highest correlation value only if it's above threshold
                    if (results[i].CompareTo(results.Max()) == 0 && results[i] > threshold)
                    {
                        if (justCount)
                        {
                            sceneObjects[i].counter++;

                            if (compareThresholds(sceneObjects[i].counter))
                            {
                                selection = sceneObjects[i].getGameObject.name;
                                _coeff = results[i];

                                break;
                            }
                            
                        }
                    }
                }

                
                if (selection != "")
                {
                    CircularMovement _cm = _tempObjects.Find(x => x.Equals(selection)).getGameObject.GetComponent<CircularMovement>();
                    Vector3 _center = _cm.localCenter;
                    float _rad = _cm.radius;
                    

                    selectionWriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";"
                    + (PupilGazeTracker.Instance._globalTime - coroutineStart).TotalSeconds + ";"
                    + lookAt + ";"
                    + selection + ";"
                    + ((lookAt + "") == selection) + ";"
                    + _coeff + ";"
                    + _tempObjects.Find(x => x.Equals(selection)).speed + ";"
                    + vg.ScreenSizeInDeg(_tempObjects.Find(x => x.Equals(selection)).getGameObject).x + ";"
                    + vg.radiusWidthInDeg(new Vector3(_center.x - _rad, _center.y, _center.z), new Vector3(_center.x + _rad, _center.y, _center.z)) + ";"
                    + counterThreshold + ";"
                    + currentDiff + ";"
                    + threshold + ";" + w + ";" + corrWindow + ";" + corrFrequency);

                    writeCounters();

                    break;
                }
            }

            calcDur = PupilGazeTracker.Instance._globalTime - calcStart;

            _pearsonIsRunning = false;

            // calculation should take place every corrFrequency seconds
            yield return new WaitForSeconds(corrFrequency - (float)calcDur.TotalSeconds);
        }

        _shouldStop = true;
        
        Debug.Log("Pearson stopped.");
    }


    private bool compareThresholds(int counter)
    {
        List<int> counters = new List<int>();
        foreach (MovingObject mo in sceneObjects) counters.Add(mo.counter);       
        counters.Remove(counter);
        currentDiff = Math.Max(currentDiff, counter);
        return (counter - counters.Average()) >= counterThreshold;
    }

    void writeCounters()
    {
        counterWriter.Write(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";");
        foreach (MovingObject mo in sceneObjects) counterWriter.Write(mo.counter + ";");
        counterWriter.WriteLine();
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

    public string Reset()
    {
        _shouldStop = true;

        currentDiff = 0;

        foreach (MovingObject mo in sceneObjects) Destroy(mo.getGameObject);
        sceneObjects.Clear();
        gazeTrajectory.flush();

        return selection;
    }

    public int endTrial()
    {
        _shouldStop = true; // lets the coroutines finish
        
        correlationWriter.Close();
        selectionWriter.Close();
        counterWriter.Close();
        gazeTrajectory.killMe();
        return 0;
    }

    private void OnDestroy()
    {
        endTrial();
    }

    private void OnGUI()
    {
        if (sceneObjects.Count > 0)
        {
            string str = "Watched Objects: " + sceneObjects.Count;
            str += "\nTraj. Length: " + sceneObjects[0].trajectory.Count;
            str += "\nCorr Duration: " + calcDur.TotalMilliseconds + " ms";
            str += "\nlook at: " + lookAt;
            str += "\nCounter Threshold: " + counterThreshold;
            str += "\nCurrent Diference: " + currentDiff;
            GUI.TextArea(new Rect(200, 0, 200, 100), str);
        }
    }
}