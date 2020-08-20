using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullBodyBasedSpeedAdaptive : MonoBehaviour
{
    #region Public Fields

    [Header("Method Settings")]
    [Tooltip("Changes the speed of forward and backward translation.")]
    public float _translationSpeedFactor;

    [Tooltip("Gives the point in percent of the maximum speed where strafing is replaced by rotating.")]
    [Range(0f, 1f)]
    public float _velocityThesholdForInterfaceSwitch;

    [Tooltip("Gives the maximum rotational speed in degree per second.")]
    public float _maxRotationSpeed;

    [Tooltip("When this mode is enabled the rotational speed is reduced with increasing travel speed.")]
    public bool _enableMotorcycleMode;

    [Header("Rotational Jumping")]
    public bool _enableRotationalJumping;

    [Tooltip("Defines the minimal time between two jumps.")]
    public float _maxSaturationTime;

    [Tooltip("Defines the default, unmodified size of a jump rotation in degree.")]
    [Range(0f, 90f)]
    public float defaultJumpSize;

    // the jumping threshold is given by rotational degree per second, this should make the threshold independent of the method used but
    // (be carefull) dependet of the transfer function
    [Tooltip("This effectivly overrides the maximum rotational speed, when the latter is larger than this one, it is never reached.")]
    public float _rotationalJumpingThresholdDegreePerSecond;

    [Header("Decreasing Jump Time")]
    [Tooltip("Enables the jump saturation time to dencrease being futher over the threshold.")]
    public bool _enableDecreasingSaturationTime;

    [Tooltip("When decreasing saturation time is activ this gives the increase of rotational speed above the threshold to effectivly halfen the saturation time.")]
    public float _timeDecreasingRotationalSpeedOvershoot;

    #endregion

    private float _jumpSaturationTimer;
    private float _relDistanceToJump = 1.0f;

    private void Start()
    {
        _jumpSaturationTimer = _maxSaturationTime;
    }

    void FixedUpdate()
    {
        if(GetComponent<LocomotionControl>().GetHeadJoint() != null)
        {
            Rotate();
        } 
        if(!GetComponent<LocomotionControl>().isBreaked())
        {
            Translate();
        }    
    }

    public float GetRelativeDistanceToJump()
    {
        return _relDistanceToJump;
    }

    private void Translate()
    {
        this.transform.position += this.transform.forward * GetComponent<LocomotionControl>().Get2DLeaningAxis().y * Time.deltaTime * _translationSpeedFactor;

        // TODO smooth transition into this
        // when slow enough leaning controlles strafing
        if (GetComponent<LocomotionControl>().Get2DLeaningAxis().y < _velocityThesholdForInterfaceSwitch)
        {
            this.transform.position += this.transform.right * GetComponent<LocomotionControl>().Get2DLeaningAxis().x * Time.deltaTime * _translationSpeedFactor;
        }

    }

    private void Rotate()
    {
        _jumpSaturationTimer -= Time.deltaTime;
        float angle = _maxRotationSpeed * Time.deltaTime;
        
        // TODO smooth transitions between the two modi
        // when fast enough leaning controlles rotation
        if (GetComponent<LocomotionControl>().Get2DLeaningAxis().y >= _velocityThesholdForInterfaceSwitch)
        {
            // leaning faster to the sides results in faster yaw rotation
            angle *= GetComponent<LocomotionControl>().Get2DLeaningAxis().x;
            
            // for faster tavel speeds rotation speed is increased;
            if(_enableMotorcycleMode)
            {
                angle *= (1.0f - GetComponent<LocomotionControl>().Get2DLeaningAxis().y);
            }  
        }
        // when slower it is the head yaw only
        else
        {
            angle *= GetComponent<LocomotionControl>().GetHeadYawAxis();
        }

        float signedAnglePerSecond = angle / Time.deltaTime;

        // calculate distance to jump for the feedback
        _relDistanceToJump = Mathf.Clamp(Mathf.Abs(signedAnglePerSecond) / _rotationalJumpingThresholdDegreePerSecond, 0, 1);
        if(!_enableRotationalJumping)
        {
            // allways max thus the vignette is not there in this case, such as in case of a jump
            _relDistanceToJump = 1.0f;
        }

        // finally aplly the rotation
        if (_enableRotationalJumping &&
           Mathf.Abs(signedAnglePerSecond) > _rotationalJumpingThresholdDegreePerSecond &&
           _jumpSaturationTimer < 0)
        {
            this.transform.RotateAround(GetComponent<LocomotionControl>().GetHeadJoint().transform.position, Vector3.up, defaultJumpSize * Mathf.Sign(signedAnglePerSecond));

            // reset saturation time
            float timeModifyer = 1;
            if (_enableDecreasingSaturationTime)
            {
                timeModifyer += (Mathf.Abs(signedAnglePerSecond) - _rotationalJumpingThresholdDegreePerSecond) / _timeDecreasingRotationalSpeedOvershoot;
            }
            _jumpSaturationTimer = _maxSaturationTime / timeModifyer;
        }
        else
        {
            this.transform.RotateAround(GetComponent<LocomotionControl>().GetHeadJoint().transform.position, Vector3.up, angle);
        }    
    }
}
