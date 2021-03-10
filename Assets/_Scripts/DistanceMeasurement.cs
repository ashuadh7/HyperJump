using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.UI;
public class DistanceMeasurement : MonoBehaviour
{
    [SerializeField]
    private SteamVR_Action_Vector2 dash;
    [SerializeField]
    private GameObject player;
    [SerializeField]
    private GameObject cameraEye;
    [SerializeField]
    private GameObject cone;

    public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.GetBooleanAction("InteractionUI");
    public SteamVR_Behaviour_Pose pose;
    [SerializeField]
    private GameObject UI;
    [SerializeField]
    private GameManager gameManager;
    private Text txt;
    [SerializeField]
    private float multiplier;
    private bool first;
    private float _distanceEstimate;
    public float distanceEstimate
    {
        get {return _distanceEstimate;}
    }
    private bool _distanceMeasurementStart;
    public bool distanceMeasurementStart
    {
        get{return _distanceMeasurementStart;}
    }
    // Start is called before the first frame update
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
        _distanceEstimate = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (interactWithUI != null && interactWithUI.GetState(pose.inputSource))
        {
            // Debug.Log("Trigger Pressed");
            _distanceMeasurementStart = true;
            UI.SetActive(true);
            _distanceEstimate = (cone.transform.position - this.transform.position).magnitude;
            // Debug.Log("Distance Estimate: " + _distanceEstimate);
            if (gameManager.showDistanceMeasurement && _distanceEstimate < 10)
            {
                txt.text = ((cone.transform.position - this.transform.position).magnitude).ToString("F1") + "m";
            }
            else if (gameManager.showDistanceMeasurement && _distanceEstimate > 10)
            {
                txt.text = ((cone.transform.position - this.transform.position).magnitude).ToString("F0") + "m";

            }
            else
            {
                txt.text = "";
            }            
            Vector2 m_MoveValue = dash.GetAxis(SteamVR_Input_Sources.RightHand);
            if (distanceEstimate < 25)
            {                
                cone.transform.position += this.transform.forward * m_MoveValue.y*.25f;
            }
            else
            {                
                cone.transform.position += this.transform.forward * m_MoveValue.y*.5f;
            }
            first = true;
        }
        else
        {
            _distanceMeasurementStart = false;
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
            float up = m_MoveValue.y * this.transform.forward.y;
        }
        
    }
}
