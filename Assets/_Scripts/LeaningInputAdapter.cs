using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LeaningInputAdapter : LocomotionInputAdapterInterface
{
    #region Public Fields
    [Header("Locomotion Settings")]
    [Tooltip("Leaning forward dead-zone in percent.")]
    [Range(0f, 0.9f)]
    public float _leaningForwardDeadzone;

    public float _headYawDeadzone;
    
    [Tooltip("Define the head yaw angel resulting in maximum axis deviation.")]
    [Range(0f, 180f)]
    public float _headYawMaxAngle;

    [Tooltip("Define the distance from center which results in maximum axis deviation.")]
    public float _leaningForwardMaximumCM;

    [Tooltip("Define the distance from center which results in maximum axis deviation.")]
    public float _leaningSidewayMaximumCM;
    
    [Tooltip("Sensitivity of leaning (inside the exponential function)")]
    [Range(0f, 5f)]
    public float _leaningAxesSensitivity = 1f;

    [Tooltip("Speed Limit (outside of the exponential function)")]
    [Range(0f, 10f)]

    #endregion

    private GameObject _camera;

    private Vector3 _leaningRefPosition = Vector3.zero;
    private Vector3 _leaningRefOrientation = Vector3.zero;

    private float _forwardLeaningCM;

    // left being negative
    private float _sidwayLeaningCM; 
    
    // ranging between -180 (left) and +180
    private float _headYaw;

    private float _headYawAxis;

    // ranging between -180 (left) and +180
    private float _headRoll;

    private Vector2 _leaningAxis = Vector2.zero;

    // center of rotation calibration
    private bool _methodIsCalibrated;
    GameObject _headJoint = null;
    private bool _calibrationRecordingEnabled;
    private List<Vector3> _hmdPositions;
    private List<Vector3> _hmdForwards;
    private float _samplingFrequency = 0.1f;
    private float _samplingTimer;
    
    // [Tooltip("Power of the exponetial Function")]
    // [Range(1f, 2f)]
    private const float ExponentialTransferFunctionPower = 1.53f;
    
    void Start()
    {
        _methodIsCalibrated = false;
        _forwardLeaningCM = 0;
        _sidwayLeaningCM = 0;
        _headYaw = 0;
        _headRoll = 0;
        _headYawAxis = 0;
        _calibrationRecordingEnabled = false;
        _camera = GameObject.Find("Camera");
    }

    void Update()
    {
        // while calibration is going on....
        if(_calibrationRecordingEnabled)
        {
            RecordFrameForCalibration();
        }        
        else if (_isInitialized)
        {
            UpdateInputs();
            NormalizeInputsToAxes();
            ApplyTransferFunctionsToAxes();
            ApplyDeadzonesToAxes();
        } 
    }
    
    private void RecordFrameForCalibration()
    {
        _samplingTimer -= Time.deltaTime;
        if(_samplingTimer < 0)
        {
            _hmdPositions.Add(_camera.transform.position);
            _hmdForwards.Add(_camera.transform.forward);
            _samplingTimer = _samplingFrequency;
        }
    }
    
    private void UpdateInputs()
    {
        UpdateLeaning();
        UpdateHeadYaw();
        UpdateHeadRoll();
    }

    private void UpdateLeaning()
    {
        Vector3 diff = this.transform.InverseTransformPoint(GetHeadJoint().transform.position) - _leaningRefPosition;
        _sidwayLeaningCM = diff.x;
        _forwardLeaningCM = diff.z;
    }

    private void UpdateHeadYaw()
    {
        _headYaw = _camera.transform.localRotation.eulerAngles.y;
        if (_headYaw > 180)
        {
            _headYaw -= 360;
        }
        _headYaw -= _leaningRefOrientation.y;
    }
    
    public float GetHeadYaw()
    {
        return _headYaw;
    }

    private void UpdateHeadRoll()
    {
        _headRoll = _camera.transform.localRotation.eulerAngles.z;
        if (_headRoll > 180)
        {
            _headRoll -= 360;
        }
        _headRoll -= _leaningRefOrientation.z;
        _headRoll *= -1;
    }
    
    public float GetHeadRoll()
    {
        return _headRoll;
    }
    
    private void NormalizeInputsToAxes()
    {
        _leaningAxis.y = Mathf.Clamp(_forwardLeaningCM / _leaningForwardMaximumCM, -1, 1);
        _leaningAxis.x = Mathf.Clamp(_sidwayLeaningCM / _leaningSidewayMaximumCM, -1, 1);
        _headYawAxis = Mathf.Clamp(_headYaw / _headYawMaxAngle, -1, 1);
    }
    
    private void ApplyTransferFunctionsToAxes()
    {
        float velocity = Mathf.Pow(Mathf.Max(0, _leaningAxis.magnitude - _leaningForwardDeadzone) * _leaningAxesSensitivity, ExponentialTransferFunctionPower);
        _leaningAxis = _leaningAxis.normalized * velocity; 
    }
    
    private void ApplyDeadzonesToAxes()
    {
        if (_headYawAxis < 0 && _headYawAxis > -_headYawDeadzone || _headYawAxis > 0 && _headYawAxis < _headYawDeadzone) 
        {
            _headYawAxis = 0;
        }
        else
        {
            _headYawAxis = (_headYawAxis - _headYawDeadzone * Mathf.Sign(_headYawAxis)) / (1.0f - _headYawDeadzone);
        }
    }

    public override Vector2 GetDirectionAxes()
    {
        return _leaningAxis;
    }

    public float GetHeadYawAxis()
    {
        return _headYawAxis;
    }
    
    public float GetHeadRollAxis()
    {
        return _headYawAxis;
    }

    
    // calibration process
    public override void StartCalibration()
    {
        Debug.Log("Started calibration...");
        _calibrationRecordingEnabled = true;
        _samplingTimer = _samplingFrequency;
        _hmdPositions = new List<Vector3>();
        _hmdForwards = new List<Vector3>();  
    }

    public override void EndCalibration()
    {
        StopCenterOfRotationCalibration();
        CalibrateLeaningKS();
        Debug.Log("Finished calibration...");
    }

    private void StopCenterOfRotationCalibration()
    {
        _calibrationRecordingEnabled = false;
        int firstSample = _hmdPositions.Count / 4;
        int secondSample = firstSample * 3;

        // construct a saggital plane at the head set's current/final position
        Plane saggitalPlane = new Plane();
        saggitalPlane.SetNormalAndPosition(_camera.transform.right, _camera.transform.position);

        // shoot a ray from two positions on the calibration arc to the plane the results is the center of (yaw) rotation
        // Note: Only one sample would be nessesary.
        float distanceToPlane;
        Ray ray = new Ray(_hmdPositions[firstSample], -_hmdForwards[firstSample]);
        saggitalPlane.Raycast(ray, out distanceToPlane);
        Vector3 firstTarget = ray.GetPoint(distanceToPlane);
      
        ray = new Ray(_hmdPositions[secondSample], -_hmdForwards[secondSample]);
        saggitalPlane.Raycast(ray, out distanceToPlane);
        Vector3 secondTarget = ray.GetPoint(distanceToPlane);

        Vector3 centerOfYawRotationGlobal = (firstTarget + secondTarget) / 2f;
        GameObject centerOfYawRotation = new GameObject("CenterOfYawRotation");
        
        _headJoint = Instantiate(centerOfYawRotation, centerOfYawRotationGlobal, Quaternion.identity, _camera.transform);

        Debug.Log("Head's center of yaw rotation distance to headset: " + (_camera.transform.position - centerOfYawRotationGlobal).magnitude);
    }
    
    private void CalibrateLeaningKS()
    {
        _leaningRefPosition = this.transform.InverseTransformPoint(GetHeadJoint().transform.position);
        _leaningRefOrientation = _camera.transform.localRotation.eulerAngles;

        if (_leaningRefOrientation.x > 180)
        {
            _leaningRefOrientation.x -= 360;
        }

        if (_leaningRefOrientation.y > 180)
        {
            _leaningRefOrientation.y -= 360;
        }

        if (_leaningRefOrientation.z > 180)
        {
            _leaningRefOrientation.z -= 360;
        }
        
        _isInitialized = true;
        // TODO ceate this in code 
        _camera.GetComponentInChildren<Canvas>().transform.gameObject.SetActive(false);
    }
    
    public GameObject GetHeadJoint()
    {
        return _headJoint;
        
    }

    public bool IsCalibrated()
    {
        return _methodIsCalibrated;
    }
}
