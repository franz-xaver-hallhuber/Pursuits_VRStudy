using System;
using UnityEngine;

public class CircularMovement : MonoBehaviour {

    public float radius, startAngleDeg, degPerSec, accelerationPerSec = 0;
    public bool counterClockwise, faceCenter, randomStartPosition, shouldStart;
    public enum RotationAxis {
        xAxis,
        yAxis,
        zAxis
    }
    public RotationAxis rotationAxis;

    private double nextRad;
    private Vector3 localCenter;
    private Quaternion startRotation;
    public bool waitForInit = true;

    
    // Use this for initialization
    void Start () {
        if (!waitForInit)
        {
            localCenter = transform.localPosition;
            nextRad = Mathf.Deg2Rad * (randomStartPosition ? UnityEngine.Random.Range(0, 259) : startAngleDeg);
            startRotation = transform.rotation;
        }
    }

    public void Init()
    {
        if (waitForInit)
        {
            localCenter = transform.localPosition;
            nextRad = Mathf.Deg2Rad * (randomStartPosition ? UnityEngine.Random.Range(0, 259) : startAngleDeg);
            startRotation = transform.rotation;
        }
    }
    

    // Update is called once per frame
    void Update () {
        if (shouldStart)
        {
            double newX, newY, newZ;

            switch (rotationAxis)
            {
                case RotationAxis.xAxis:
                    newZ = localCenter.x + Math.Cos(nextRad) * radius;
                    newY = localCenter.y + Math.Sin(nextRad) * radius;
                    transform.localPosition = new Vector3(localCenter.x, (float)newY, (float)newZ);
                    break;
                case RotationAxis.yAxis:
                    newX = localCenter.x + Math.Cos(nextRad) * radius;
                    newZ = localCenter.z + Math.Sin(nextRad) * radius;
                    transform.localPosition = new Vector3((float)newX, localCenter.y, (float)newZ);
                    break;
                case RotationAxis.zAxis:
                    newX = localCenter.x + Math.Cos(nextRad) * radius;
                    newY = localCenter.y + Math.Sin(nextRad) * radius;
                    transform.localPosition = new Vector3((float)newX, (float)newY, localCenter.z);
                    break;
                default:
                    break;
            }

            // calculate next step
            degPerSec += accelerationPerSec * Time.deltaTime;
            nextRad += Mathf.Deg2Rad * (counterClockwise ? 1 : -1) * degPerSec * Time.deltaTime;
            // nextRad = (nextRad == Mathf.Abs(Mathf.PI * 2)) ? 0 : nextRad;

            if (faceCenter)
                transform.rotation = Quaternion.LookRotation(localCenter - transform.localPosition);
            else
                transform.localRotation = startRotation;
        }
    }
}