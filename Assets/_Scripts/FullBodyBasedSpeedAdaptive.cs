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
    public float _velocityThresholdForInterfaceSwitch;

    [Tooltip("... or just the HMD position instead.")]
    public bool _useCalibratedCenterOfRotation;

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
    public float _defaultJumpSize;

    // the jumping threshold is given by rotational degree per second, this should make the threshold independent of the method used but
    // (be carefull) dependet of the transfer function
    [Tooltip("This effectivly overrides the maximum rotational speed, when the latter is larger than this one, it is never reached.")]
    public float _rotationalJumpingThresholdDegreePerSecond;

    [Header("Decreasing Jump Time")]
    [Tooltip("Enables the jump saturation time to dencrease being futher over the threshold.")]
    public bool _enableDecreasingSaturationTime;

    [Tooltip("When decreasing saturation time is activ this gives the increase of rotational speed above the threshold to effectivly halfen the saturation time.")]
    public float _timeDecreasingRotationalSpeedOvershoot;

    [Header("Translational Jumping")]
    public bool _enableTranslationalJumping;

    public float _maxJumpSize;

    public float _translationalJumpingThresholdMeterPerSecond;

    #endregion

    private float _rotationJumpSaturationTimer;
    private bool _jumpedRotationalThisFrame;
    private float _translationJumpSaturationTimer;
    private float _relDistanceToJump = 0.0f;

    // path prediction
    private List<GameObject> _spheres;
    GameObject _futureCameraRig;
    GameObject _futureCamera;
    GameObject _futureRotationalCenter;

    private LocomotionControl _locomotionControl;
    private GameObject _camera;
    
    void Start()
    {
        _locomotionControl = GetComponent<LocomotionControl>();
        _camera = GameObject.Find("Camera");
        _rotationJumpSaturationTimer = _maxSaturationTime;
        _translationJumpSaturationTimer = _maxSaturationTime;
        InitPathPrediction();
    }

    void FixedUpdate()
    {
        // actual travel
        if (_locomotionControl.GetHeadJoint() != null)
        {
            if (_useCalibratedCenterOfRotation)
            {
                Rotate(Time.deltaTime, this.transform, _locomotionControl.GetHeadJoint().transform, ref _rotationJumpSaturationTimer);
            }
            else
            {
                Rotate(Time.deltaTime, this.transform, _camera.transform, ref _rotationJumpSaturationTimer);
            }
        } 
        if(!_locomotionControl.IsBraked())
        {
            Translate(Time.deltaTime, this.transform, ref _translationJumpSaturationTimer);
        }    
    }

    private void Update()
    {
        // travel simulation
        for (int i = 0; i < 50; ++i)
        {
            _spheres[i].SetActive(_enabledPathPrediction);
        }

        if (_locomotionControl.GetHeadJoint() != null && _enabledPathPrediction)
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
        _futureCameraRig.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
        _futureCamera.transform.SetPositionAndRotation(_camera.transform.position, _camera.transform.rotation);
        _futureRotationalCenter.transform.position = _locomotionControl.GetHeadJoint().transform.position;

        float futureRotationalSaturatiuonTimer = _rotationJumpSaturationTimer;
        float futureTranslationalSaturationTimer = _translationJumpSaturationTimer;

        for (int i = 0; i < 50; ++i)
        {
            if(_useCalibratedCenterOfRotation)
            {
                Rotate(0.04f, _futureCameraRig.transform, _futureRotationalCenter.transform, ref futureRotationalSaturatiuonTimer);
            }
            else
            {
                Rotate(0.04f, _futureCameraRig.transform, _futureCamera.transform, ref futureRotationalSaturatiuonTimer);
            }

            Translate(0.04f, _futureCameraRig.transform, ref futureTranslationalSaturationTimer);
            _spheres[i].transform.position = _futureCamera.transform.position + new Vector3(0, -1.0f, 0);   
        }
    }

    private void Translate(float deltaTime, Transform trans, ref float saturationTimer)
    {  
        saturationTimer -= deltaTime;
        float distanceToTravel = _locomotionControl.Get2DLeaningAxis().y * _translationSpeedFactor;

        // jump?
        // TODO & no wall...
        if (_enableTranslationalJumping &&
          Mathf.Abs(distanceToTravel) > _translationalJumpingThresholdMeterPerSecond &&
          saturationTimer < 0)
        {
            RaycastHit hitOrigin, hitTarget;
            int layerMask = 1 << 8; // terrain
            Physics.Raycast(transform.position + new Vector3(0,10,0), transform.TransformDirection(-Vector3.up), out hitOrigin, Mathf.Infinity,
                layerMask);
            
            float threshold = _translationalJumpingThresholdMeterPerSecond / _translationSpeedFactor;
            float normalizedAxis = (_locomotionControl.Get2DLeaningAxis().y - (threshold * Mathf.Sign(_locomotionControl.Get2DLeaningAxis().y))) * 1 / (1 - threshold);
            
            trans.position += normalizedAxis * _maxJumpSize * Mathf.Sign(distanceToTravel) * trans.forward;
            
            Physics.Raycast(transform.position + new Vector3(0,10,0), transform.TransformDirection(-Vector3.up), out hitTarget, Mathf.Infinity,
                layerMask);

            float terrainHeightDiff = hitOrigin.distance - hitTarget.distance;
            trans.position += Vector3.up * terrainHeightDiff;
            
            saturationTimer = _maxSaturationTime;
        }
        else
        {
            trans.position += distanceToTravel * deltaTime * trans.forward;
        }
    }

    private void Rotate(float deltaTime, Transform trans, Transform rotationalCenter, ref float saturationTimer)
    {
        float angle = _maxRotationSpeed * deltaTime;
        
        // TODO smooth transitions between the two modi
        // when fast enough leaning controlles rotation
        if (Mathf.Abs(_locomotionControl.Get2DLeaningAxis().y) >= _velocityThresholdForInterfaceSwitch)
        {
            // leaning faster to the sides results in faster yaw rotation
            angle *= _locomotionControl.Get2DLeaningAxis().x;
            
            // for faster tavel speeds rotation speed is increased;
            if(_enableMotorcycleMode)
            {
                angle *= (1.0f - _locomotionControl.Get2DLeaningAxis().y);
            }  
        }
        // when slower it is the head yaw only
        else
        {
            angle *= _locomotionControl.GetHeadYawAxis();
        }

        saturationTimer -= deltaTime;
        float signedAnglePerSecond = angle / deltaTime;

        // calculate distance to jump for the feedback
        _relDistanceToJump = Mathf.Clamp(signedAnglePerSecond / _rotationalJumpingThresholdDegreePerSecond, -1, 1);
        if (!_enableRotationalJumping)
        {
            _relDistanceToJump = 0.0f;
        }

        // finally apply the rotation
        if (_enableRotationalJumping &&
           Mathf.Abs(signedAnglePerSecond) > _rotationalJumpingThresholdDegreePerSecond &&
           saturationTimer < 0)
        {
            trans.RotateAround(rotationalCenter.position, Vector3.up, _defaultJumpSize * Mathf.Sign(signedAnglePerSecond));

            // reset saturation time
            float timeModifier = 1;
            if (_enableDecreasingSaturationTime)
            {
                timeModifier += (Mathf.Abs(signedAnglePerSecond) - _rotationalJumpingThresholdDegreePerSecond) / _timeDecreasingRotationalSpeedOvershoot;
            }
            saturationTimer = _maxSaturationTime / timeModifier;
        }
        else
        {
            trans.RotateAround(rotationalCenter.position, Vector3.up, angle);
        }
    }

    private void InitPathPrediction()
    {
        // setup path
        GameObject pathPrediction = new GameObject("PathPrediction");
        pathPrediction.transform.parent = this.transform; ;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        sphere.transform.SetPositionAndRotation(this.transform.position, Quaternion.identity);
        sphere.transform.parent = pathPrediction.transform;
        Destroy(sphere.GetComponent<Collider>());
        _spheres = new List<GameObject>();
        _spheres.Add(sphere);

        for (int i = 1; i < 50; ++i)
        {
            GameObject go = Instantiate(sphere, this.transform.position, Quaternion.identity, pathPrediction.transform);
            Destroy(go.GetComponent<Collider>());
            _spheres.Add(go);
        }

        // setup simulated rig
        _futureCameraRig = new GameObject("SimulatedCameraRig");
        _futureCameraRig.transform.SetParent(this.transform.parent);

        _futureCamera = new GameObject("SiumlatedCamera");
        _futureCamera.transform.SetParent(_futureCameraRig.transform);

        _futureRotationalCenter = new GameObject("SimulatedHeadJoint");
        _futureRotationalCenter.transform.SetParent(_futureCamera.transform);
    }
}
