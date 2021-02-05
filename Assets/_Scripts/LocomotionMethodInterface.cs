using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LocomotionMethodInterface : MonoBehaviour
{
    protected LocomotionInputAdapterInterface _locomotionInput;

    public LocomotionInputAdapterInterface GetInputSource()
    {
        return _locomotionInput;
    }
}
