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

        // create an instance of the object
        GameObject _tempObj = GameObject.Instantiate(go);

        // deactivate its MeshRenderer
        // _tempObj.GetComponent<MeshRenderer>().enabled = false;

        // get bounds.extents for further calculation
        Bounds tempBounds = _tempObj.GetComponentInChildren<Renderer>().bounds;

        // destroy temporary object
        Destroy(_tempObj);

        // get size of object
        float sizeX = tempBounds.size.x;
        float sizeY = tempBounds.size.y;

        double maxWidthAtDepth = 2 * go.transform.localPosition.z * (((Convert.ToDouble(maxX) - Convert.ToDouble(minX)) / 2) / Convert.ToDouble(atDepth));
        
        _ret = (Convert.ToDouble(maxFOV) * sizeX / maxWidthAtDepth);

        Debug.Log("Object " + go.name + " has angle " + _ret);

        return _ret;
    }
}
