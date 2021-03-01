using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerInputAdapter : LocomotionInputAdapterInterface
{
    public SteamVR_Action_Pose _controllerPose;
    public SteamVR_Action_Vector2 _joystickAxes;

    [Tooltip("Dead-zone in percent.")]
    [Range(0f, 0.9f)]
    public float _deadzone;
    
    [Tooltip("Sensitivity (inside the exponential function)")]
    [Range(0f, 5f)]
    public float _axesSensitivity = 1f;
    
    private const float ExponentialTransferFunctionPower = 1.53f;
    
    private Vector2 _axes = Vector2.zero;

    private GameObject _camera;

    private void Start()
    {
        _isInitialized = true;
        GameObject.Find("Camera").GetComponentInChildren<Canvas>().transform.gameObject.SetActive(false);
        _camera = GameObject.Find("Camera");
    }

    void Update()
    {
        UpdateInputs();
        ApplyTransferFunctionsToAxes();
    }

    private void UpdateInputs()
    {
        Vector3 joystickTo3D = new Vector3( _joystickAxes.axis.x, 0,  _joystickAxes.axis.y);
        joystickTo3D = _controllerPose.localRotation * joystickTo3D;
       _axes = new Vector2(joystickTo3D.x, joystickTo3D.z).normalized * _joystickAxes.axis.magnitude;
    }
    
    private void ApplyTransferFunctionsToAxes()
    {
        float velocity = Mathf.Pow(Mathf.Max(0, _axes.magnitude - _deadzone) * _axesSensitivity, ExponentialTransferFunctionPower);
        _axes = _axes.normalized * velocity; 
    }
    
    public override Vector2 GetDirectionAxes()
    {
        return _axes;
    }

    public override Vector3 GetCenterofRotation()
    {
        return _camera.transform.position;
    }

    public override void StartCalibration(){}
    public override void EndCalibration(){}
}
