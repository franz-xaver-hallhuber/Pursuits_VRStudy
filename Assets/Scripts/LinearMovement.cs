using UnityEngine;

public class LinearMovement : MonoBehaviour {
    public float speed, min, max, accelerationPerSec = 0;
    //StreamWriter write;

    //progressiveTilt parentobject;
    //bool tiltMyFather;

    public enum MovementType {
        xAxis, yAxis, zAxis
    }

    public MovementType movement;

	// Use this for initialization
	void Start () {
         
	}
	
	// Update is called once per frame
	void Update () {
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
            speed = -speed;
            transform.localPosition = new Vector3(
                (movement == MovementType.xAxis) ? max : temp.x,
                (movement == MovementType.yAxis) ? max : temp.y,
                (movement == MovementType.zAxis) ? max : temp.z);
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

    private void OnApplicationQuit()
    {
        //write.Close();
    }
}
