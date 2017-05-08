using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class StudyMasterMeteor : MonoBehaviour {
    
    public string studyName;
    public float timeout;
    public int maxNumberOfMeteors, counterThreshold;
    public GameObject meteorPrefab;

    private List<string> allResults;
    private List<GameObject> allMeteors;
    
    public enum state
    {
        waitingForUserInput,
        creatingMeteors,
        readyToStart,
        gameRunning,
        gameOver
    }

    public state currentState { get; private set; }
    private string participant = "";
    
    private DateTime gameStart;

    private CorrelatorMeteor coco;
    private GameObject correlator;
    public float refreshRateSec;
    public GameObject eyeCam;
    private int meteorCounter=0;

    // Use this for initialization
    void Start () {
        allResults = new List<string>();
        allMeteors = new List<GameObject>();
        currentState = state.waitingForUserInput;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width-200, 0, 200, 80), currentState.ToString() + "\nParticipant No: " + participant + "\nNo of Meteors: " + allMeteors.Count +  "\nScore: ");
        if (currentState == state.waitingForUserInput)
        {
            GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 100, 200, 200), "Enter Participant Number");
            participant = GUI.TextField(new Rect(Screen.width / 2 - 50, Screen.height / 2 - 25, 100, 50), participant, 2);
            if (GUI.Button(new Rect(Screen.width / 2 - 30, Screen.height / 2 + 40, 60, 20), "Submit"))
            {
                if (Convert.ToInt32(participant) != 0)
                {
                    currentState = state.readyToStart;
                    StartCoroutine(waitForStart());
                }
            }
        }
    }

    private IEnumerator waitForStart()
    {
        while (!Input.GetKeyDown(KeyCode.Space)) yield return null;
        gameStart = DateTime.Now;
        currentState = state.gameRunning;
        startGame();
    }

    private IEnumerator createMeteors()
    {
        while ((DateTime.Now-gameStart).TotalSeconds < timeout)
        {
            while (GameObject.FindGameObjectsWithTag("Trackable").Length < maxNumberOfMeteors)
            {
                meteorPrefab.SetActive(true);

                // preferences of the new trajectory
                bool counter = UnityEngine.Random.value >= 0.5f; // counterclockwise?
                int degSec = UnityEngine.Random.Range(10, 90); // speed
                float rad = UnityEngine.Random.Range(0.5f, 2.5f); // radius

                // where should the center of the new trajectory be?
                Vector3 _newCenter = UnityEngine.Random.insideUnitSphere;
                _newCenter.Scale(new Vector3(3, 3, 3));

                //how big should the new meteors be?
                float _scale = UnityEngine.Random.Range(0.5f, 1.5f);
                
                // how many meteors should there be in the new trajectory? maximum 5
                int _atOnce = UnityEngine.Random.Range(1, Math.Min(maxNumberOfMeteors - GameObject.FindGameObjectsWithTag("Trackable").Length,5));
                
                for (int i = 0; i<_atOnce ; i++)
                {
                    GameObject _newMeteor = GameObject.Instantiate(meteorPrefab, new Vector3(0, 0, 0), Quaternion.identity, eyeCam.transform);
                    _newMeteor.transform.localPosition = new Vector3(_newCenter.x, _newCenter.y, 6); // +_newCenter.z); // because Instantiate location is global
                    _newMeteor.tag = "Trackable";
                    _newMeteor.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    _newMeteor.name = meteorCounter++.ToString();
                    _newMeteor.GetComponentInChildren<MeshRenderer>().enabled = false;
                    
                    CircularMovement _newMove = _newMeteor.AddComponent<CircularMovement>();
                    _newMove.counterClockwise = counter;
                    _newMove.degPerSec = degSec;
                    _newMove.radius = rad;
                    _newMove.randomStartPosition = true;
                    _newMove.rotationAxis = CircularMovement.RotationAxis.zAxis;
                    _newMove.Init();
                    _newMove.shouldStart = true;
                    _newMove.recalibrateOnce = false;

                    coco.register(_newMeteor, Convert.ToInt32(_newMeteor.name));
                    coco.selectAim();
                }
                yield return new WaitForSeconds(refreshRateSec);

                meteorPrefab.SetActive(false);
            }
            yield return null;
        }
        currentState = state.gameOver;
        coco.lookAt = -1;
    }

    private void startGame()
    {
        correlator = new GameObject("CorrelatorMeteor");
        coco = correlator.AddComponent<CorrelatorMeteor>();

        // set Correlator variables
        coco.corrFrequency = 0.08f; // in seconds, make sure this duration is longer than the average correlation cycle
        coco.w = 300;
        coco.corrWindow = 900;
        coco.threshold = 0.6;
        coco.Coefficient = CorrelatorMeteor.CorrelationMethod.Pearson;
        coco.waitForInit = true;
        coco._shouldStop = true;
        coco.participantID = Convert.ToInt32(participant);
        coco.enableHalo = false;
        coco.startRightAway = true;
        coco.Gaze = PupilGazeTracker.GazeSource.BothEyes;
        coco.justCount = true;
        coco.counterThreshold = counterThreshold;
        string logFolder = coco.Init(studyName);

        StartCoroutine(createMeteors());
        
        GetComponent<WalkingTask>().Init(logFolder);

        StartCoroutine(GetComponent<WalkingTask>().RunWalkingTask());

        coco.setAimAndStartCoroutine();
    }
}
