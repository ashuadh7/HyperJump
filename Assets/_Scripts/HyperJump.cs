﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class HyperJump : LocomotionMethodInterface
{
    #region Public Fields

    public bool _enableJumping;
    
    [Header("Method Settings")]
    [Tooltip("Defines the time between two jumps.")]
    public float _jumpSaturationTime;
    public float _minJumpSize;
    public float _jumpingThresholdMeterPerSecond;
    #endregion

    private float _jumpSaturationTimer;

    // should range from 0 no break to 1 full break;
    private float _breakState = 0f;
    private float _breakTarget = 0f;
    private float _currentBreakingVelocity = 0f;
    private float _speedAxis = 0f;
    
    private GameObject _camera;
    
    void Start()
    {
        LocomotionInputAdapterInterface[] inputAdapters = GetComponents<LocomotionInputAdapterInterface>();
        foreach (var elem in inputAdapters)
        {
            if (elem.enabled)
            {
                _locomotionInput = elem;
                break;
            }
        }
       
        _camera = GameObject.Find("Camera");
        _jumpSaturationTimer = _jumpSaturationTime;
    }

    private void Update()
    {
        if (_locomotionInput.IsInitialized())
        {
            Translate(Time.deltaTime, this.transform, _camera.transform, ref _jumpSaturationTimer, false);
        }
    }
    
    public void Translate(float deltaTime, Transform trans, Transform cameraTrans, ref float saturationTimer, bool isSimulation)
    {
        saturationTimer -= deltaTime;
        RaycastHit hit;
        Vector3 movementDirection;
        float travelSpeed;
        
        if (!isSimulation)
        {
            _STATE_jumpedThisFrame = false;
            _STATE_distanceLastJump = 0f;
            
            _breakState = Mathf.SmoothDamp(_breakState, _breakTarget, ref _currentBreakingVelocity, 0.5f);
        }
        
        movementDirection = _locomotionInput.GetDirectionAxes();
        speedAxis = Mathf.Max(Mathf.Abs(_locomotionInput.GetDirectionAxes().x), Mathf.Abs(_locomotionInput.GetDirectionAxes().z));
        travelSpeed =  speedAxis * GeneralLocomotionSettings.Instance._maxTranslationSpeed;
        // Debug.Log(travelSpeed);
        
        
        // when there is no obstacle in moving direction...
        if (!Physics.Raycast(trans.position, movementDirection,
            out hit, 1f))
        {
            // jump?
            if (_enableJumping &&
                Mathf.Abs(travelSpeed) > _jumpingThresholdMeterPerSecond &&
                saturationTimer < 0 &&
                !GetBreak())
            {
                // ... then calculate jump
                Vector3 targetPosition = trans.position;
                
                // normalize jump size, because there was a deadzone and we want to start at 0
                float threshold = _jumpingThresholdMeterPerSecond /
                                  GeneralLocomotionSettings.Instance._maxTranslationSpeed;
                float normalizedAxis = (_speedAxis - threshold) * 1 / (1 - threshold);
                
                targetPosition += _minJumpSize * movementDirection + normalizedAxis * (DetermineMaximumJumpSize() - _minJumpSize) * movementDirection;

                // obstacle?
                Vector3 path = targetPosition - trans.position;

                if (Physics.Raycast(trans.position, path.normalized, out hit, path.magnitude))
                {
                    // ...then do not jump
                    trans.position += (1f - _breakState) * travelSpeed * deltaTime * movementDirection;
                }
                // ... yes jump!
                else
                {
                    if (!isSimulation)
                    {
                        _STATE_jumpedThisFrame = true;
                        _STATE_distanceLastJump = (targetPosition - trans.position).magnitude;
                    }
                    trans.position = targetPosition;
                    saturationTimer = _jumpSaturationTime;
                }

            }
            // no jump, continues movement
            else
            {
                trans.position += (1f - _breakState) * travelSpeed * deltaTime * movementDirection;
            }
        }
    }

    private float DetermineMaximumJumpSize()
    {
        // _jumpSaturationTime should never be 0!!!!
        // maximum jump size solve equation: (_jumpSaturationTime * _jumpingThresholdMeterPerSecond + maxJumpSize) / _jumpSaturationTime = GeneralLocomotionSettings.Instance._maxTranslationSpeed
        return GeneralLocomotionSettings.Instance._maxTranslationSpeed * _jumpSaturationTime - (_jumpSaturationTime * _jumpingThresholdMeterPerSecond);
    }

    public float GetSaturationTimer()
    {
        return _jumpSaturationTimer;
    }

    public override void SetBreak(bool val)
    {
        if (val)
        {
            _breakTarget = 1;
        }
        else
        {
            _breakTarget = 0;
        }
    }

    public override bool GetBreak()
    {
        return _breakTarget == 1;
    }

    public override void SetTeleport(bool val)
    {
        _enableJumping = val;
    }

    public override float GetSpeedAxis()
    {
        return _speedAxis;
    }
}
