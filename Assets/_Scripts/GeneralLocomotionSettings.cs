using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralLocomotionSettings : MonoBehaviour
{
    private static GeneralLocomotionSettings _instance;

    public static GeneralLocomotionSettings Instance { get { return _instance; } }

    public Vector3 _resetPosition; 
    
    [Tooltip("Changes the maximum speed in m/s of forward and backward translation.")]
    public float _maxTranslationSpeed;
    
    [Tooltip("Gives the maximum rotational speed in degree per second.")]
    public float _maxRotationSpeed;

    private bool _isBreaked;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else {
            _instance = this;
        }
    }
    
    private void Start()
    {
        _isBreaked = false;
    }

    public void ResetPlayerPosition()
    {
        this.transform.position = _resetPosition;
    }
}
