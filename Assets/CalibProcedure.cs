using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CalibProcedure : MonoBehaviour {

    public GameObject xBox, yBox, camera, xScale, yScale, xTick, yTick;

    private string maxFOVX = "";
    private string maxFOVY = "";
    private string minX = "";
    private string maxX = "";
    private string minY = "";
    private string maxY = "";
    private string participant = "";

    // Use this for initialization
    void Start () {        
        xBox.GetComponent<Renderer>().material.color = Color.red;
        yBox.GetComponent<Renderer>().material.color = Color.red;

        createScales();
    }
	
	// Update is called once per frame
	void Update () {
		if (Input.anyKeyDown)
        {
            xScale.SetActive(false);
            yScale.SetActive(false);
            xBox.SetActive(false);
            yBox.SetActive(false);
            xTick.SetActive(false);
            yTick.SetActive(false);

            if (Input.GetKeyDown(KeyCode.Q)) xScale.SetActive(true);
            if (Input.GetKeyDown(KeyCode.W)) yScale.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E)) xBox.SetActive(true);
            if (Input.GetKeyDown(KeyCode.R)) yBox.SetActive(true);
        }
	}

    private void OnGUI()
    {
        int yPos = 80;

        GUI.Box(new Rect(0, yPos, 300, 30), "Enter maximum horizontal FOV in degrees.");
        maxFOVX = GUI.TextArea(new Rect(300, yPos, 40, 30), maxFOVX);

        yPos += 30; // newline

        GUI.Box(new Rect(0, yPos, 300, 30), "Enter maximum vertical FOV in degrees.");
        maxFOVY = GUI.TextArea(new Rect(300, yPos, 40, 30), maxFOVY);

        yPos += 30; // newline

        GUI.Box(new Rect(0, yPos, 300, 30), "Enter minimum x");
        minX = GUI.TextArea(new Rect(300, yPos, 40, 30), minX);

        if (GUI.Button(new Rect(340, yPos, 30,30),"get")) minX= camera.GetComponent<Camera>().WorldToScreenPoint(xBox.transform.position).x.ToString();

        yPos += 30; // newline

        GUI.Box(new Rect(0, yPos, 300, 30), "Enter maximum x");
        maxX = GUI.TextArea(new Rect(300, yPos, 40, 30), maxX);

        if (GUI.Button(new Rect(340, yPos, 30, 30), "get")) maxX = camera.GetComponent<Camera>().WorldToScreenPoint(xBox.transform.position).x.ToString();

        yPos += 30; // newline

        GUI.Box(new Rect(0, yPos, 300, 30), "Enter minimum y");
        minY = GUI.TextArea(new Rect(300, yPos, 40, 30), minY);

        if (GUI.Button(new Rect(340, yPos, 30, 30), "get")) minY = camera.GetComponent<Camera>().WorldToScreenPoint(yBox.transform.position).y.ToString();

        yPos += 30; // newline

        GUI.Box(new Rect(0, yPos, 300, 30), "Enter maximum y");
        maxY = GUI.TextArea(new Rect(300, yPos, 40, 30), maxY);

        if (GUI.Button(new Rect(340, yPos, 30, 30), "get")) maxY = camera.GetComponent<Camera>().WorldToScreenPoint(yBox.transform.position).y.ToString();

        yPos += 30; // newline

        GUI.Box(new Rect(0, yPos, 300, 30), "Participant No ");
        participant = GUI.TextArea(new Rect(300, yPos, 40, 30), participant, 2);

        yPos += 30; // newline

        if (GUI.Button(new Rect(100, yPos, 100, 30), "Submit")) writeData();
 
    }

    private void writeData()
    {
        //Directory.CreateDirectory("UserCalibData");
        StreamWriter dataWriter = new StreamWriter(@"UserCalibData\UserData.csv",true);
        dataWriter.WriteLine(participant
            + ";" + maxFOVX
            + ";" + maxFOVY
            + ";" + minX
            + ";" + maxX
            + ";" + minY
            + ";" + maxY
            + ";" + xBox.transform.localPosition.z);

        dataWriter.Close();
    }

    void createScales()
    {
        GameObject yScaleObject = GameObject.Find("yScaleElement");
        GameObject xScaleObject = GameObject.Find("xScaleElement");

        for (int i = 5; i < 360; i += 5)
        {
            GameObject _yCopy = GameObject.Instantiate(yScaleObject, GameObject.Find("yScale").transform);
            GameObject _xCopy = GameObject.Instantiate(xScaleObject, GameObject.Find("xScale").transform);

            _yCopy.transform.localPosition = new Vector3(0, Mathf.Sin(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z, Mathf.Cos(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z);
            _xCopy.transform.localPosition = new Vector3(Mathf.Sin(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z,0, Mathf.Cos(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z);

            _yCopy.GetComponentInChildren<TextMesh>().text = i + "°";
            _xCopy.GetComponentInChildren<TextMesh>().text = i + "°";
        }

        for (int i = 1; i < 360; i ++)
        {
            if (!(i%5==0))
            {
                GameObject _yCopy = GameObject.Instantiate(yTick, GameObject.Find("yScale").transform);
                GameObject _xCopy = GameObject.Instantiate(xTick, GameObject.Find("xScale").transform);

                _yCopy.transform.localPosition = new Vector3(0, Mathf.Sin(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z, Mathf.Cos(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z);
                _xCopy.transform.localPosition = new Vector3(Mathf.Sin(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z, 0, Mathf.Cos(i * Mathf.Deg2Rad) * yScaleObject.transform.localPosition.z);
            }
            
        }

        yScaleObject.GetComponentInChildren<MeshRenderer>().material.color = Color.blue;
        xScaleObject.GetComponentInChildren<MeshRenderer>().material.color = Color.blue;
    }

}
