using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ScreenScript : MonoBehaviour {

    public void setDigit(int d)
    {
        Texture2D _new = new Texture2D(256, 256);
        _new.LoadImage(File.ReadAllBytes("./Assets/Textures/pin" + (d+1) + ".png"), true);
        GetComponent<Renderer>().material.mainTexture = _new;
    }

    public void error()
    {
        Texture2D _new = new Texture2D(256, 256);
        _new.LoadImage(File.ReadAllBytes("./Assets/Textures/error.png"), true);
        GetComponent<Renderer>().material.mainTexture = _new;
    }

    public void ok()
    {
        Texture2D _new = new Texture2D(256, 256);
        _new.LoadImage(File.ReadAllBytes("./Assets/Textures/correct.png"), true);
        GetComponent<Renderer>().material.mainTexture = _new;
    }

    public void Reset()
    {
        Texture2D _new = new Texture2D(256, 256);
        _new.LoadImage(File.ReadAllBytes("./Assets/Textures/pin0.png"), true);
        GetComponent<Renderer>().material.mainTexture = _new;
    }
}
