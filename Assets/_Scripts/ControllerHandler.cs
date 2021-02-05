using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerHandler : MonoBehaviour
{
    public SteamVR_Action_Boolean _calibrattion;
    public SteamVR_Action_Boolean _resetPosition;
    public SteamVR_Action_Boolean _break;
    private bool _calibrated = false;
    public bool calibrated
    {
        get {return _calibrated;}
    }

    void Start()
    {
        _calibrattion.AddOnStateDownListener(StartCalibration, SteamVR_Input_Sources.Any);
        _calibrattion.AddOnStateUpListener(FinishCalibration, SteamVR_Input_Sources.Any);
        _resetPosition.AddOnStateDownListener(Reset, SteamVR_Input_Sources.Any);
        _break.AddOnUpdateListener(Break, SteamVR_Input_Sources.Any);
    }

    // the calibration procedure is as follows:
    // 1) press the buttion an look to the left or right only using head yaw and try to be stationary else
    // 2) keep the buttion pressed and look to the other side
    // 3) keep the buttion pressed, look forward in a comfortable leaning position and release
    public void StartCalibration(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("Started calibration...");
        GameObject.Find("LocomotionPlatform").GetComponent<LocomotionControl>().StartCenterOfRotationCalibration();
    }

    public void FinishCalibration(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        GameObject.Find("LocomotionPlatform").GetComponent<LocomotionControl>().FinishCenterOfRotationCalibration();
        GameObject.Find("LocomotionPlatform").GetComponent<LocomotionControl>().CalibrateLeaningKS();     
        Debug.Log("Finished calibration...");
        _calibrated = true;
    }

    public void Reset(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("Reset player position...");
        GameObject.Find("LocomotionPlatform").transform.SetPositionAndRotation(new Vector3(-45, 9.3f, 27), Quaternion.identity);
    }


    public void Break(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        GameObject.Find("LocomotionPlatform").GetComponent<LocomotionControl>().UpdateBrake(newState);
    }
}
