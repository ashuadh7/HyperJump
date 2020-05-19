using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerHandler : MonoBehaviour
{
    public SteamVR_Action_Boolean SetSphere;
    public SteamVR_Input_Sources handType;
    public GameObject framingObject;

    private SteamVR_Action_Pose pose;
    private Collider isColliding;

    void Start()
    {
        SetSphere.AddOnStateUpListener(TriggerDown, handType);
        pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("default", "Pose");
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

    private void OnTriggerEnter(Collider other)
    {
        isColliding = other;
    }

    private void OnTriggerExit(Collider other)
    {
        isColliding = null;
        
    }
}
