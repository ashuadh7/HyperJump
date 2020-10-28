using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointCollider : MonoBehaviour
{
    public GameObject core;
    public GameObject cameraEye;
    public GameManager gameManager;
    private bool enter = false;
    private bool exit = false;
    Vector2 enterLocation;
    Vector2 exitLocation;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (enter)
        {
            enterLocation.x = cameraEye.transform.position.x;
            enterLocation.y = cameraEye.transform.position.z;
        }
        if (exit)
        {
           
            exitLocation.x = cameraEye.transform.position.x;
            exitLocation.y = cameraEye.transform.position.z;
            float x1 = enterLocation.x;
            float y1 = enterLocation.y;
            float x2 = exitLocation.x;
            float y2 = exitLocation.y;
            float x0 = core.transform.position.x;
            float y0 = core.transform.position.z;
            float numerator = Mathf.Abs((y2 - y1) * x0 - (x2 - x1) * y0 + x2 * y1 - y2 * x1);
            float denominator = Mathf.Pow((y2 - y1) * (y2 - y1) + (x2 - x1) * (x2 - x1), .5f);
            if (denominator != 0)
            {
                float distance = numerator/denominator;
                Debug.Log("Distance = " + distance);
            }

            this.gameObject.SetActive(false);

        }
        
    }
    private void OnTriggerEnter (Collider other)
    {
        if (other.GetComponent<Collider>().gameObject.tag == "MainCamera")
        {
            enter = true;
        }
    }

    private void OnTriggerExit (Collider other)
    {
        if (other.GetComponent<Collider>().gameObject.tag == "MainCamera")
        {
            exit = true;
        }
    }
}
