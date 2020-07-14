using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class Translation : MonoBehaviour
{
    #region Public Fields
    [Header("Locomotion Settings")]
    [Tooltip("Changes the speed of forward and backward translation.")]
    [Range(0f, 20f)]
    public float translationSpeedFactor;

    public bool enableLeaning;

    [Range(0f, 1f)]
    public float _leaningDeadzone;

    [Tooltip("Define the distance from center which results in maximum speed.")]
    public float maxSpeedAfterCM;

    #endregion

    private SteamVR_Action_Vector2 _axis;
    private Vector3 _leaningRef = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        _axis = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("MySet", "Throttle");
    }

    void Update()
    {
        if(!enableLeaning)
        {
            this.transform.position += this.transform.forward * _axis.GetAxis(SteamVR_Input_Sources.Any).y * Time.deltaTime * translationSpeedFactor;
        }
        else
        {
            float vertical = 0;

            if (_leaningRef != Vector3.zero)
            {
                Vector3 diff = GameObject.Find("Camera").transform.localPosition - _leaningRef;
                vertical = diff.z;
                vertical = Mathf.Clamp(vertical / maxSpeedAfterCM, -1, 1);
            }

            // applay deadzone
            if (vertical < 0 && vertical > -_leaningDeadzone || vertical > 0 && vertical < _leaningDeadzone)
            {
                vertical = 0;
            }

            this.transform.position += this.transform.forward * vertical * Time.deltaTime * translationSpeedFactor;
        } 
    }


    public void CalibrateLeaningKS()
    {
        _leaningRef = GameObject.Find("Camera").transform.localPosition;
    }
}
