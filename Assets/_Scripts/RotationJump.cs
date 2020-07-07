using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RotationJump : MonoBehaviour
{
    #region Public Fields
    [Header("Jump Settings")]
    [Tooltip("Enables audio feedback for jumping.")]
    public bool enableAudioFeedback;

    [Range(0f, 1f)]
    public float ratioOfContinuesRotation;

    public float speedFactorOfContinuesRotation;

    [Tooltip("Changes the speed of forward and backward translation.")]
    [Range(0f, 20f)]
    public float translationSpeedFactor;

    public float angleThreshold;

    [Tooltip("Use head roll instead of head yaw.")]
    public bool useRollInstead;

    [Tooltip("Defines the default, unmodified size of a jump rotation in degree.")]
    [Range(0f, 90f)]
    public float defaultJumpSize;

    [Tooltip("Defines the minimal time between two jumps.")]
    public float maxSaturationTime;

    [Header("Jump Size Increases")]
    [Tooltip("Enables the jump size to increase being futher over the threshold.")]
    public bool enableIncreasingAngle;

    [Tooltip("When increasing angle is activ this gives the amount of difference in degree that effectivly doubles the jump size.")]
    public float jumpDoulblingAngle;

    [Header("Jump Time Decreases")]
    [Tooltip("Enables the jump saturation time to dencrease being futher over the threshold.")]
    public bool enableDecreasingTime;

    [Tooltip("When dencreasing saturation time is activ this gives the amount of difference in degree nessesary to effectivly halfen the saturation time.")]
    public float timeHalfingAngle;
    #endregion

    private float saturationTimer;
    private SteamVR_Action_Vector2 axis;

    // -1 jump left, 0 center, 1 right
    private float reltativDistanceToJump;


    // Start is called before the first frame update
    void Start()
    {
        saturationTimer = maxSaturationTime;
        reltativDistanceToJump = 0;
        axis = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("MySet", "Throttle");

    }

    // Update is called once per frame
    void Update()
    {
        // virtual translation
        this.transform.position += this.transform.forward * axis.GetAxis(SteamVR_Input_Sources.Any).y * Time.deltaTime * translationSpeedFactor;

        float rotation;

        // user roll or yaw?
        if (!useRollInstead)
        {
            rotation = GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.y % 360;
        }
        else
        {
            rotation = (360 - GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.z) % 360;
        }

        // clculate relative distance to jump
        if (rotation < 180)
        {
            reltativDistanceToJump = rotation / angleThreshold;
        }
        else if (rotation > 180)
        {
            reltativDistanceToJump = -(360 - rotation) / angleThreshold;
        }
        reltativDistanceToJump = Mathf.Clamp(reltativDistanceToJump, -1, 1);

        // continious rotation right
        if (reltativDistanceToJump > 1 - ratioOfContinuesRotation)
        {
            this.transform.Rotate(Vector3.up, 10.0f * speedFactorOfContinuesRotation * Time.deltaTime * ((reltativDistanceToJump - ratioOfContinuesRotation) * 1 / ratioOfContinuesRotation));

        }

        //continious rotation left
        if (reltativDistanceToJump < -1 + ratioOfContinuesRotation)
        {
            this.transform.Rotate(Vector3.up, -10.0f * speedFactorOfContinuesRotation * Time.deltaTime * ((-reltativDistanceToJump - ratioOfContinuesRotation) * 1 / ratioOfContinuesRotation));

        }

        // virtual rotation
        if (saturationTimer < 0)
        {
            // jump rotation right
            if (reltativDistanceToJump == 1)
            { 
                if(enableAudioFeedback)
                {
                    GetComponent<AudioSource>().Play();
                }
                
                float overshootInDegree = rotation - angleThreshold;
                float multiplyer = 1;
                if(enableIncreasingAngle)
                {
                    multiplyer += overshootInDegree / jumpDoulblingAngle;
                }
                this.transform.Rotate(Vector3.up, defaultJumpSize * multiplyer);

                multiplyer = 1;
                if (enableDecreasingTime)
                {
                    multiplyer += overshootInDegree / timeHalfingAngle;
                }
                saturationTimer = maxSaturationTime / multiplyer;
                
            }
            // jump rotation left
            else if(reltativDistanceToJump == -1)
            {
                if (enableAudioFeedback)
                {
                    GetComponent<AudioSource>().Play();
                }

                float overshootInDegree = 360 - angleThreshold - rotation;
                float multiplyer = 1;
                if (enableIncreasingAngle)
                {
                    multiplyer += overshootInDegree / jumpDoulblingAngle;
                }
                this.transform.Rotate(Vector3.up, -defaultJumpSize * multiplyer);

                multiplyer = 1;
                if (enableDecreasingTime)
                {
                    multiplyer += overshootInDegree / timeHalfingAngle;
                }
                saturationTimer = maxSaturationTime / multiplyer;
            }
        }
        else
        {
            saturationTimer -= Time.deltaTime;
        }   
    }

    public float GetRelativDistanceToJump()
    {
        return Mathf.Sign(reltativDistanceToJump) * (Mathf.Pow(1000, Mathf.Abs(reltativDistanceToJump)) - 1) / (1000 - 1);
    }
}
