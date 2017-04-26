using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorCollider : MonoBehaviour {

    public bool isOverlapping = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnCollisionStay(Collision collision)
    {
        isOverlapping = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isOverlapping = false;
    }
}
