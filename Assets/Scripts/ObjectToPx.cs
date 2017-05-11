using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToPx : MonoBehaviour {

    public Camera ViveCamera;

	// Use this for initialization
	void Start () {
		if(null == ViveCamera)
        {
            ViveCamera = GameObject.Find("Camera (eye)").GetComponent<Camera>();
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    /// <summary>
    /// Calculates the absolute size in px of an object on the Vive sceen
    /// </summary>
    /// <param name="go">GameObject which size is to be determined</param>
    /// <returns>absolute size in px of an object on the Vive sceen</returns>
    public Vector2 getAbsolutePxSize(GameObject go)
    {
        Bounds goBounds = go.GetComponentInChildren<Renderer>().bounds;
        List<Vector3> minMaxValues = new List<Vector3>();

        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z + goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x - goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z + goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y + goBounds.extents.y, goBounds.center.z + goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z - goBounds.extents.z));
        minMaxValues.Add(new Vector3(goBounds.center.x + goBounds.extents.x, goBounds.center.y - goBounds.extents.y, goBounds.center.z + goBounds.extents.z));

        Debug.Log(goBounds.ToString());

        float maxX = 0, maxY = 0, minX = Screen.width, minY = Screen.height;

        foreach (Vector3 x in minMaxValues)
        {
            
            Vector3 screenPoint = ViveCamera.WorldToScreenPoint(go.transform.TransformPoint(x));
            if (screenPoint.x < minX) minX = screenPoint.x;
            if (screenPoint.x > maxX) maxX = screenPoint.x;
            if (screenPoint.y < minY) minY = screenPoint.y;
            if (screenPoint.y > maxY) maxY = screenPoint.y;
        }

        return new Vector2(maxX-minX,maxY-minY);
    }


}
