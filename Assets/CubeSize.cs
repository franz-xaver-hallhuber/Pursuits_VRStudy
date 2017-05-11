using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSize : MonoBehaviour {

    public GameObject cube, camera, indicator;
    Rect gui;
    VisualDegrees vd;

    void Start()
    {
        vd = new VisualDegrees();
        vd.Init(99, camera.GetComponent<Camera>());
    }

    // Update is called once per frame
    void Update () {
        Debug.Log(vd.ScreenSizeInDeg(cube));
    }
}
