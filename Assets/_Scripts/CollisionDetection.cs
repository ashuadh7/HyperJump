using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    // Start is called before the first frame update
    public GameManager gameManager;
    public LocomotionControl locomotionControl;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter (Collider other)
    {
        Debug.Log("Collided");
        if (other.GetComponent<Collider>().gameObject.tag == "MainCamera")
        {
            gameManager.pointingTask = true;
            locomotionControl.locomotionFreeze = true;
            Debug.Log("Pointing Task turned true");
        }
    }
}
