using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FullBodyBasedSpeedAdaptive : MonoBehaviour
{
    #region Public Fields

    [Header("Method Settings")]
    public bool _enabledPathPrediction;

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
    private List<GameObject> _spheres;


    private void Start()
    {
        _jumpSaturationTimer = _maxSaturationTime;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        _spheres = new List<GameObject>();

        for (int i = 0; i < 50; ++i)
        {
            _spheres.Add(Instantiate(sphere, this.transform.position, Quaternion.identity, this.transform));
        }
    }

    void FixedUpdate()
    {
        // actual travel
        if (GetComponent<LocomotionControl>().GetHeadJoint() != null)
        {
            Rotate(Time.deltaTime, this.transform, ref _jumpSaturationTimer);
        } 
        if(!GetComponent<LocomotionControl>().isBreaked())
        {
            Translate(Time.deltaTime, this.transform);
        }    
    }

    private void Update()
    {
        // travel simulation
        for (int i = 0; i < 50; ++i)
        {
            _spheres[i].SetActive(_enabledPathPrediction);
        }

        if (GetComponent<LocomotionControl>().GetHeadJoint() != null && _enabledPathPrediction)
        {
            SimulateMovement();
        }
    }

    public float GetRelativeDistanceToJump()
    {
        return _relDistanceToJump;
    }


    private void SimulateMovement()
    {
        // make a copy of the current transform, working as its prediction
        GameObject future = new GameObject();
        future.transform.SetParent(this.transform.parent);
        future.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
        float futureSaturatiuonTimer = _jumpSaturationTimer;

        for (int i = 0; i < 50; ++i)
        {
            Rotate(0.04f, future.transform, ref futureSaturatiuonTimer);
            Translate(0.04f, future.transform);
            _spheres[i].transform.position = future.transform.position + new Vector3(0, 0.5f, 0);   
        }
        Destroy(future);
    }

    private void Translate(float deltaTime, Transform trans)
    {
        trans.position += trans.forward * GetComponent<LocomotionControl>().Get2DLeaningAxis().y * deltaTime * _translationSpeedFactor;

        // TODO smooth transition into this
        // when slow enough leaning controlles strafing
        if (GetComponent<LocomotionControl>().Get2DLeaningAxis().y < _velocityThesholdForInterfaceSwitch)
        {
            trans.position += trans.right * GetComponent<LocomotionControl>().Get2DLeaningAxis().x * deltaTime * _translationSpeedFactor;
        }
    }

    private void Rotate(float deltaTime, Transform trans, ref float saturationTimer)
    {
        float angle = _maxRotationSpeed * deltaTime;
        
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

        saturationTimer -= deltaTime;
        float signedAnglePerSecond = angle / deltaTime;

        // calculate distance to jump for the feedback
        _relDistanceToJump = Mathf.Clamp(Mathf.Abs(signedAnglePerSecond) / _rotationalJumpingThresholdDegreePerSecond, 0, 1);
        if (!_enableRotationalJumping)
        {
            // allways max thus the vignette is not there in this case, such as in case of a jump
            _relDistanceToJump = 1.0f;
        }

        // finally aplly the rotation
        if (_enableRotationalJumping &&
           Mathf.Abs(signedAnglePerSecond) > _rotationalJumpingThresholdDegreePerSecond &&
           saturationTimer < 0)
        {
            trans.RotateAround(GetComponent<LocomotionControl>().GetHeadJoint().transform.position, Vector3.up, defaultJumpSize * Mathf.Sign(signedAnglePerSecond));

            // reset saturation time
            float timeModifyer = 1;
            if (_enableDecreasingSaturationTime)
            {
                timeModifyer += (Mathf.Abs(signedAnglePerSecond) - _rotationalJumpingThresholdDegreePerSecond) / _timeDecreasingRotationalSpeedOvershoot;
            }
            saturationTimer = _maxSaturationTime / timeModifyer;
        }
        else
        {
            trans.RotateAround(GetComponent<LocomotionControl>().GetHeadJoint().transform.position, Vector3.up, angle);
        }
    }
}
