using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class xMovement : MonoBehaviour {
    public float speed, maxX, minX;
    //StreamWriter write;

    //progressiveTilt parentobject;
    bool tiltMyFather;

	// Use this for initialization
	void Start () {
        //Vector3 temp = transform.localPosition;
        //transform.localPosition = new Vector3(temp.x, temp.y, temp.z);
        //write = new StreamWriter("log_" + name + "_xmove_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
        
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 temp = transform.localPosition;
        transform.localPosition = new Vector3(temp.x + speed, temp.y, temp.z);
        if (temp.x > maxX)
        {
            speed = -speed;
            transform.localPosition = new Vector3(maxX, temp.y, temp.z);
        }
        else if (temp.x < minX)
        {
            speed = -speed;
            transform.localPosition = new Vector3(minX, temp.y, temp.z);
        }
        //write.WriteLine(Time.time + ";" + transform.localPosition.x);
    }

    private void OnApplicationQuit()
    {
        //write.Close();
    }
}
