using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerHandler : MonoBehaviour
{
    public SteamVR_Action_Boolean _calibration;
    public SteamVR_Action_Boolean _resetPosition;

    void Start()
    {
        _calibration.AddOnStateDownListener(StartCalibration, SteamVR_Input_Sources.Any);
        _calibration.AddOnStateUpListener(FinishCalibration, SteamVR_Input_Sources.Any);
        _resetPosition.AddOnStateDownListener(Reset, SteamVR_Input_Sources.Any);
    }

    // the calibration procedure is as follows:
    // 1) press the buttion an look to the left or right only using head yaw and try to be stationary else
    // 2) keep the buttion pressed and look to the other side
    // 3) keep the buttion pressed, look forward in a comfortable leaning position and release
    public void StartCalibration(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (!GameObject.Find("LocomotionPlatform").GetComponent<LocomotionMethodInterface>().GetInputSource().IsInitialized())
        {
            GameObject.Find("LocomotionPlatform").GetComponent<LocomotionMethodInterface>().GetInputSource().StartCalibration();
        }
    }

    public void FinishCalibration(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (!GameObject.Find("LocomotionPlatform").GetComponent<LocomotionMethodInterface>().GetInputSource()
            .IsInitialized())
        {
            GameObject.Find("LocomotionPlatform").GetComponent<LocomotionMethodInterface>().GetInputSource().EndCalibration();
        }
    }

    public void Reset(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        GeneralLocomotionSettings.Instance.ResetPlayerPosition();
    }
}
