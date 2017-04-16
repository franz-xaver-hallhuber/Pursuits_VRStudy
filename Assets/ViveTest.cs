using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ViveTest : MonoBehaviour {

    public GameObject plane;
    public GameObject ViveCamera;
    public float tolerance;

    private Vector2 min, max;
    private Quarters q;

    // Use this for initialization
    void Start()
    {
        HmdQuad_t rect = new HmdQuad_t();
        SteamVR_PlayArea.GetBounds(SteamVR_PlayArea.Size.Calibrated, ref rect);

        List<float> x, z;
        x = new List<float>(new float[] { rect.vCorners0.v0, rect.vCorners1.v0, rect.vCorners2.v0, rect.vCorners3.v0 });
        z = new List<float>(new float[] { rect.vCorners0.v2, rect.vCorners1.v2, rect.vCorners2.v2, rect.vCorners3.v2 });

        x.Sort();
        z.Sort();

        max = new Vector2(x[x.Count - 1], z[z.Count - 1]);
        min = new Vector2(x[0], z[0]);

        q = new Quarters(max, min);
    }
	
	// Update is called once per frame
	void Update () {
		if (reachedDestination())
        {
            Vector2 newXZ = q.getNextPosition(new Vector2(plane.transform.position.x, plane.transform.position.z));
            plane.transform.position = new Vector3(newXZ.x, 0, newXZ.y);
        }
    }

    private bool reachedDestination()
    {
        return (Vector2.Distance(
            new Vector2(ViveCamera.transform.position.x, ViveCamera.transform.position.z),
            new Vector2(plane.transform.position.x, plane.transform.position.z)) <= tolerance);
    }
}

public class Quarters
{
    Vector2 start, middle, end;
    float maxDistance;

    public Quarters(Vector2 max, Vector2 min)
    {
        start = new Vector2(min.x, min.y);
        middle = new Vector2(max.x - min.x / 2, max.y - min.y / 2);
        end = new Vector2(max.x, max.y);
        maxDistance = Mathf.Abs(Vector2.Distance(min, max));
    }

    public Vector2 getNextPosition(Vector2 last)
    {
        Vector2 _res = new Vector2();

        _res = last + UnityEngine.Random.Range(maxDistance / 2, maxDistance) * UnityEngine.Random.insideUnitCircle;

        //_res.x = (_res.x < start.x ? _res.x + end.x - start.x : (_res.x > end.x ? _res.x - end.x + start.x : _res.x));
        //int xFactor = (_res.x < start.x ? 1 : (_res.x > end.x ? -1 : 0));
        //int yFactor = (_res.y < start.y ? 1 : (_res.y > end.y ? -1 : 0));
        //_res.x = _res.x + xFactor * (end.x - start.x);
        //_res.y = _res.x + yFactor * (end.y - start.y);

        //if (last.x < middle.x) _res.x = UnityEngine.Random.Range(middle.x, end.x);
        //else _res.x = UnityEngine.Random.Range(start.x, middle.x);
        //if (last.y < middle.y) _res.y = UnityEngine.Random.Range(middle.y, end.y);
        //else _res.y = UnityEngine.Random.Range(start.y, middle.y);

        if (_res.x < start.x || _res.x > end.x) _res.x = start.x + (Mathf.Abs(_res.x - start.x) % (end.x - start.x));
        if (_res.y < start.y || _res.y > end.y) _res.y = start.y + (Mathf.Abs(_res.y - start.y) % (end.y - start.y));

        return _res;
    }
    
}
