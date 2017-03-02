using UnityEngine;
using System.Collections;
using System.IO;

public class zMovement : MonoBehaviour {
    public float speed, maxZ, minZ;
    StreamWriter write;

	// Use this for initialization
	void Start () {
        Vector3 temp = transform.localPosition;
        transform.localPosition = new Vector3(temp.x, temp.y, minZ);
        write = new StreamWriter("zmove.csv");
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 temp = transform.localPosition;
        transform.localPosition = new Vector3(temp.x, temp.y, temp.z + speed);
        if (temp.z > maxZ)
        {
            speed = -speed;
            transform.localPosition = new Vector3(temp.x, temp.y, maxZ);
        }
        else if (temp.z < minZ)
        {
            speed = -speed;
            transform.localPosition = new Vector3(temp.x, temp.y, minZ);
        }
        write.WriteLine(Time.time + ";" + transform.localPosition.z);
    }

    private void OnApplicationQuit()
    {
        write.Close();
    }
}
