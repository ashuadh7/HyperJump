using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerHandler : MonoBehaviour
{
    public SteamVR_Action_Boolean _setSphere;
    public SteamVR_Action_Boolean _calibrattion;
    public SteamVR_Action_Boolean _resetPosition;
    public SteamVR_Action_Boolean _break;

    private SteamVR_Action_Pose _pose;
    private Collider _isColliding;

    void Start()
    {
        _setSphere.AddOnStateDownListener(TriggerDown, SteamVR_Input_Sources.Any);
        _calibrattion.AddOnStateDownListener(StartCalibration, SteamVR_Input_Sources.Any);
        _calibrattion.AddOnStateUpListener(FinishCalibration, SteamVR_Input_Sources.Any);
        _resetPosition.AddOnStateDownListener(Reset, SteamVR_Input_Sources.Any);
        _break.AddOnUpdateListener(Break, SteamVR_Input_Sources.Any);
        _pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("MySet", "RightPose");
        _isColliding = null;
    }

    public void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (_isColliding == null)
        {
            Vector3 position = _pose.GetLocalPosition(fromSource);
            CoucheronManager.Instance.AddSphere(position);
        }   
        else
        {
            CoucheronManager.Instance.HandleTriggerOnCollision(_isColliding);
        }
    }

    // the calibration procedure is as follows:
    // 1) press the buttion an look to the left or right only using head yaw and try to be stationary else
    // 2) keep the buttion pressed and look to the other side
    // 3) keep the buttion pressed, look forward in a comfortable leaning position and release
    public void StartCalibration(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("Started calibration...");
        GameObject.Find("[CameraRig]").GetComponent<LocomotionControl>().StartCenterofRotationCalibration();
    }

    public void FinishCalibration(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        GameObject.Find("[CameraRig]").GetComponent<LocomotionControl>().FinishCenterofRotationCalibration();
        GameObject.Find("[CameraRig]").GetComponent<LocomotionControl>().CalibrateLeaningKS();
        Debug.Log("Finished calibration...");
    }

    public void Reset(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("Reset player position...");
        GameObject.Find("[CameraRig]").transform.SetPositionAndRotation(new Vector3(-45, 9.3f, 27), Quaternion.identity);
    }


    public void Break(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        GameObject.Find("[CameraRig]").GetComponent<LocomotionControl>().UpdateBreak(newState);
    }

    private void OnTriggerEnter(Collider other)
    {
        _isColliding = other;
    }

    private void OnTriggerExit(Collider other)
    {
        _isColliding = null;
        
    }
}
