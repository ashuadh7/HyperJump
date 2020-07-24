using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerHandler : MonoBehaviour
{
    public SteamVR_Action_Boolean SetSphere;
    public SteamVR_Action_Boolean CalibrateLeaning;
    public SteamVR_Action_Boolean ResetPosition;

    private SteamVR_Action_Pose pose;
    private Collider isColliding;

    void Start()
    {
        SetSphere.AddOnStateUpListener(TriggerDown, SteamVR_Input_Sources.Any);
        CalibrateLeaning.AddOnStateUpListener(Calibrate, SteamVR_Input_Sources.Any);
        ResetPosition.AddOnStateUpListener(Reset, SteamVR_Input_Sources.Any);
        pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("MySet", "RightPose");
        isColliding = null;
    }

    public void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (isColliding == null)
        {
            Vector3 position = pose.GetLocalPosition(fromSource);
            CoucheronManager.Instance.AddSphere(position);
        }   
        else
        {
            CoucheronManager.Instance.HandleTriggerOnCollision(isColliding);
        }
    }

    public void Calibrate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("Calibrate leaning position...");
        GameObject.Find("[CameraRig]").GetComponent<LocomotionControl>().CalibrateLeaningKS();
    }

    public void Reset(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("Reset player position...");
        GameObject.Find("[CameraRig]").transform.SetPositionAndRotation(new Vector3(-45, 8.3f, 27), Quaternion.identity);
    }

    private void OnTriggerEnter(Collider other)
    {
        isColliding = other;
    }

    private void OnTriggerExit(Collider other)
    {
        isColliding = null;
        
    }
}
