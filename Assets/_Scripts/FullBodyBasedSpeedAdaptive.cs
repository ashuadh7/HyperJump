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

    public float _continuesRotationSpeedFactor;
    #endregion


    void Update()
    {
        Rotate();
        Translate();
    }

    private void Translate()
    {
        this.transform.position += this.transform.forward * GetComponent<LocomotionControl>().GetLeaningAxis().y * Time.deltaTime * _translationSpeedFactor;

        // when slow enough leaning controlles strafing
        if (GetComponent<LocomotionControl>().GetLeaningAxis().y < _velocityThesholdForInterfaceSwitch)
        {
            this.transform.position += this.transform.right * GetComponent<LocomotionControl>().GetLeaningAxis().x * Time.deltaTime * _translationSpeedFactor;
        }

    }

    private void Rotate()
    {
        // when fast enough leaning controlles rotation
        if (GetComponent<LocomotionControl>().GetLeaningAxis().y >= _velocityThesholdForInterfaceSwitch)
        {
            float rotationSpeedFactor = _continuesRotationSpeedFactor * (1.0f - GetComponent<LocomotionControl>().GetLeaningAxis().y);

            this.transform.RotateAround(
                GameObject.Find("Camera").transform.position, 
                Vector3.up,
                rotationSpeedFactor * Time.deltaTime * GetComponent<LocomotionControl>().GetLeaningAxis().x
            );
        }
    }
}
