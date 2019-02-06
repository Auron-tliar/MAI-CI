using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//noise object
public class Noise
{
    public Vector2 pos;
    public float intencity;

    public Noise(Vector2 pos, float intencity)
    {
        this.pos = pos;
        this.intencity = intencity;
    }

    public override string ToString()
    {
        return (pos.ToString() + ": " + intencity.ToString());
    }
}
