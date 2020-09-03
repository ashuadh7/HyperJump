using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocomotionControl : MonoBehaviour
{
    #region Public Fields
    [Header("Locomotion Settings")]
    [Tooltip("Leaning forward deadzone in percent.")]
    [Range(0f, 0.9f)]
    public float _leaningForwardDeadzone;

    [Tooltip("Leaning sideways deadzone in percent.")]
    [Range(0f, 0.9f)]
    public float _leaningSidewayDeadzone;

    [Tooltip("Head yaw deadzone in percent.")]
    [Range(0f, 0.9f)]
    public float _headYawDeadzone;

    [Tooltip("Define the head yaw angel resulting in maximum axis deviation.")]
    [Range(0f, 180f)]
    public float _headYawMaxAngle;

    [Tooltip("Define the distance from center which results in maximum axis deviation.")]
    public float _leaningForwardMaximumCM;

    [Tooltip("Define the distance from center which results in maximum axis deviation.")]
    public float _leaningSidewayMaximumCM;
    
    // [Tooltip("Power of the exponetial Function")]
    // [Range(1f, 2f)]
    private float _exponentialTransferFunctionPower = 1.53f;
    
    [Tooltip("Sensitivity of leaning (inside the exponetial function)")]
    [Range(0f, 5f)]
    public float _speedSensitiviy = 1f;

    [Tooltip("Speed Limit (outside of the exponential function)")]
    [Range(0f, 10f)]
    public float _speedLimit = 1f;
    
    // In Summary --> input = speedLimit * (leaningMag * speedSensitivity)^(exponential)
    #endregion 


    private Vector3 _leaningRefPosition = Vector3.zero;
    private Vector3 _leaningRefOrientation = Vector3.zero;

    private float _forwadLeaningCM;

    // left being negative
    private float _sidwayLeaningCM; 
    
    // ranging between -180 (left) and +180
    private float _headYaw;

    private float _headYawAxis;

    // ranging between -180 (left) and +180
    private float _headRoll;

    private Vector2 _leaningAxis = Vector2.zero;
    private bool _break;

    // TODO mabye move the whole procedure to its own class
    // center of rotation calibration
    GameObject _headJoint = null;
    private bool _calibrationRecordingEndabled;
    private List<Vector3> _hmdPositions;
    private List<Vector3> _hmdForwards;
    private float _samplingFrequence = 0.1f;
    private float _samplingTimer;


    void Start()
    {
        _forwadLeaningCM = 0;
        _sidwayLeaningCM = 0;
        _headYaw = 0;
        _headRoll = 0;
        _break = false;
        _headYawAxis = 0;
        _calibrationRecordingEndabled = false;    
    }

    void Update()
    {
        if(_calibrationRecordingEndabled)
        {
            _samplingTimer -= Time.deltaTime;
            if(_samplingTimer < 0)
            {
                _hmdPositions.Add(GameObject.Find("Camera").transform.position);
                _hmdForwards.Add(GameObject.Find("Camera").transform.forward);
                _samplingTimer = _samplingFrequence;
            }
        }        
        else if (_leaningRefPosition != Vector3.zero)
        {
            UpdateInputs();
            NormalizeTranslationalInputsToAxis();
            ApplyDeadzonesToAxis();
        } 
    }

    public float GetHeadYaw()
    {
            return _headYaw;
    }

    public float GetHeadYawAxis()
    {
        return _headYawAxis;
    }

    public Vector2 Get2DLeaningAxis()
    {
        return _leaningAxis;
    }

    private void NormalizeTranslationalInputsToAxis()
    {
        _leaningAxis.y = Mathf.Clamp(_forwadLeaningCM / _leaningForwardMaximumCM, -1, 1);
        _leaningAxis.x = Mathf.Clamp(_sidwayLeaningCM / _leaningSidewayMaximumCM, -1, 1);
        _headYawAxis = Mathf.Clamp(_headYaw / _headYawMaxAngle, -1, 1);
    }

    private void ApplyDeadzonesToAxis()
    {
        // apply smooth deadzones
        // if (_leaningAxis.y < 0 && _leaningAxis.y > -_leaningForwardDeadzone || _leaningAxis.y > 0 && _leaningAxis.y < _leaningForwardDeadzone)
        // {
        //     _leaningAxis.y = 0;
        // }
        // else
        // {
        //     _leaningAxis.y = (_leaningAxis.y - _leaningForwardDeadzone * Mathf.Sign(_leaningAxis.y)) / (1.0f - _leaningForwardDeadzone);
        // }

        // if (_leaningAxis.x < 0 && _leaningAxis.x > -_leaningSidewayDeadzone || _leaningAxis.x > 0 && _leaningAxis.x < _leaningSidewayDeadzone)
        // {
        //     _leaningAxis.x = 0;
        // }
        // else
        // {
        //     _leaningAxis.x = (_leaningAxis.x - _leaningSidewayDeadzone * Mathf.Sign(_leaningAxis.x)) / (1.0f - _leaningSidewayDeadzone);
        // }

        // if (_headYawAxis < 0 && _headYawAxis > -_headYawDeadzone || _headYawAxis > 0 && _headYawAxis < _headYawDeadzone)
        // {
        //     _headYawAxis = 0;
        // }
        // else
        // {
        //     _headYawAxis = (_headYawAxis - _headYawDeadzone * Mathf.Sign(_headYawAxis)) / (1.0f - _headYawDeadzone);
        // }

        float velocity = Mathf.Pow(Mathf.Max(0, _leaningAxis.magnitude - _leaningForwardDeadzone)*_speedSensitiviy, _exponentialTransferFunctionPower)*_speedLimit;
        _leaningAxis = _leaningAxis.normalized * velocity; 
    }

    public GameObject GetHeadJoint()
    {
        return _headJoint;
    }

    public float GetRelativDistanceToJump()
    {
        return this.GetComponent<FullBodyBasedSpeedAdaptive>().GetRelativeDistanceToJump();
    }

    public void CalibrateLeaningKS()
    {
        _leaningRefPosition = this.transform.InverseTransformPoint(GetComponent<LocomotionControl>().GetHeadJoint().transform.position);
        _leaningRefOrientation = GetComponentInChildren<Camera>().transform.localRotation.eulerAngles;

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
    }

    public void StartCenterofRotationCalibration()
    {
        _calibrationRecordingEndabled = true;
        _samplingTimer = _samplingFrequence;
        _hmdPositions = new List<Vector3>();
        _hmdForwards = new List<Vector3>();  
    }

    public void FinishCenterofRotationCalibration()
    {
        _calibrationRecordingEndabled = false;
        int firstSample = _hmdPositions.Count / 4;
        int secondSample = firstSample * 3;

        // constuct a saggital plane at the head set's current/final position
        Plane saggitalPlane = new Plane();
        saggitalPlane.SetNormalAndPosition(GameObject.Find("Camera").transform.right, GameObject.Find("Camera").transform.position);

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

        // TODO
        // 1. use this center of rotation for yaw rotations
        // 2. use it to differ between looking to the side and moving to the side
        _headJoint = Instantiate(centerOfYawRotation, centerOfYawRotationGlobal, Quaternion.identity, GameObject.Find("Camera").transform);

        Debug.Log("Head's center of yaw rotation distance to headset: " + (GameObject.Find("Camera").transform.position - centerOfYawRotationGlobal).magnitude);
        
        // Debugging Visualisation 
        /*
        Debug.Log(firstTarget);
        Debug.Log(secondTarget);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        Instantiate(sphere, firstTarget, Quaternion.identity, GameObject.Find("Camera").transform);
        Instantiate(sphere, secondTarget, Quaternion.identity, GameObject.Find("Camera").transform);
        Instantiate(sphere, GameObject.Find("Camera").transform.position, Quaternion.identity, GameObject.Find("Camera").transform);*/
    }


    private void UpdateInputs()
    {
        Vector3 diff = this.transform.InverseTransformPoint(GetComponent<LocomotionControl>().GetHeadJoint().transform.position) - _leaningRefPosition;
        _sidwayLeaningCM = diff.x;
        _forwadLeaningCM = diff.z;

        _headYaw = GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.y;
        if (_headYaw > 180)
        {
            _headYaw -= 360;
        }
        _headYaw -= _leaningRefOrientation.y;

        _headRoll = GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.z;
        if (_headRoll > 180)
        {
            _headRoll -= 360;
        }
        _headRoll -= _leaningRefOrientation.z;
        _headRoll *= -1;
    }

    public void UpdateBreak(bool val)
    {
        _break = val;
    }

    public bool isBreaked()
    {
        return _break;
    }
}
