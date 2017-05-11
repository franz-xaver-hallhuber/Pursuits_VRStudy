using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lookAtCamera : MonoBehaviour {

    public GameObject cam;

	// Use this for initialization
	void Start () {
        if (cam == null) cam = GameObject.Find("Camera (eye)");
	}
	
	// Update is called once per frame
	void Update () {
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
	}
}
