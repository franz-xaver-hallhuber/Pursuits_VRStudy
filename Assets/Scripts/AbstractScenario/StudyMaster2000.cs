using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Trial
{
    public float trajectoryRadius { get; set; }
    public float objectSize { get; set; }
    public float objectDepth { get; set; }
    
    public Trial (float radius, float size, float depth)
    {
        trajectoryRadius = radius;
        objectSize = size;
        objectDepth = depth;
    }
}

public class StudyMaster2000 : MonoBehaviour {

    public int numberOfObjects;
    public float[] radii, sizes, depths;
    public bool automatic, walkingTask;
    public string studyName;

    private List<Trial> combinations;
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
    private string participant = "";
    private int _currentRun = 0;

    private Correlator coco;
    private GameObject correlator;
    private bool _abort;
    private string _lastEntry;

    private StreamWriter conditionWriter;

    // Use this for initialization
    void Start () {
        currentState = state.creatingCases;

        combinations = new List<Trial>();
        

        // for the start just create a list with all combinations in random order
        foreach (float f in radii)
        {
            foreach (float g in sizes)
            {
                foreach (float h in depths)
                    combinations.Add(new Trial(f, g, h));
            }
        }
        
        // shuffle combinations
        int n = combinations.Count-1;
        while (n>1) {
            int k = (int)Mathf.Floor(UnityEngine.Random.Range(0,n - 1));
            Trial _t = combinations[k];
            combinations[k] = combinations[n];
            combinations[n] = _t;
            n--;
        }

        currentState = state.waitingForUserInput;

        // disable play area visualisazion if there's no walking task
        if (walkingTask)
        {
            GetComponent<WalkingTask>().Init();
            StartCoroutine(GetComponent<WalkingTask>().RunWalkingTask());
            GameObject.Find("[CameraRig]").GetComponent<MeshRenderer>().enabled = true;
        } else
        {
            GameObject.Find("[CameraRig]").GetComponent<MeshRenderer>().enabled = false;
        }
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
            GameObject eyeCam = GameObject.Find("Camera (eye)");

            conditionWriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";"
                + combinations[_currentRun].trajectoryRadius + ";"
                + combinations[_currentRun].objectSize + ";"
                + combinations[_currentRun].objectDepth
                );

            for (int i = 0; i < numberOfObjects; i++)
            {
                GameObject newCube = GameObject.Instantiate(master, new Vector3(0,0,0), master.transform.localRotation, eyeCam.transform);
                
                newCube.tag = "Trackable";
                newCube.transform.localScale = new Vector3(combinations[_currentRun].objectSize, combinations[_currentRun].objectSize, combinations[_currentRun].objectSize);
                newCube.GetComponent<MeshRenderer>().enabled = false;
                newCube.transform.localPosition = new Vector3(0, 0, combinations[_currentRun].objectDepth); // because Instantiate location is global
                resize(newCube, combinations[_currentRun].objectDepth);

                CircularMovement cm = newCube.GetComponent<CircularMovement>();
                cm.startAngleDeg = i * (360 / numberOfObjects);
                cm.radius = resize(combinations[_currentRun].trajectoryRadius, combinations[_currentRun].objectDepth);
                cm.shouldStart = false;
                cm.waitForInit = false;
            }
                    
        } else
        {
            currentState = state.studyOver;
        }
        
    }

    private void startStudy()
    {
        correlator = new GameObject("Correlator");
        coco = correlator.AddComponent<Correlator>();

        _abort = false;

        // set Correlator variables
        coco.corrFrequency = 0.3f;
        coco.w = 300;
        coco.corrWindow = 900;
        coco.threshold = 0.6;
        coco.Coefficient = Correlator.CorrelationMethod.Pearson;
        coco.transparent = true;
        coco.waitForInit = true;
        coco._shouldStop = true;
        coco.trialNo = Convert.ToInt32(participant);
        coco.selectAimAuto = true;
        coco.enableHalo = false;
        coco.startRightAway = true;
        conditionWriter = new StreamWriter(coco.Init(studyName) + @"\log_Conditions_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv"); ;
        conditionWriter.WriteLine("timestamp;radius;size;depth");
        StartCoroutine(conductStudy());      
          
    }

    /// <summary>
    /// If Correlator._shouldStop is true at some point, StudyMaster2000 will shut it down and load the next setting
    /// </summary>
    /// <returns></returns>
    IEnumerator conductStudy()
    {
        while (_currentRun < combinations.Count)
        {
            // prepare the scene
            createObjects();
            _abort = false; // reset abort flag
            
            // registers objects and selects aim
            coco.registerNewObjectsAndSetAim();

            currentState = state.readyToStart;
            StartCoroutine(waitforStart());

            // wait until user hits space
            while (currentState == state.readyToStart) yield return null;

            // start correlator
            coco._shouldStop = false;
            coco.startCoroutine(); //also enables object movement and visibility

            // wait until an object is selected
            while (!coco._shouldStop) yield return null; 

            _lastEntry = coco.clearGazeAndRemoveObjects(); //reset the Correlator

            // wait until all objects are killed
            while (GameObject.FindGameObjectsWithTag("Trackable").Length != 0) yield return null;

            if (!_abort) _currentRun++; // increment
        }
        currentState = state.studyOver;

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
