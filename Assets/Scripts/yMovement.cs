using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class yMovement : MonoBehaviour {
    public float speed, maxY, minY;
    //StreamWriter write;

	// Use this for initialization
	void Start () {
        //Vector3 temp = transform.localPosition;
        //transform.localPosition = new Vector3(temp.x, temp.y, temp.z);
        //write = new StreamWriter("log_" + name + "_ymove_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv");
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 temp = transform.localPosition;
        transform.localPosition = new Vector3(temp.x, temp.y + speed, temp.z);
        if (temp.y > maxY)
        {
            speed = -speed;
            transform.localPosition = new Vector3(temp.x, maxY, temp.z);
        }
        else if (temp.y < minY)
        {
            speed = -speed;
            transform.localPosition = new Vector3(temp.x, minY, temp.z);
        }
        //write.WriteLine(Time.time + ";" + transform.localPosition.y);
    }

    private void OnApplicationQuit()
    {
        //write.Close();
    }
}
