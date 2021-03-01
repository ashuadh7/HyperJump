using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerAdapter : LocomotionInputAdapterInterface
{
    void Update()
    {
        UpdateInputs();
        NormalizeInputsToAxes();
        ApplyTransferFunctionsToAxes();
        ApplyDeadzonesToAxes();
    }
    
    public override Vector2 GetDirectionAxes()
    {
        throw new System.NotImplementedException();
    }

    public override void StartCalibration(){}
    public override void EndCalibration(){}
}
