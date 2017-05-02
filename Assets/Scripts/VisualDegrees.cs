using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VisualDegrees : MonoBehaviour {

    private string maxFOV;
    private string minX;
    private string maxX;
    private string minY;
    private string maxY;
    private string participant;
    private string atDepth;

    public string UserDataFile = @"UserCalibData\UserData.csv";
    


    // Use this for initialization
    void Start () {
		
	}
	
    public void Init(int userID)
    {
        participant = userID.ToString();
        foreach (string s in File.ReadAllLines(UserDataFile))
        {
            if (s.StartsWith(participant))
            {
                string[] userData = s.Split(';');
                if (userData.Length == 7)
                {
                    maxFOV = userData[1];
                    minX = userData[2];
                    maxX = userData[3];
                    minY = userData[4];
                    maxY = userData[5];
                    atDepth = userData[6];

                    return;
                } else
                {
                    Debug.LogError("User Data File Corrupted");
                }
            }
        }

        Debug.LogError("Participant not found. Make sure calibration was executed.");
    }

    public double GetWidthInDeg(GameObject go)
    {
        double _ret;
        Bounds goBounds = go.GetComponentInChildren<MeshFilter>().mesh.bounds;

        double maxWidthAtDepth = 2 * go.transform.localPosition.z * (((Convert.ToDouble(maxX) - Convert.ToDouble(minX)) / 2) / Convert.ToDouble(atDepth));

        

        _ret = (Convert.ToDouble(maxFOV) * goBounds.size.x / maxWidthAtDepth);

        Debug.Log("Object: " + go.name + " has width " + goBounds.size.x + " in deg " + _ret);

        return _ret;
    }
}
