using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class circularMovement : MonoBehaviour {

    public float speed, radius, startAngle;
    public Vector3 localCenter;
    private float angle;
    private Vector3 startPosition;


	// Use this for initialization
	void Start () {
        angle = startAngle;
        transform.localPosition = new Vector3(localCenter.x - radius, localCenter.y, localCenter.z);
        transform.RotateAround(startPosition, Vector3.back, startAngle);
    }
	
	// Update is called once per frame
	void Update () {
        transform.RotateAround(startPosition, Vector3.back, speed * Time.deltaTime);
    }
}
