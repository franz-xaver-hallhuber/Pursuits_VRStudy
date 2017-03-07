using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateAroundZ : MonoBehaviour {

    public float radius, startAngle, degreesPerSec;
    public bool counterClockwise, faceCenter;

    private double nextRad;
    private Vector3 localCenter;
    private Quaternion startRotation;


	// Use this for initialization
	void Start () {
        localCenter = transform.localPosition;
        nextRad = Mathf.Deg2Rad * startAngle;
        startRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update () {
        double newX = localCenter.x + Math.Cos(nextRad) * radius;
        double newY = localCenter.y + Math.Sin(nextRad) * radius;
        transform.localPosition = new Vector3((float)newX, (float)newY, localCenter.z);
        nextRad += Mathf.Deg2Rad * (counterClockwise? 1 : -1) * degreesPerSec * Time.deltaTime;
        nextRad = (nextRad == Mathf.Abs(Mathf.PI * 2)) ? 0 : nextRad;

        //Vector3 upVec = new Vector3(localCenter.x + (float)Math.Cos(nextRad + Mathf.PI / 8) * radius, localCenter.y + (float)Math.Sin(nextRad + Mathf.PI / 8) * radius, localCenter.z);
        // if (faceCenter) transform.rotation = Quaternion.Euler(startRotation.eulerAngles.x, startRotation.eulerAngles.y, (float)(startRotation.eulerAngles.z + Mathf.Rad2Deg * nextRad));
        if (faceCenter)
            transform.rotation = Quaternion.LookRotation(localCenter - transform.localPosition);
        else
            transform.localRotation = startRotation;
    }
}