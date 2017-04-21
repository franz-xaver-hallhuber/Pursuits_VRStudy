using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineTest : MonoBehaviour {
    public Light highlight;
    bool up = true;

	// Use this for initialization
	void Start () {
        StartCoroutine(Flash());
    }
	
	// Update is called once per frame
	void Update () {
        
        //if (up)
        //{
        //    GetComponent<Light>().intensity += Time.deltaTime * 8;
        //    if (GetComponent<Light>().intensity >= 8) up = false;
        //}
        //if (!up)
        //{
        //    GetComponent<Light>().intensity -= Time.deltaTime * 8;
        //    if (GetComponent<Light>().intensity <= 0) up = true;
        //}
    }

    private IEnumerator Flash()
    {
        bool up = true;
        while (up)
        {
            GetComponent<Light>().intensity += Time.deltaTime * 8 * 3;
            if (GetComponent<Light>().intensity >= 8) up = false;
            yield return null;
        }
        while (!up)
        {
            GetComponent<Light>().intensity -= Time.deltaTime * 8 * 3;
            if (GetComponent<Light>().intensity <= 0) break;
            yield return null;
        }
    }
}
