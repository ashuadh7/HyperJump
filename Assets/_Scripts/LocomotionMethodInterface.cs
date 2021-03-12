using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LocomotionMethodInterface : MonoBehaviour
{
    protected LocomotionInputAdapterInterface _locomotionInput;

    // internal states for logging
    [HideInInspector] public bool _STATE_jumpedThisFrame = false;
    [HideInInspector] public float _STATE_distanceLastJump = 0f;
    [HideInInspector] public bool _STATE_rotationalJumpThisFrame = false;
    [HideInInspector] public float _STATE_angleOfVirtualRotationThisFrame = 0f;
    
    public LocomotionInputAdapterInterface GetInputSource()
    {
        return _locomotionInput;
    }

    public abstract void SetBreak(bool val);
    public abstract bool GetBreak();
    public abstract void SetTeleport(bool val);

    public abstract float GetSpeedAxis();
}
