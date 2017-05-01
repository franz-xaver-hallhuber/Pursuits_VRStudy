using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.VR;

public class WalkingTask : MonoBehaviour {

    public GameObject plane;
    public GameObject ViveCamera;
    public float walkingTolerance;

    private Vector2 min, max;
    private Quarters q;
    private StreamWriter walkingWriter;

    public bool _shouldStop { get; set; }

    private void OnDestroy()
    {
        walkingWriter.Close();
    }

    public void Init(string logFolder)
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

        walkingWriter = new StreamWriter(logFolder + @"\log_Walking_" + DateTime.Now.ToString("ddMMyy_HHmmss") + ".csv", true);
        walkingWriter.WriteLine("Timestamp;taskTime;taskDistance;taskVelocity");
    }
	
	// Update is called once per frame
	public IEnumerator RunWalkingTask () {
        plane.GetComponent<MeshRenderer>().enabled = true;
        TimeSpan _startTask = PupilGazeTracker.Instance._globalTime;
        Vector2 _currentDest = Vector2.one;
        Vector2 _lastDest = new Vector2(plane.transform.position.x, plane.transform.position.z);

        while (!_shouldStop)
        {
            if (reachedDestination())
            {
                walkingWriter.WriteLine(PupilGazeTracker.Instance._globalTime.TotalSeconds + ";"
                    + (PupilGazeTracker.Instance._globalTime - _startTask).TotalSeconds + ";"
                    + Vector2.Distance(_currentDest, _lastDest));
                _currentDest = q.getNextPosition(new Vector2(plane.transform.position.x, plane.transform.position.z));
                _lastDest = new Vector2(plane.transform.position.x, plane.transform.position.z);
                plane.transform.position = new Vector3(_currentDest.x, 0, _currentDest.y);
                _startTask = PupilGazeTracker.Instance._globalTime;
            }
            yield return null;
        }
        
    }

    private bool reachedDestination()
    {
        return (Vector2.Distance(
            new Vector2(ViveCamera.transform.position.x, ViveCamera.transform.position.z),
            new Vector2(plane.transform.position.x, plane.transform.position.z)) <= walkingTolerance);
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

    /// <summary>
    /// Calculates a new position with a distance at least half the value of the Vive play area diagonal to the last position
    /// </summary>
    /// <param name="last">The object's last position</param>
    /// <returns>The object's new position</returns>
    public Vector2 getNextPosition(Vector2 last)
    {
        Vector2 _res = new Vector2();

        _res = last + UnityEngine.Random.Range(maxDistance / 2, maxDistance) * UnityEngine.Random.insideUnitCircle;

        if (_res.x < start.x || _res.x > end.x) _res.x = start.x + (Mathf.Abs(_res.x - start.x) % (end.x - start.x));
        if (_res.y < start.y || _res.y > end.y) _res.y = start.y + (Mathf.Abs(_res.y - start.y) % (end.y - start.y));

        return _res;
    }

    
    
}
