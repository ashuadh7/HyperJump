using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullBodyBasedSpeedAdaptive : MonoBehaviour
{
    #region Public Fields

    [Header("Method Settings")]
    [Tooltip("Changes the speed of forward and backward translation.")]
    public float _translationSpeedFactor;

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

        // when slow strafe here leaning
    }

    private void Rotate()
    {
        float rotationSpeedFactor = _continuesRotationSpeedFactor * (1.0f - GetComponent<LocomotionControl>().GetLeaningAxis().y);

        // TODO when fast: roll and leaning rotates, when slow: only roll / yaw
        this.transform.RotateAround(
            GameObject.Find("Camera").transform.position, 
            Vector3.up,
            rotationSpeedFactor * Time.deltaTime * GetComponent<LocomotionControl>().GetLeaningAxis().x
        );
    }
}
