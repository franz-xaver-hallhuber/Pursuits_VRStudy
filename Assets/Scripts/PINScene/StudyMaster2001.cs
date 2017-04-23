﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StudyMaster2001 : MonoBehaviour {
    
    public float radius, size;
    public string studyName;
    public int numberOfTrials, pinLength = 4;
    public float timeoutSec = 5;

    private List<int> pins, result, digits;
    private List<string> allResults;

    public Light highlight;
    
    public enum state
    {
        cratingPins,
        waitingForUserInput,
        creatingObjects,
        readyToStart,
        studyRunning,
        studyOver
    }

    public state currentState { get; private set; }
    private string participant = "";

    // indices
    private int _currentRun = 0, _currentDigit = 0;

    private Correlator2 coco;
    private GameObject correlator;
    public int counterThreshold;

    // Use this for initialization
    void Start () {
        currentState = state.cratingPins;
        pins = new List<int>();
        result = new List<int>();
        digits = new List<int>();
        allResults = new List<string>();
        
        // for the start just create a list with all combinations in random order
        for (int i=0; i< numberOfTrials; i++)
        {
            pins.Add(UnityEngine.Random.Range(1000, 9999));
        }

        currentState = state.waitingForUserInput;

    }


    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width-200, 0, 200, 80), currentState.ToString() + "\nParticipant No: " + participant + "\nCurrent PIN: " + pins[_currentRun] +  "\nResult: " + resultAsString());
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
        if (_currentRun < numberOfTrials)
        {
            GameObject master = GameObject.Find("cubePrefab");
            GameObject eyeCam = GameObject.Find("Camera (eye)");

            // create clockwise objects
            for (int i = 1; i > 0; i++)
            {
                if (i > 9) i = 0; // makes sure the last object is tagged "0"

                GameObject newCube = GameObject.Instantiate(master, new Vector3(0,0,0), master.transform.localRotation, eyeCam.transform);
                newCube.transform.localPosition = new Vector3(((i > 5 || i == 0) ? -1 : 1) * -1.5f, 0, 6); // because Instantiate location is global. cubes 1-5 on the left, 6-0 to the right
                newCube.tag = "Trackable";
                newCube.transform.localScale = new Vector3(size, size, size);
                newCube.GetComponent<MeshRenderer>().enabled = false;
                newCube.name = i.ToString();

                CircularMovement cm = newCube.GetComponent<CircularMovement>();
                cm.startAngleDeg = (i - 1)%5 * 360 / 6;
                if (i==0) cm.startAngleDeg = (5 - 1) % 5 * 360 / 6;
                if (Enumerable.Range(1, 5).Contains(i)) cm.degPerSec = 30;

                cm.radius = radius;
                cm.shouldStart = false;
                cm.counterClockwise = (i > 5 || i == 0);
                cm.waitForInit = false;

                if (i == 0) break;
            }
            
                currentState = state.readyToStart;
                StartCoroutine(waitforStart());
           
        } else
        {
            currentState = state.studyOver;
        }
    }

    private void startTrial()
    {
        // split up PIN
        int _temp = pins[_currentRun];
        digits = new List<int>();
        while (_temp > 0)
        {
            digits.Add(_temp % 10);
            _temp /= 10;
        }
        digits.Reverse();

        // create and configure correlator
        correlator = new GameObject("Correlator2");
        coco = correlator.AddComponent<Correlator2>();

        // set Correlator variables
        coco.corrFrequency = 0.08f; // in seconds, make sure this duration is longer than the average correlation cycle
        coco.w = 300;
        coco.corrWindow = 900;
        coco.threshold = 0.6;
        coco.Coefficient = Correlator2.CorrelationMethod.Pearson;
        coco.transparent = true;
        coco.waitForInit = true;
        coco._shouldStop = false;
        coco.participantID = Convert.ToInt32(participant);
        coco.selectAimAuto = false;
        coco.enableHalo = false;
        coco.startRightAway = true;
        coco.Gaze = PupilGazeTracker.GazeSource.BothEyes;
        coco.counterThreshold = counterThreshold;
        coco.justCount = true;
        coco.Init(studyName);

        StartCoroutine(FeedCoco());        
    }

    /// <summary>
    /// Feeds the Correlator one digit at a time. Calls Correlator2.clearTrajectories either
    /// if an object has been detected or the timeout has been reached.
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator FeedCoco()
    {
        result.Clear();

        for (_currentDigit = 0; _currentDigit < pinLength; _currentDigit++)
        {
            float start = Time.realtimeSinceStartup;
            //Debug.Log("Started Correlator for digit " + _currentDigit);
            coco.setAimAndStartCoroutine(digits[_currentDigit]);
            
            while (!coco._shouldStop) // _shouldStop turns true if object was selected
            {
                if (Time.realtimeSinceStartup - start > timeoutSec)
                {
                    //Debug.Log("Reached time limit");
                    coco._shouldStop = true;
                    break;
                }
                yield return new WaitForSeconds(0.2f);
            }
            result.Add(coco.clearTrajectories());
            StartCoroutine(Flash());
            yield return new WaitForSeconds(0.5f);
        }

        allResults.Add(resultAsString());

        
        coco.endTrial(); // also kills all "Trackable"-tagged GameObjects
        coco.StopAllCoroutines(); // they stop anyway when _shouldStop is set to true, but to be sure..
        Destroy(coco);
        Destroy(correlator);

        // reset variables
        _currentRun++;
        _currentDigit = 0;

        currentState = state.creatingObjects;
        createObjects();
    }

    private string resultAsString()
    {
        string _ret = "";
        foreach (int i in result)
        {
            _ret += i;
        }
        return _ret;
    }

    /// <summary>
    /// Flashes the attached Highlight. Important: Set Baking to realtime and Render Mode to important
    /// </summary>
    private IEnumerator Flash()
    {
        float flashtimeSec = 0.5f;
        bool up = true;
        while (up)
        {
            highlight.GetComponent<Light>().intensity += Time.deltaTime * 8 * (1/flashtimeSec);
            if (highlight.GetComponent<Light>().intensity >= 8) up = false;
            yield return null;
        }
        while (!up)
        {
            highlight.GetComponent<Light>().intensity -= Time.deltaTime * 8 * (1/flashtimeSec);
            if (highlight.GetComponent<Light>().intensity <= 0) break;
            yield return null;
        }
    }

    internal void abortTrial()
    {
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