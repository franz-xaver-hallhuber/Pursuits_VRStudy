using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CalibProcedure : MonoBehaviour {

    public GameObject xBox, yBox;

    private string maxFOV = "";
    private string minX = "";
    private string maxX = "";
    private string minY = "";
    private string maxY = "";
    private string participant = "";

    // Use this for initialization
    void Start () {
        xBox.GetComponent<Renderer>().material.color = Color.red;
        yBox.GetComponent<Renderer>().material.color = Color.red;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnGUI()
    {
        GUI.Box(new Rect(0, 80, 300, 30), "Enter maximum FOV in degrees.");
        maxFOV = GUI.TextArea(new Rect(300, 80, 40, 30), maxFOV);

        GUI.Box(new Rect(0, 110, 300, 30), "Enter minimum x");
        minX = GUI.TextArea(new Rect(300, 110, 40, 30), minX);

        if (GUI.Button(new Rect(340,110,30,30),"get")) minX=xBox.transform.localPosition.x.ToString();
        
        GUI.Box(new Rect(0, 140, 300, 30), "Enter maximum x");
        maxX = GUI.TextArea(new Rect(300, 140, 40, 30), maxX);

        if (GUI.Button(new Rect(340, 140, 30, 30), "get")) maxX = xBox.transform.localPosition.x.ToString();

        GUI.Box(new Rect(0, 170, 300, 30), "Enter minimum y");
        minY = GUI.TextArea(new Rect(300, 170, 40, 30), minY);

        if (GUI.Button(new Rect(340, 170, 30, 30), "get")) minY = yBox.transform.localPosition.y.ToString();

        GUI.Box(new Rect(0, 200, 300, 30), "Enter maximum y");
        maxY = GUI.TextArea(new Rect(300, 200, 40, 30), maxY);

        if (GUI.Button(new Rect(340, 200, 30, 30), "get")) maxY = yBox.transform.localPosition.y.ToString();

        GUI.Box(new Rect(0, 230, 300, 30), "Participant No ");
        participant = GUI.TextArea(new Rect(300, 230, 40, 30), participant, 2);

        if (GUI.Button(new Rect(100, 260, 100, 30), "Submit")) writeData();
 
    }

    private void writeData()
    {
        //Directory.CreateDirectory("UserCalibData");
        StreamWriter dataWriter = new StreamWriter(@"UserCalibData\UserData.csv",true);
        dataWriter.WriteLine(participant
            + ";" + maxFOV
            + ";" + minX
            + ";" + maxX
            + ";" + minY
            + ";" + maxY
            + ";" + xBox.transform.localPosition.z);

        dataWriter.Close();
    }
}
