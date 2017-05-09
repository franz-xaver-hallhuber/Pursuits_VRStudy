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

    private Camera ec;

    public string UserDataFile = @"UserCalibData\UserData.csv";
    
    // Use this for initialization
    void Start () {
		
	}
	
    public void Init(int userID, Camera eyeCam)
    {
        participant = userID.ToString();
        ec = eyeCam;
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

    public double RenderWidthInDeg(GameObject go)
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

    public Vector2 ScreenSizeInDeg(GameObject go)
    {
        Vector2 pxSz = ObjectSizeInPx(go);
        return new Vector2((pxSz.x * Convert.ToSingle(maxFOV) / (Convert.ToSingle(maxX) - Convert.ToSingle(minX))),0);
    }

    public Vector2 ObjectSizeInPx(GameObject go)
    {
        // create an instance of the object
        GameObject _tempObj = GameObject.Instantiate(go);

        // deactivate its MeshRenderer
        _tempObj.GetComponent<MeshRenderer>().enabled = false;

        // get bounds.extents for further calculation
        Bounds tempBounds = _tempObj.GetComponentInChildren<MeshCollider>().bounds;

        // destroy temporary object
        Destroy(_tempObj);

        // project extents on camera plane
        Vector3 newExtents = ec.transform.rotation * tempBounds.extents;
        //_deb += " projectedExt: " + newExtents.ToString();
        
        // calculate extents
        List<Vector3> minMaxValues = new List<Vector3>();
        
        minMaxValues.Add(new Vector3(tempBounds.center.x - tempBounds.extents.x, tempBounds.center.y + tempBounds.extents.y, tempBounds.center.z - tempBounds.extents.z));
        minMaxValues.Add(new Vector3(tempBounds.center.x - tempBounds.extents.x, tempBounds.center.y + tempBounds.extents.y, tempBounds.center.z + tempBounds.extents.z));
        minMaxValues.Add(new Vector3(tempBounds.center.x - tempBounds.extents.x, tempBounds.center.y - tempBounds.extents.y, tempBounds.center.z - tempBounds.extents.z));
        minMaxValues.Add(new Vector3(tempBounds.center.x - tempBounds.extents.x, tempBounds.center.y - tempBounds.extents.y, tempBounds.center.z + tempBounds.extents.z));
        minMaxValues.Add(new Vector3(tempBounds.center.x + tempBounds.extents.x, tempBounds.center.y + tempBounds.extents.y, tempBounds.center.z - tempBounds.extents.z));
        minMaxValues.Add(new Vector3(tempBounds.center.x + tempBounds.extents.x, tempBounds.center.y + tempBounds.extents.y, tempBounds.center.z + tempBounds.extents.z));
        minMaxValues.Add(new Vector3(tempBounds.center.x + tempBounds.extents.x, tempBounds.center.y - tempBounds.extents.y, tempBounds.center.z - tempBounds.extents.z));
        minMaxValues.Add(new Vector3(tempBounds.center.x + tempBounds.extents.x, tempBounds.center.y - tempBounds.extents.y, tempBounds.center.z + tempBounds.extents.z));

        float maxX = 0, maxY = 0, minX = float.MaxValue, minY = float.MaxValue;

        foreach (Vector3 x in minMaxValues)
        {
            Vector3 screenPoint = ec.GetComponent<Camera>().WorldToScreenPoint(ec.transform.TransformPoint(x));
            if (screenPoint.x < minX) minX = screenPoint.x;
            if (screenPoint.x > maxX) maxX = screenPoint.x;
            if (screenPoint.y < minY) minY = screenPoint.y;
            if (screenPoint.y > maxY) maxY = screenPoint.y;
        }

        return new Vector2(maxX - minX, maxY - minY);
    }
}
