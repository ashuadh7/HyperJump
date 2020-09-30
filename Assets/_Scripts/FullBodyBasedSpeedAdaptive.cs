﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class FullBodyBasedSpeedAdaptive : MonoBehaviour
{
    #region Public Fields

    [Header("Method Settings")]
    public bool _enabledPathPrediction;

    public GameObject _pathPrefab;

    [Tooltip("Changes the speed of forward and backward translation.")]
    public float _translationSpeedFactor;

    [Tooltip("Gives the point in percent of the maximum speed where strafing is replaced by rotating.")]
    [Range(0f, 1f)]
    public float _velocityThresholdForInterfaceSwitch;

    [Tooltip("... or just the HMD position instead.")]
    public bool _useCalibratedCenterOfRotation;

    [Tooltip("Gives the maximum rotational speed in degree per second.")]
    public float _maxRotationSpeed;

    [Header("Rotational Jumping")]
    public bool _enableRotationalJumping;
    
    [Tooltip("Enables the jump saturation time to dencrease being futher over the threshold.")]
    public bool _enableDecreasingSaturationTime;

    [Tooltip("When decreasing saturation time is activ this gives the increase of rotational speed above the threshold to effectivly halfen the saturation time.")]
    public float _timeDecreasingRotationalSpeedOvershoot;

    [Header("Decreasing Jump Time")]

    [Tooltip("Defines the minimal time between two jumps.")]
    public float _maxSaturationTime;

    [Tooltip("Defines the default, unmodified size of a jump rotation in degree.")]
    [Range(0f, 90f)]
    public float _defaultJumpSize;

    // the jumping threshold is given by rotational degree per second, this should make the threshold independent of the method used but
    // (be carefull) dependet of the transfer function
    [Tooltip("This effectivly overrides the maximum rotational speed, when the latter is larger than this one, it is never reached.")]
    public float _rotationalJumpingThresholdDegreePerSecond;

    [Header("Translational Jumping")]
    public bool _enableTranslationalJumping;

    public float _minJumpSize;
    
    public float _maxJumpSize;

    public float _translationalJumpingThresholdMeterPerSecond;

    #endregion

    private float _jumpSaturationTimer;
    private float _relDistanceToJump = 0.0f;
    private bool _initialized = false;

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
        _jumpSaturationTimer = _maxSaturationTime;
        InitPathPrediction();
    }

    void FixedUpdate()
    {
        // async timers
        float saturationTimeCopy = _jumpSaturationTimer;
        
        // actual travel
        if (_locomotionControl.GetHeadJoint() != null)
        {
            if (!_initialized)
            {
                _camera.GetComponentInChildren<Canvas>().transform.gameObject.SetActive(false);
                _initialized = true;
            }
            
            if (_useCalibratedCenterOfRotation)
            {
                Rotate(Time.deltaTime, this.transform, _locomotionControl.GetHeadJoint().transform, ref saturationTimeCopy);
            }
            else
            {
                Rotate(Time.deltaTime, this.transform, _camera.transform, ref saturationTimeCopy);
            }
        } 
        if(!_locomotionControl.IsBraked())
        {
            Translate(Time.deltaTime, this.transform, ref _jumpSaturationTimer);
        }

        // resync timers
        _jumpSaturationTimer = Mathf.Max(saturationTimeCopy, _jumpSaturationTimer);
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

        float futureRsaturatiuonTimer = _jumpSaturationTimer;

        for (int i = 0; i < 50; ++i)
        {
            // async timers
            float saturationTimeCopy = futureRsaturatiuonTimer;
            
            if(_useCalibratedCenterOfRotation)
            {
                Rotate(0.04f, _futureCameraRig.transform, _futureRotationalCenter.transform, ref saturationTimeCopy);
            }
            else
            {
                Rotate(0.04f, _futureCameraRig.transform, _futureCamera.transform, ref saturationTimeCopy);
            }

            Translate(0.04f, _futureCameraRig.transform, ref futureRsaturatiuonTimer);
            
            // resync timers
            futureRsaturatiuonTimer = Mathf.Max(saturationTimeCopy, futureRsaturatiuonTimer);
            
            RaycastHit hit;
            int layerMask = 1 << 8; // terrain
            Physics.Raycast(_futureCamera.transform.position + new Vector3(0,10,0), -Vector3.up, out hit, Mathf.Infinity,
                layerMask);
            
            _spheres[i].transform.position = hit.point;   
        }
    }

    private void Translate(float deltaTime, Transform trans, ref float saturationTimer)
    {  
        saturationTimer -= deltaTime;
        RaycastHit hit;
        
        if (!Physics.Raycast(trans.position, trans.forward * Mathf.Sign(_locomotionControl.Get2DLeaningAxis().y), out hit, 1f))
        {
            float distanceToTravel = _locomotionControl.Get2DLeaningAxis().y * _translationSpeedFactor;
            
            // jump?
            if (_enableTranslationalJumping &&
                Mathf.Abs(distanceToTravel) > _translationalJumpingThresholdMeterPerSecond &&
                saturationTimer < 0)
            {
                // ... then calculate jump
                Vector3 targetPosition = trans.position;
                
                // measuring ground level at start position
                RaycastHit hitOrigin, hitTarget;
                int layerMask = 1 << 8; // terrain
                Physics.Raycast(trans.position + new Vector3(0,10,0), -Vector3.up, out hitOrigin, Mathf.Infinity,
                    layerMask);
            
                // normalize jump size, because there was a deadzone and we want to start at 0
                float threshold = _translationalJumpingThresholdMeterPerSecond / _translationSpeedFactor;
                float normalizedAxis = (_locomotionControl.Get2DLeaningAxis().y - (threshold * Mathf.Sign(_locomotionControl.Get2DLeaningAxis().y))) * 1 / (1 - threshold);
            
                targetPosition += _minJumpSize * trans.forward + normalizedAxis * (_maxJumpSize - _minJumpSize) * trans.forward;
            
                // measuring gound level at target position...
                Physics.Raycast(targetPosition + new Vector3(0,10,0), -Vector3.up, out hitTarget, Mathf.Infinity,
                    layerMask);

                // to correct for the elevation
                float terrainHeightDiff = hitOrigin.distance - hitTarget.distance;
                targetPosition += Vector3.up * terrainHeightDiff;
            
                // obstacle?
                Vector3 path = targetPosition - trans.position;
               
                if (Physics.Raycast(trans.position, path.normalized, out hit, path.magnitude))
                {
                   // ...then do not jump
                   trans.position += distanceToTravel * deltaTime * trans.forward;
                }
                else
                {
                    trans.position = targetPosition;
                    saturationTimer = _maxSaturationTime;
                }
                
            }
            else
            {
                trans.position += distanceToTravel * deltaTime * trans.forward;
            }
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
            Mathf.Abs(_locomotionControl.Get2DLeaningAxis().y) < _velocityThresholdForInterfaceSwitch &&
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
        else if (_enableRotationalJumping &&
                 Mathf.Abs(_locomotionControl.Get2DLeaningAxis().y) < _velocityThresholdForInterfaceSwitch &&
                 saturationTimer > 0)
        {
            // no-op
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
        _spheres = new List<GameObject>();
        
        for (int i = 0; i < 50; ++i)
        {
            GameObject go = Instantiate(_pathPrefab, this.transform.position, Quaternion.identity, pathPrediction.transform);
            Color color = go.GetComponent<Renderer>().material.color;
            color.a = Mathf.Lerp(0.4f, 0.0f, i/50f);
            go.GetComponent<Renderer>().material.SetColor("_Color", color);
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
