using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATMScript : MonoBehaviour {

    public GameObject eyeCam, screen, money;
    public float tolerance;
    Vector3 hidePosition;
    // Use this for initialization
    void Start () {
        hidePosition = money.transform.position;
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

    public void dispense()
    {
        StartCoroutine(dispenseMoney());
    }

    public void reset()
    {
        money.transform.position = hidePosition;
    }

    private IEnumerator dispenseMoney()
    {
        bool _animationDone = false;
        
        while (!_animationDone)
        {
            Vector3 _old = money.transform.position;
            money.transform.position = new Vector3(_old.x, _old.y, _old.z + Time.deltaTime * 0.05f);
            if (_old.z <= 1.16f) _animationDone = true;
            yield return null;
        }
    }
}
