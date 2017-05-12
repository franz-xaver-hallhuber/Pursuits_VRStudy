using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VisualDegrees : MonoBehaviour {

    private string participant;

    private float fmaxFOVX;
    private float fmaxFOVY;
    private float fminX;
    private float fmaxX;
    private float fminY;
    private float fmaxY;
    private float fatDepth;

    float Xalpha; // half the total field of view in degrees
    float Xd; // get viewing distance in px
    float Xmiddle; // exact middle of fov in px

    float Ymiddle; // exact middle of fov in px
    float Yalpha; // half the total field of view in degrees
    float Yd; // get viewing distance in px

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
                if (userData.Length == 8)
                {
                    fmaxFOVX = Convert.ToSingle(userData[1]);
                    fmaxFOVY = Convert.ToSingle(userData[2]);
                    fminX = Convert.ToSingle(userData[3]);
                    fmaxX = Convert.ToSingle(userData[4]);
                    fminY = Convert.ToSingle(userData[5]);
                    fmaxY = Convert.ToSingle(userData[6]);
                    fatDepth = Convert.ToSingle(userData[7]);

                    Xalpha = fmaxFOVX / 2; // half the total horizontal field of view in degrees
                    Xd = (fmaxX - fminX) / (2 * Mathf.Tan(Xalpha * Mathf.Deg2Rad)); // get viewing distance in px
                    Xmiddle = fminX + (fmaxX - fminX) / 2;

                    Yalpha = fmaxFOVY / 2; // half the total vertical field of view in degrees
                    Yd = (fmaxY - fminY) / (2 * Mathf.Tan(Yalpha * Mathf.Deg2Rad)); // get viewing distance in px
                    Ymiddle = fminY + (fmaxY - fminY) / 2;

                    return;
                } else
                {
                    throw new FormatException("Wrong UserCalib File format!");
                }
            }
        }

        throw new KeyNotFoundException("Participant not found. Make sure calibration was executed.");
    }

    /// <summary>
    /// gets angle for a specific position measured from the middle point 0°
    /// </summary>
    /// <param name="localPos"></param>
    public Vector2 positionInDeg(Vector3 localPos)
    {
        Vector2 _ret = new Vector2();

        Vector3 worldPos = ec.transform.TransformPoint(localPos);
        Vector3 screenPos = ec.GetComponent<Camera>().WorldToScreenPoint(worldPos);

        float xPx =  screenPos.x - Xmiddle;
        _ret.x = Mathf.Atan2(xPx, Xd)*Mathf.Rad2Deg;

        float yPx = screenPos.y - Ymiddle;
        _ret.y = Mathf.Atan2(yPx, Yd)*Mathf.Rad2Deg;

        return _ret;
    }

    public float radiusWidthInDeg(Vector3 minLocal, Vector3 maxLocal)
    {
        Camera cam = ec.GetComponent<Camera>();

        if (maxLocal.x <minLocal.x)
        {
            Vector3 _temp = minLocal;
            minLocal = maxLocal;
            maxLocal = _temp;
        }

        float[] rBounds = { cam.WorldToScreenPoint(cam.transform.TransformPoint(minLocal)).x, cam.WorldToScreenPoint(cam.transform.TransformPoint(maxLocal)).x, 0,0 };
        return ScreenSizeInDeg(rBounds).x;
    }

    public Vector2 ScreenSizeInDeg(float[] bounds)
    {
        Vector2 _ret = new Vector2();

        // ########### (1) compute x-angle

        float Xbeta; // angle between min and (fmax-fmin)/2
        float Xgamma; // angle between max and (fmax-fmin)/2 

        
        Xbeta = Mathf.Atan2(Mathf.Abs(Xmiddle - bounds[0]), Xd) * Mathf.Rad2Deg;
        Xgamma = Mathf.Atan2(Mathf.Abs(Xmiddle - bounds[1]), Xd) * Mathf.Rad2Deg;

        // minX < (fmaxX - fminX) / 2
        if (bounds[0] < Xmiddle) // cases 1,4,7
        {
            if (bounds[1] < Xmiddle) _ret.x = Xbeta - Xgamma; // case 1
            else if (bounds[1] == Xmiddle) _ret.x = Xbeta; // case 4
            else _ret.x = Xbeta + Xgamma; // case 7
        }
        // minX > (fmaxX - fminX) / 2
        else if (bounds[0] > Xmiddle) // cases 3,6,9
        {
            if (bounds[1] <= Xmiddle) throw new Exception("minX>maxX!!"); // cases 3,6
            else _ret.x = -Xbeta + Xgamma; // case 9
        }
        // minX == (fmaxX - fminX) / 2
        else if (bounds[0] == Xmiddle) // cases 2,5,8
        {
            if (bounds[1] < Xmiddle) throw new Exception("minX>maxX!!"); // case 2
            else if (bounds[1] == Xmiddle) _ret.x = 0; // case 5
            else _ret.x = Xgamma; // case 8
        }

        // ########### (2) compute y-angle
        
        float Ybeta; // angle between min and (fmax-fmin)/2
        float Ygamma; // angle between max and (fmax-fmin)/2 
        

        

        Ybeta = Mathf.Atan2(Mathf.Abs(Ymiddle - bounds[2]), Yd) * Mathf.Rad2Deg;
        Ygamma = Mathf.Atan2(Mathf.Abs(Ymiddle - bounds[3]), Yd) * Mathf.Rad2Deg;

        // minY < (fmaxY - fminY) / 2
        if (bounds[2] < Ymiddle) // cases 1,4,7
        {
            if (bounds[3] < Ymiddle) _ret.y = Ybeta - Ygamma; // case 1
            else if (bounds[1] == Ymiddle) _ret.y = Ybeta; // case 4
            else _ret.y = Ybeta + Ygamma; // case 7
        }
        // minY > (fmaxY - fminY) / 2
        else if (bounds[2] > Ymiddle) // cases 3,6,9
        {
            if (bounds[3] <= Ymiddle) throw new Exception("minY>maxY!!"); // cases 3,6
            else _ret.y = -Ybeta + Ygamma; // case 9
        }
        // minY == (fmaxY - fminY) / 2
        else if (bounds[2] == Ymiddle) // cases 2,5,8
        {
            if (bounds[3] < Ymiddle) throw new Exception("minY>maxY!!"); // case 2
            else if (bounds[3] == Ymiddle) _ret.y = 0; // case 5
            else _ret.y = Ygamma; // case 8
        }

        return _ret;
    }

    public Vector2 ScreenSizeInDeg(GameObject go)
    {
        return ScreenSizeInDeg(getObjectBoundsPx(go));
    }

    public bool amIOffScreen(Vector3 localPos)
    {
        Vector3 worldCoor = ec.transform.TransformPoint(localPos);
        Vector3 screenCoor = ec.WorldToScreenPoint(worldCoor);
        // a²+b²=c²; b: y component of point on circle around xMiddle with radius c at a: x component of position, c: xMax - Xmiddle
        //Debug.Log("yDist to Middle" + Math.Abs(screenCoor.y - Ymiddle) + " point on circle " + Math.Sqrt(Math.Pow(fmaxX - Xmiddle, 2) - Math.Pow(screenCoor.x-Xmiddle, 2)));
        float distance = Vector2.Distance(new Vector2(screenCoor.x, screenCoor.y), new Vector2(Xmiddle, Ymiddle));
        //Debug.Log("Distance: " + distance + " radius " + (fmaxX - Xmiddle));
        return distance > fmaxX - Xmiddle;
        
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
        _tempObj.tag = "Untagged";

        // deactivate its MeshRenderer
        //_tempObj.GetComponent<MeshRenderer>().enabled = false;

        // get bounds.extents for further calculation
        Bounds tempBounds = _tempObj.GetComponentInChildren<Renderer>().bounds;

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
