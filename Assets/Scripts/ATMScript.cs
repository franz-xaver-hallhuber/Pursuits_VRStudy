using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATMScript : MonoBehaviour {

    public GameObject eyeCam, screen, money;
    public float tolerance;
    Vector3 hidePosition;
    bool _animationDone = false;


    private ScreenScript sc;

    // Use this for initialization
    void Start () {
        hidePosition = money.transform.position;
        sc = GetComponentInChildren<ScreenScript>();
    }
	
	// Update is called once per frame
	void Update () {
        var _distance = Vector3.Distance(eyeCam.transform.position, GetComponentInParent<Transform>().position);
        //Debug.Log("cam:" + eyeCam.transform.position + " me " + GetComponentInParent<Transform>().position + " dist " +  _distance);
		if (_distance <= tolerance)
        {
            screen.SetActive(true);
        } else
        {
            screen.SetActive(false);
        }
	}

    
    private IEnumerator dispenseMoney()
    {
        while (!_animationDone)
        {
            Vector3 _old = money.transform.position;
            money.transform.position = new Vector3(_old.x, _old.y, _old.z + Time.deltaTime * 0.04f);
            yield return null;
        }
    }

    private IEnumerator reset()
    {
        yield return new WaitForSeconds(2);
        sc.Reset();
        _animationDone = true;
        money.transform.position = hidePosition;
    }

    internal void error()
    {
        sc.error();
        StartCoroutine(reset());
    }

    internal void progress(int _currentDigit)
    {
        sc.setDigit(_currentDigit);
    }

    internal void ok()
    {
        sc.ok();
        _animationDone = false;
        StartCoroutine(dispenseMoney());
        StartCoroutine(reset());
    }
}
