using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMyNumber : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Renderer sh = GetComponent<Renderer>();
        Material ma = sh.material;
        ma.color = Color.blue;
        ma.mainTexture = CreateNumberTexture.getNumberTexture(5, true);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
