using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RotationJump : MonoBehaviour
{
    public bool enableAudioFeedback;
    public float translationSpeedFactor;

    public float angleThreshold;
    public bool useRollInstead;

    public float defaultJumpSize;
    public bool enableIncreasingAngle;
    public float jumpDoulblingAngle;

    public float maxSaturationTime;
    public bool enableDecreasingTime;
    public float timeHalfingAngle;


    private float saturationTimer;
    private SteamVR_Action_Vector2 axis;

    // -1 jump left, 0 center, 1 right
    private float reltativDistanceToJump;


    // Start is called before the first frame update
    void Start()
    {
        saturationTimer = maxSaturationTime;
        reltativDistanceToJump = 0;
        axis = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("MySet", "Throttle");

    }

    // Update is called once per frame
    void Update()
    {
        // virtual translation
        this.transform.position += this.transform.forward * axis.GetAxis(SteamVR_Input_Sources.Any).y * Time.deltaTime * translationSpeedFactor;

        float rotation;

        // user roll or yaw?
        if (!useRollInstead)
        {
            rotation = GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.y % 360;
        }
        else
        {
            rotation = (360 - GetComponentInChildren<Camera>().transform.localRotation.eulerAngles.z) % 360;
        }

        // clculate relative distance to jump
        if (rotation < 180)
        {
            reltativDistanceToJump = rotation / angleThreshold;
        }
        else if (rotation > 180)
        {
            reltativDistanceToJump = -(360 - rotation) / angleThreshold;
        }
        reltativDistanceToJump = Mathf.Clamp(reltativDistanceToJump, -1, 1);

        // virtual rotation
        if (saturationTimer < 0)
        {
            if (reltativDistanceToJump == 1)
            { 
                if(enableAudioFeedback)
                {
                    GetComponent<AudioSource>().Play();
                }
                
                float overshootInDegree = rotation - angleThreshold;
                float multiplyer = 1;
                if(enableIncreasingAngle)
                {
                    multiplyer += overshootInDegree / jumpDoulblingAngle;
                }
                this.transform.Rotate(Vector3.up, defaultJumpSize * multiplyer);

                multiplyer = 1;
                if (enableDecreasingTime)
                {
                    multiplyer += overshootInDegree / timeHalfingAngle;
                }
                saturationTimer = maxSaturationTime / multiplyer;
                
            }
            else if(reltativDistanceToJump == -1)
            {
                if (enableAudioFeedback)
                {
                    GetComponent<AudioSource>().Play();
                }

                float overshootInDegree = 360 - angleThreshold - rotation;
                float multiplyer = 1;
                if (enableIncreasingAngle)
                {
                    multiplyer += overshootInDegree / jumpDoulblingAngle;
                }
                this.transform.Rotate(Vector3.up, -defaultJumpSize * multiplyer);

                multiplyer = 1;
                if (enableDecreasingTime)
                {
                    multiplyer += overshootInDegree / timeHalfingAngle;
                }
                saturationTimer = maxSaturationTime / multiplyer;
            }
        }
        else
        {
            saturationTimer -= Time.deltaTime;
        }   
    }

    public float GetRelativDistanceToJump()
    {
        return reltativDistanceToJump;
    }
}
