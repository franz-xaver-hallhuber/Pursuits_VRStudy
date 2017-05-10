using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VisualDegrees : MonoBehaviour {

    private string maxFOVX;
    private string minX;
    private string maxX;
    private string minY;
    private string maxY;
    private string participant;
    private string atDepth;

    private float fmaxFOVX;
    private float fminX;
    private float fmaxX;
    private float fminY;
    private float fmaxY;
    private float fparticipant;
    private float fatDepth;

    private Camera ec;

    public string UserDataFile = @"UserCalibData\UserData.csv";
    
 
	
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
                    fmaxFOVX = Convert.ToSingle(userData[1]);
                    fminX = Convert.ToSingle(userData[2]);
                    fmaxX = Convert.ToSingle(userData[3]);
                    fminY = Convert.ToSingle(userData[4]);
                    fmaxY = Convert.ToSingle(userData[5]);
                    fatDepth = Convert.ToSingle(userData[6]);

                    return;
                } else
                {
                    throw new FormatException("Wrong UserCalib File format!");
                }
            }
        }

        throw new KeyNotFoundException("Participant not found. Make sure calibration was executed.");
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
        
        _ret = (Convert.ToDouble(maxFOVX) * sizeX / maxWidthAtDepth);

        Debug.Log("Object " + go.name + " has angle " + _ret);

        return _ret;
    }

    public Vector2 ScreenSizeInDeg(GameObject go)
    {
        Vector2 _ret = new Vector2();

        float[] bounds = getObjectBoundsPx(go);
        
        // ########### (1) compute x-angle
        float Xalpha = fmaxFOVX / 2; // half the total field of view in degrees
        float Xd = (fmaxX - fminX) / (2 * Mathf.Tan(Xalpha)); // get viewing distance in px
        float Xbeta; // angle between min and (fmax-fmin)/2
        float Xgamma; // angle between max and (fmax-fmin)/2 

        Xbeta = Mathf.Atan2(Mathf.Abs((fmaxX - fminX) / 2 - bounds[0]), Xd);
        Xgamma = Mathf.Atan2(Mathf.Abs((fmaxX - fminX) / 2 - bounds[1]), Xd);

        
        // minX < (fmaxX - fminX) / 2)
        if (bounds[0] < (fmaxX - fminX) / 2)
        {
            if (bounds[1] < (fmaxX - fminX) / 2) _ret.x = Xbeta - Xgamma;
            else _ret.x = Xbeta + Xgamma;
        }
        // minX > (fmaxX - fminX) / 2)
        else if (bounds[0] > (fmaxX - fminX) / 2)
        {
            if (bounds[1] < (fmaxX - fminX) / 2) throw new Exception("min>max!!");
            else _ret.x = _ret.x = -Xbeta + Xgamma;
        }
        else
        {
            // TODO: xMin is x/2 ??
        }

        // ########### (2) compute y-angle
        // TODO: How to get vertical FOV?

        return _ret;
    }

    /// <summary>
    /// Returns GameObject bounds in px
    /// </summary>
    /// <param name="go">Respective GameObject</param>
    /// <returns>float[] {minX,maxX,minY,maxY}</returns>
    public float[] getObjectBoundsPx(GameObject go)
    {
        // create an instance of the object
        GameObject _tempObj = GameObject.Instantiate(go);

        // deactivate its MeshRenderer
        _tempObj.GetComponent<MeshRenderer>().enabled = false;

        // get bounds.extents for further calculation
        Bounds tempBounds = _tempObj.GetComponentInChildren<MeshCollider>().bounds;

        // destroy temporary object
        Destroy(_tempObj);
        
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

        return new float[] { minX,maxX,minY,maxY };
    }

    /// <summary>
    /// Returns Object size in px
    /// </summary>
    /// <param name="go">Respective GameObject</param>
    /// <returns>Vector2(width,height)</returns>
    public Vector2 ObjectSizeInPx(GameObject go)
    {
        float[] bounds = getObjectBoundsPx(go);
        return new Vector2(bounds[0] - bounds[1], bounds[2] - bounds[3]);
    }
}
