
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VeloTrial
{
    public float trajectoryRadius { get; set; }
    public float objectVelocity { get; set; }
    
    public VeloTrial (float radius, float velo)
    {
        trajectoryRadius = radius;
        objectVelocity = velo;
    }
}

public class StudyMaster2000a : MonoBehaviour {

    public int numberOfObjects, numberOfRepetitions;
    public float[] radii, velocities;
    public bool automatic, walkingTask;
    public string studyName;

    private List<VeloTrial> combinations;
    public enum state
    {
        creatingCases,
        waitingForUserInput,
        creatingObjects,
        readyToStart,
        studyRunning,
        studyOver
    }

    public state currentState { get; private set; }
    public int counterThreshold;

    public double timeLimit = 10;

    private string participant = "";
    private int _currentRun = 0;

    private Correlator2A coco;
    private GameObject correlator;
    private bool _abort;
    private string _lastEntry;

    public float pearsonThreshold = 0.9f;

    private StreamWriter conditionWriter;

    
    GameObject eyeCam;

    // Use this for initialization
    void Start () {
        currentState = state.creatingCases;

        combinations = new List<VeloTrial>();
        

        // for the start just create a list with all combinations in random order
        foreach (float f in radii)
        {
                foreach (float h in velocities)
                    for(int i = 0;i<numberOfRepetitions;i++)
                        combinations.Add(new VeloTrial(f, h));
        }
        
        // shuffle combinations
        int n = combinations.Count-1;
        while (n>1) {
            int k = (int)Mathf.Floor(UnityEngine.Random.Range(0,n - 1));
            VeloTrial _t = combinations[k];
            combinations[k] = combinations[n];
            combinations[n] = _t;
            n--;
        }

        currentState = state.waitingForUserInput;
        
    }


    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width-200, 0, 200, 60), currentState.ToString() + "\nParticipant No: " + participant + "\nLastEntry: " + _lastEntry);
        if (currentState == state.waitingForUserInput)
        {
            GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 100, 200, 200), "Enter Participant Number");
            participant = GUI.TextField(new Rect(Screen.width / 2 - 50, Screen.height / 2 - 25, 100, 50), participant, 2);
            if (GUI.Button(new Rect(Screen.width / 2 - 30, Screen.height / 2 + 40, 60, 20), "Submit"))
            {
                startStudy();
            }
        }
        
        // when done:
        // currentState = state.creatingObjects;
    }

    private void resize(GameObject g, float newdepth)
    {
        //float _newDist = this.transform.position.z - myCam.transform.position.z;
        float _newFactor = g.transform.localScale.z * newdepth / 6; // use depth 6 as size reference: everything has to have the size as if it was at depth 6
        g.transform.localScale = new Vector3(_newFactor, _newFactor, _newFactor);
    }

    private float resize(float oldRad, float newdepth)
    {
        return oldRad * newdepth / 6; // use depth 6 as size reference: everything has to have the size as if it was at depth 6
    }

    private void createObjects()
    {
        if (_currentRun < combinations.Count)
        {
            GameObject master = GameObject.Find("cubePrefab");
            eyeCam = GameObject.Find("Camera (eye)");

            conditionWriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";"
                + combinations[_currentRun].trajectoryRadius + ";"
                + combinations[_currentRun].objectVelocity + ";"
                + walkingTask
                );
            

            for (int i = 0; i < numberOfObjects; i++)
            {
                GameObject newCube = GameObject.Instantiate(master, new Vector3(0,0,0), master.transform.localRotation, eyeCam.transform);
                
                newCube.tag = "Trackable";
                newCube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                newCube.GetComponent<MeshRenderer>().enabled = false;
                newCube.transform.localPosition = new Vector3(0, 0, 6); // because Instantiate location is global
                newCube.name = (i + 1) + "";

                CircularMovement cm = newCube.GetComponent<CircularMovement>();
                cm.startAngleDeg = i * (360 / numberOfObjects);
                cm.shouldStart = false;
                cm.waitForInit = false;
                cm.radius = combinations[_currentRun].trajectoryRadius;
                cm.degPerSec = Mathf.Rad2Deg * combinations[_currentRun].objectVelocity / combinations[_currentRun].trajectoryRadius;

                Debug.Log("velocity " + combinations[_currentRun].objectVelocity + " radius " + combinations[_currentRun].trajectoryRadius + " corrected Rad " + cm.radius  + " degPerSec " + cm.degPerSec);
            }
                    
        } else
        {
            currentState = state.studyOver;
        }
        
    }



    private void startStudy()
    {
        correlator = new GameObject("Correlator2A");
        coco = correlator.AddComponent<Correlator2A>();

        _abort = false;
        

        // set Correlator variables
        coco.corrFrequency = 0.2f;
        coco.w = 300;
        coco.corrWindow = 900;
        coco.threshold = pearsonThreshold;
        coco.Coefficient = Correlator2A.CorrelationMethod.Pearson;
        coco.transparent = true;
        coco.waitForInit = true;
        coco._shouldStop = true;
        coco.participantID = Convert.ToInt32(participant);
        coco.selectAimAuto = true;
        coco.enableHalo = false;
        coco.startRightAway = true;
        coco.Gaze = PupilGazeTracker.GazeSource.BothEyes;
        coco.justCount = true;
        coco.MaxTimeLimitSec = timeLimit;
        coco.counterThreshold = counterThreshold;
        string logFolder = coco.Init(studyName);

        conditionWriter = new StreamWriter(logFolder + @"\log_Conditions_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv"); ;
        conditionWriter.WriteLine("timestamp;radius;velocity;walking");
        StartCoroutine(conductStudy());
        
        // start walking task
        if (walkingTask)
        {
            GetComponent<WalkingTask>().Init(logFolder);
            StartCoroutine(GetComponent<WalkingTask>().RunWalkingTask());
            GameObject.Find("[CameraRig]").GetComponent<MeshRenderer>().enabled = true;
        }
        else // disable play area visualisazion if there's no walking task
        {
            GameObject.Find("[CameraRig]").GetComponent<MeshRenderer>().enabled = false;
        }
    }

    /// <summary>
    /// If Correlator._shouldStop is true at some point, StudyMaster2000 will shut it down and load the next setting
    /// </summary>
    /// <returns></returns>
    IEnumerator conductStudy()
    {

        currentState = state.readyToStart;
        StartCoroutine(waitforStart());

        // wait until user hits space
        while (currentState == state.readyToStart) yield return null;

        while (_currentRun < combinations.Count)
        {
            // prepare the scene
            
            _abort = false; // reset abort flag

            createObjects();

            //selects aim
            coco.registerObjectsAndSelectAim();

            yield return new WaitForSeconds(1);

            // start correlator
            coco._shouldStop = false;
            coco.startCoroutine(); //also enables object movement and visibility

            // wait until an object is selected
            while (!coco._shouldStop) yield return null; 

            _lastEntry = coco.Reset(); //reset the Correlator

            // wait until all objects are killed
            while (GameObject.FindGameObjectsWithTag("Trackable").Length != 0) yield return null;

            if (!_abort) _currentRun++; // increment
        }
        currentState = state.studyOver;

        coco._shouldStop = true;
        GetComponent<WalkingTask>()._shouldStop = true;
        coco.StopAllCoroutines();
        Destroy(coco);
        Destroy(correlator);
        
    }

    internal void abortTrial()
    {
        _abort = true;
        coco._shouldStop = true;
    }
    
    IEnumerator waitforStart()
    {
        while (currentState == state.readyToStart)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                currentState = state.studyRunning;
            }
            yield return null;
        }        
    }

    private void OnDestroy()
    {
        conditionWriter.Close();
    }
}
