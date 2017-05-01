using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorCollider : MonoBehaviour {

    public bool isOverlapping = false;
    bool recalibrateOnce = false;

	// Use this for initialization
	void Start () {
        //gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.one;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    

    private void OnTriggerStay(Collider other)
    {
        isOverlapping = true;
    }

    private void OnTriggerExit(Collider other)
    {
        isOverlapping = false;
    }
}
