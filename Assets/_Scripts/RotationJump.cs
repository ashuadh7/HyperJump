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

    [Tooltip("This option enables continues rotation between two jumps.")]
    public bool enableContinuesRotationBetweenJumps;

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

    private float _saturationTimer;
    

    // -1 jump left, 0 center, 1 right
    private float _reltativDistanceToJump;


    // Start is called before the first frame update
    void Start()
    {
        _saturationTimer = maxSaturationTime;
        _reltativDistanceToJump = 0;
    }

    // Update is called once per frame
    void Update()
    {
        float rotation = GetHeadRotation();
        _reltativDistanceToJump = CalculateRelativeDistanceToJumpRotation(rotation);

        DoContinuesRotation();
        
        if (_saturationTimer < 0)
        {
            DoJumpRotation(rotation);
        }
        else
        {
            _saturationTimer -= Time.deltaTime;
        }   
    }

    public float GetRelativDistanceToJump()
    {
        return Mathf.Sign(_reltativDistanceToJump) * (Mathf.Pow(1000, Mathf.Abs(_reltativDistanceToJump)) - 1) / (1000 - 1);
    }

    private float GetHeadRotation()
    {
        // user roll or yaw?
        if (!useRollInstead)
        {
            return GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.y % 360;
        }
        else
        {
            return (360 - GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.z) % 360;
        }
    }

    private float CalculateRelativeDistanceToJumpRotation(float rotation)
    {
        float res;
        
        // calculate relative distance to jump
        if (rotation < 180)
        {
            res = rotation / angleThreshold;
        }
        else
        {
            res = -(360 - rotation) / angleThreshold;
        }

        return Mathf.Clamp(res, -1, 1);
    }

    private void DoContinuesRotation()
    {
        if (enableContinuesRotationBetweenJumps || _saturationTimer < 0)
        {
            // continious rotation right
            if (_reltativDistanceToJump > 1 - ratioOfContinuesRotation)
            {
                this.transform.Rotate(Vector3.up, 10.0f * speedFactorOfContinuesRotation * Time.deltaTime * ((_reltativDistanceToJump - ratioOfContinuesRotation) * 1 / ratioOfContinuesRotation));

            }

            //continious rotation left
            if (_reltativDistanceToJump < -1 + ratioOfContinuesRotation)
            {
                this.transform.Rotate(Vector3.up, -10.0f * speedFactorOfContinuesRotation * Time.deltaTime * ((-_reltativDistanceToJump - ratioOfContinuesRotation) * 1 / ratioOfContinuesRotation));

            }
        }
    }

    private void DoJumpRotation(float rotation)
    {
        // jump rotation right
        if (_reltativDistanceToJump == 1)
        {
            if (enableAudioFeedback)
            {
                GetComponent<AudioSource>().Play();
            }

            float overshootInDegree = rotation - angleThreshold;
            float multiplyer = 1;
            if (enableIncreasingAngle)
            {
                multiplyer += overshootInDegree / jumpDoulblingAngle;
            }
            this.transform.Rotate(Vector3.up, defaultJumpSize * multiplyer);

            multiplyer = 1;
            if (enableDecreasingTime)
            {
                multiplyer += overshootInDegree / timeHalfingAngle;
            }
            _saturationTimer = maxSaturationTime / multiplyer;

        }
        // jump rotation left
        else if (_reltativDistanceToJump == -1)
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
            _saturationTimer = maxSaturationTime / multiplyer;
        }
    }
}
