using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.UI;

public class Translate : MonoBehaviour
{
    public SteamVR_Action_Vector2 dash;
    public GameObject player;
    public GameObject cameraEye;
    public GameObject cone;
    public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.GetBooleanAction("InteractUI");
    public SteamVR_Behaviour_Pose pose;
    public GameObject UI;
    Text txt;
    public float multiplier;
    private bool first;
    public bool distanceMeasurementStart = false;
    void Start()
    {
        if (pose == null)
            pose = this.GetComponent<SteamVR_Behaviour_Pose>();
        if (pose == null)
            Debug.LogError("No SteamVR_Behaviour_Pose component found on this object", this);

        if (interactWithUI == null)
            Debug.LogError("No ui interaction action has been set on this component.", this);
        txt = UI.GetComponent<Text>();
        first = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (interactWithUI != null && interactWithUI.GetState(pose.inputSource))
        {
            distanceMeasurementStart = true;
            UI.SetActive(true);
            txt.text = ((cone.transform.position - this.transform.position).magnitude).ToString("F1") + "m";
            Vector2 m_MoveValue = dash.GetAxis(SteamVR_Input_Sources.RightHand);
            cone.transform.position += this.transform.forward * m_MoveValue.y*.25f;
            first = true;
        }
        else
        {
            distanceMeasurementStart = false;
            UI.SetActive(false);
            if (first)
            {
                cone.transform.position = this.transform.position;
                first = false;
            }
            
            if ((cone.transform.position - this.transform.position).magnitude<multiplier)
            {
                cone.transform.position += this.transform.forward * multiplier;
            }
            Vector2 m_MoveValue = dash.GetAxis(SteamVR_Input_Sources.RightHand);
            float forward = m_MoveValue.y * this.transform.forward.x + m_MoveValue.x* this.transform.forward.z;
            float strafe = m_MoveValue.y * this.transform.forward.z - m_MoveValue.x* this.transform.forward.x;  
            // - m_MoveValue.x * this.transform.forward.z;
            // float _strafe = m_MoveValue.y * this.transform.forward.z + m_MoveValue.x * this.transform.forward.x;
            float up = m_MoveValue.y * this.transform.forward.y;
            player.transform.position += new Vector3(forward, up, strafe);
            player.transform.position += this.transform.forward * m_MoveValue.y;
        }
        
    }
}
