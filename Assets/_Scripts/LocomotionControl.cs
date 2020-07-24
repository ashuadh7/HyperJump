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

    [Tooltip("Define the distance from center which results in maximum axis deviation.")]
    public float _leaningForwardMaximumCM;

    [Tooltip("Define the distance from center which results in maximum axis deviation.")]
    public float _leaningSidewayMaximumCM;

    #endregion 


    private Vector3 _leaningRefPosition = Vector3.zero;
    private Vector3 _leaningRefOrientation = Vector3.zero;

    private float _forwadLeaningCM;

    // left being negative
    private float _sidwayLeaningCM; 
    
    // ranging between -180 (left) and +180
    private float _headYaw;

    // ranging between -180 (left) and +180
    private float _headRoll;

    private Vector2 _leaningAxis = Vector2.zero;


    void Start()
    {
        _forwadLeaningCM = 0;
        _sidwayLeaningCM = 0;
        _headYaw = 0;
        _headRoll = 0;
    }

    void Update()
    {
        if (_leaningRefPosition != Vector3.zero)
        {
            UpdateInputs();
            NormalizeTranslationalInputsToAxis();
            ApplyDeadzonesToAxis();
        } 
    }

    public Vector2 GetLeaningAxis()
    {
        return _leaningAxis;
    }

    private void NormalizeTranslationalInputsToAxis()
    {
        _leaningAxis.y = Mathf.Clamp(_forwadLeaningCM / _leaningForwardMaximumCM, -1, 1);
        _leaningAxis.x = Mathf.Clamp(_sidwayLeaningCM / _leaningSidewayMaximumCM, -1, 1);
    }

    private void ApplyDeadzonesToAxis()
    {
        // apply smooth deadzones
        if (_leaningAxis.y < 0 && _leaningAxis.y > -_leaningForwardDeadzone || _leaningAxis.y > 0 && _leaningAxis.y < _leaningForwardDeadzone)
        {
            _leaningAxis.y = 0;
        }
        else
        {
            _leaningAxis.y = (_leaningAxis.y - _leaningForwardDeadzone * Mathf.Sign(_leaningAxis.y)) / (1.0f - _leaningForwardDeadzone);
        }

        if (_leaningAxis.x < 0 && _leaningAxis.x > -_leaningSidewayDeadzone || _leaningAxis.x > 0 && _leaningAxis.x < _leaningSidewayDeadzone)
        {
            _leaningAxis.x = 0;
        }
        else
        {
            _leaningAxis.x = (_leaningAxis.x - _leaningSidewayDeadzone * Mathf.Sign(_leaningAxis.x)) / (1.0f - _leaningSidewayDeadzone);
        }
    }

    public void CalibrateLeaningKS()
    {
        _leaningRefPosition = GameObject.Find("Camera").transform.localPosition;
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

    private void UpdateInputs()
    {
        Vector3 diff = GameObject.Find("Camera").transform.localPosition - _leaningRefPosition;
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
}
