using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trial
{
    public float trajectoryRadius { get; set; }
    public float objectSize { get; set; }
    
    public Trial (float radius, float size)
    {
        trajectoryRadius = radius;
        objectSize = size;
    }
}

public class StudyMaster2000 : MonoBehaviour {

    public int numberOfObjects;
    public float[] radii, sizes;
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

    // Use this for initialization
    void Start () {
        currentState = state.creatingCases;

        combinations = new List<Trial>();

        // for the start just create a list with all combinations in random order
        foreach (float f in radii)
        {
            foreach (float g in sizes)
            {
                combinations.Add(new Trial(f, g));
            }
        }
        
        int n = combinations.Count-1;
        while (n>1) {
            int k = (int)Mathf.Floor(UnityEngine.Random.Range(0,n - 1));
            Trial _t = combinations[k];
            combinations[k] = combinations[n];
            combinations[n] = _t;
            n--;
        }

        currentState = state.waitingForUserInput;

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
                if (Convert.ToInt32(participant) != 0) createObjects();
            }
        }
        
        // when done:
        // currentState = state.creatingObjects;
    }

    private void createObjects()
    {
        if (_currentRun < combinations.Count)
        {
            GameObject master = GameObject.Find("cubePrefab");
            GameObject eyeCam = GameObject.Find("Camera (eye)");
            for (int i = 0; i < numberOfObjects; i++)
            {
                GameObject newCube = GameObject.Instantiate(master, new Vector3(0,0,0), master.transform.localRotation, eyeCam.transform);
                newCube.transform.localPosition = new Vector3(0, 0, 6); // because Instantiate location is global
                newCube.tag = "Trackable";
                newCube.transform.localScale = new Vector3(combinations[_currentRun].objectSize, combinations[_currentRun].objectSize, combinations[_currentRun].objectSize);
                newCube.GetComponent<MeshRenderer>().enabled = false;

                CircularMovement cm = newCube.GetComponent<CircularMovement>();
                cm.startAngleDeg = i * (360 / numberOfObjects);
                cm.radius = combinations[_currentRun].trajectoryRadius;
                cm.shouldStart = false;
            }

            if (!automatic)
            {
                currentState = state.readyToStart;
                StartCoroutine(waitforStart());
            } else
            {
                currentState = state.studyRunning;
                startTrial();
            }            
        } else
        {
            currentState = state.studyOver;
        }
        
    }

    private void startTrial()
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
        coco._shouldStop = false;
        coco.trialNo = Convert.ToInt32(participant);
        coco.selectAimAuto = true;
        coco.enableHalo = true;
        coco.startRightAway = true;
        coco.Init(studyName);

        StartCoroutine(waitForCocoToFinish());        
    }

    /// <summary>
    /// If Correlator._shouldStop is true at some point, StudyMaster2000 will shut it down and load the next setting
    /// </summary>
    /// <returns></returns>
    IEnumerator waitForCocoToFinish()
    {
        while (!coco._shouldStop) yield return null;

        _lastEntry = coco.endTrial();
        coco.StopAllCoroutines();
        Destroy(coco);
        Destroy(correlator);

        if (!_abort) _currentRun++;
        currentState = state.creatingObjects;
        createObjects();

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
                startTrial();
            }
            yield return null;
        }        
    }
}
