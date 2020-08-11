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

    public float _movingRotationSpeedFactor;

    public float _standingRotationSpeedFactor;

    public bool _enableRotationalJumping;

    // the jumping threshold is given by rotational degree per second, this should make the threshold independent of the method used but
    // (be carefull) dependet of the transfer function
    public float _rotationalJumpingThresholdDegreePerSecond;

    #endregion


    void Update()
    {
        Rotate();
        Translate();
    }

    private void Translate()
    {
        this.transform.position += this.transform.forward * GetComponent<LocomotionControl>().GetLeaningAxis().y * Time.deltaTime * _translationSpeedFactor;

        // TODO smooth transition into this
        // when slow enough leaning controlles strafing
        if (GetComponent<LocomotionControl>().GetLeaningAxis().y < _velocityThesholdForInterfaceSwitch)
        {
            this.transform.position += this.transform.right * GetComponent<LocomotionControl>().GetLeaningAxis().x * Time.deltaTime * _translationSpeedFactor;
        }

    }

    private void Rotate()
    {
        float rotation = Time.deltaTime;

        // TODO smooth transitions between the two modi
        // when fast enough leaning controlles rotation
        if (GetComponent<LocomotionControl>().GetLeaningAxis().y >= _velocityThesholdForInterfaceSwitch)
        {
            // leaning faster to the sides results in faster yaw rotation
            rotation *= _movingRotationSpeedFactor * GetComponent<LocomotionControl>().GetLeaningAxis().x;
            
            // for faster tavel speeds rotation speed is increased;
            rotation *= (1.0f - GetComponent<LocomotionControl>().GetLeaningAxis().y);

            
        }
        // when slower it is the head yaw only
        else
        {
            rotation *= _standingRotationSpeedFactor * GetComponent<LocomotionControl>().GetYaw();
        }

        // finally aplly the rotation
        this.transform.RotateAround(GameObject.Find("Camera").transform.position, Vector3.up, rotation);
    }
}
