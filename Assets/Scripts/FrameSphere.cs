using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameSphere : MonoBehaviour
{
    public Material neutralMat;
    public Material deleteMat;
    public Material finishMat;

    private bool firstOnePlaced;

    void Awake()
    {
        GetComponent<MeshRenderer>().material = neutralMat;
        firstOnePlaced = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (firstOnePlaced)
        {
            GetComponent<MeshRenderer>().material = finishMat;
        }
        else
        {
            GetComponent<MeshRenderer>().material = deleteMat;
        }
            
    }

    private void OnTriggerExit(Collider other)
    {
        GetComponent<MeshRenderer>().material = neutralMat;
    }

    public void SetFirstOnePlaced()
    {
        firstOnePlaced = true;
    }

    public bool GetIsFirstPlaced()
    {
        return firstOnePlaced;
    }

}
