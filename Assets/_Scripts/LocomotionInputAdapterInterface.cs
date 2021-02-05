using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class LocomotionInputAdapterInterface : MonoBehaviour
{
    protected bool _isInitialized = false;

    public abstract Vector2 GetDirectionAxes();

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    public abstract void StartCalibration();
    public abstract void EndCalibration();
}
