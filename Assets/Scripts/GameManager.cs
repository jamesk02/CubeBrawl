using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkBehaviour
{
    private float sliderVal = 0.1f;

    public void SetScaleSliderVal(float val)
    {
        sliderVal = val;
    }

    public float GetSliderVal()
    {
        if (sliderVal > 0.05f)
        {
            return sliderVal;
        }
        else
        {
            return 0.05f;
        }
    }
}
