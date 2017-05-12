using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

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
        //Debug.Log(vd.ScreenSizeInDeg(cube));

        Vector3 center = cube.GetComponent<CircularMovement>().localCenter;
        float radius = cube.GetComponent<CircularMovement>().radius;

        Vector3 minLocal = new Vector3(center.x - radius, center.y, center.z);
        Vector3 maxLocal = new Vector3(center.x + radius, center.y, center.z);

        Vector3 minWorld = camera.transform.TransformPoint(minLocal);
        Vector3 maxWorld = camera.transform.TransformPoint(maxLocal);

        //float widthInWorld = vd.radiusWidthInDeg(minLocal, maxLocal);

        // Debug.Log("center: " + center + "radius: " + radius + " minLocal: " + minLocal + "maxLocal: " + maxLocal + " minWorld: " + minWorld + " maxWorld " + maxWorld + " degrees: " + widthInWorld);
       Debug.Log("Cube visible " + vd.amIOffScreen(cube.transform.localPosition));

        HmdQuad_t rect = new HmdQuad_t();
        SteamVR_PlayArea.GetBounds(SteamVR_PlayArea.Size.Calibrated, ref rect);
        
    }
}
