using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MySize : MonoBehaviour {

    public GameObject go;
    public Camera eyeCam;

	// Use this for initialization
	void Start () {
        if (go == null) go = this.gameObject;
	}
	
	// Update is called once per frame
	void Update () {
        string _str = "";

        

        //_str += go.GetComponent<Renderer>().bounds.size.x + "\n";
        //_str += go.GetComponent<Collider>().bounds.size.x + "\n";

        // erst das objekt in die richtige rotation bringen und dann die bounds bestimmen

        GameObject _temo = GameObject.Instantiate(go, eyeCam.transform);
        _temo.SetActive(false);
        _temo.transform.Rotate(-_temo.transform.rotation.eulerAngles);
        Bounds byRenderer = go.GetComponent<Renderer>().bounds;
        Destroy(_temo);

        float width, height, maxX = 0, minX = 5000;

        foreach (Vector3 v in getMinMax(byRenderer)) 
        {
            
            minX = Mathf.Min(minX, v.x);
            maxX = Mathf.Max(maxX, v.x);
            
        }
        
        Debug.Log(maxX-minX);
	}

    List<Vector3> getMinMax(Bounds goBounds)
    {
        List<Vector3> minMaxValues = new List<Vector3>();

        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z + goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z + goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z + goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z + goBounds.extents.z));

        return minMaxValues;
    }
}
