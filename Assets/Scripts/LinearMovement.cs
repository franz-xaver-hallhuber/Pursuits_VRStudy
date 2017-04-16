using System;
using UnityEngine;

public class LinearMovement : MonoBehaviour {
    public float speed, min, max, accelerationPerSec = 0;
    public bool randomStartPosition, jojo;
    //StreamWriter write;

    //progressiveTilt parentobject;
    //bool tiltMyFather;
    public bool _shouldStart, stopAtEnd, mainMovementAxis;

    public enum MovementType {
        xAxis, yAxis, zAxis
    }

    public MovementType movement;

	// Use this for initialization
	void Start () {
        if (randomStartPosition)
        {
            switch (movement)
            {
                case MovementType.xAxis:
                    transform.localPosition = new Vector3(UnityEngine.Random.Range(min, max), transform.localPosition.y, transform.localPosition.z);
                    break;
                case MovementType.yAxis:
                    transform.localPosition = new Vector3(transform.localPosition.x, UnityEngine.Random.Range(min, max), transform.localPosition.z);
                    break;
                case MovementType.zAxis:
                    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, UnityEngine.Random.Range(min, max));
                    break;
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
        if (_shouldStart)
        {
            Vector3 temp = transform.localPosition;
            float val;

            speed += accelerationPerSec * Time.deltaTime * Mathf.Sign(speed);

            transform.localPosition = new Vector3(
                temp.x + ((movement == MovementType.xAxis) ? speed : 0) * Time.deltaTime,
                temp.y + ((movement == MovementType.yAxis) ? speed : 0) * Time.deltaTime,
                temp.z + ((movement == MovementType.zAxis) ? speed : 0) * Time.deltaTime);

            switch (movement)
            {
                case MovementType.xAxis:
                    val = temp.x;
                    break;
                case MovementType.yAxis:
                    val = temp.y;
                    break;
                case MovementType.zAxis:
                    val = temp.z;
                    break;
                default:
                    val = 0;
                    break;
            }


            if (val > max)
            {
                if (mainMovementAxis && stopAtEnd)
                    QuitTrial();
                else
                {
                    speed = -speed;
                    transform.localPosition = new Vector3(
                        (movement == MovementType.xAxis) ? (jojo ? max : min) : temp.x,
                        (movement == MovementType.yAxis) ? (jojo ? max : min) : temp.y,
                        (movement == MovementType.zAxis) ? (jojo ? max : min) : temp.z);
                }
            }
            else if (val < min)
            {
                speed = -speed;
                transform.localPosition = new Vector3(
                    (movement == MovementType.xAxis) ? min : temp.x,
                    (movement == MovementType.yAxis) ? min : temp.y,
                    (movement == MovementType.zAxis) ? min : temp.z);
            }
            //write.WriteLine(Time.time + ";" + transform.localPosition.x);
        }
        //else transform.position = transform.position;
    }

    private void QuitTrial()
    {
        UnityEditor.EditorApplication.isPlaying = false;
    }


}
