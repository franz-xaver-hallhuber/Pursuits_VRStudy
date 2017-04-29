using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LagScript : MonoBehaviour {

	public void blink()
    {
        StartCoroutine(doBlink());
    }

    private IEnumerator doBlink()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        yield return new WaitForSeconds(0.2f);
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
}
