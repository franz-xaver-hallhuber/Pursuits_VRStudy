using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trial
{
    public float trajectoryRadius { get; set; }
    public float objectSize { get; set; }
    
    public Trial (float radius, float size)
    {
        trajectoryRadius = radius;
        objectSize = size;
    }
}

public class StudyMaster2000 : MonoBehaviour {

    public int numberOfObjects;
    public float[] radii, sizes;

    private List<Trial> combinations;

	// Use this for initialization
	void Start () {
        // for the start just create a list with all combinations in random order
        foreach (float f in radii)
        {
            foreach (float g in sizes)
            {
                combinations.Add(new Trial(f, g));
            }
        }

        Random ran = new Random();
        int n = combinations.Count;
        while (n>1) {

        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnGUI()
    {

    }
}
