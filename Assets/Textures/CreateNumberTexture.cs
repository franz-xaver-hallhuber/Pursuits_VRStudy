﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CreateNumberTexture : MonoBehaviour {

	public static Texture2D getNumberTexture(int number, bool transparent)
    {
        Texture2D _ret = new Texture2D(256, 256);
        _ret.LoadImage(File.ReadAllBytes("./Assets/Textures/" + number + (transparent ? "t" : "") + ".png"));
        return _ret;
    }
}
