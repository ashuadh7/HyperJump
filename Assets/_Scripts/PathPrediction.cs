using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class PathPrediction : MonoBehaviour
{
    public GameObject _pathPrefab;
    
    // path prediction
    private List<GameObject> _spheres;
    GameObject _futureCameraRig;
    GameObject _futureCamera;
    GameObject _futureRotationalCenter;

    private LocomotionInputAdapterInterface _locomotionInput;
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

        InitPathPrediction();
    }

    private void Update()
    {
        if (_locomotionInput.IsInitialized())
        {
            SimulateMovement();
        }
    }
    
    private void InitPathPrediction()
    {
        // setup path
        GameObject pathPrediction = new GameObject("PathPrediction");
        pathPrediction.transform.parent = this.transform;
        _spheres = new List<GameObject>();
        
        for (int i = 0; i < 50; ++i)
        {
            GameObject go = Instantiate(_pathPrefab, this.transform.position, Quaternion.identity, pathPrediction.transform);
            Color color = go.GetComponent<Renderer>().material.color;
            color.a = Mathf.Lerp(0.4f, 0.0f, i/50.0f);
            go.GetComponent<Renderer>().material.SetColor("_Color", color);
            _spheres.Add(go);
        }

        // create dummy copies of movement rig etc. for simulation
        _futureCameraRig = new GameObject("SimulatedCameraRig");
        _futureCameraRig.transform.SetParent(this.transform.parent);

        _futureCamera = new GameObject("SiumlatedCamera");
        _futureCamera.transform.SetParent(_futureCameraRig.transform);

        _futureRotationalCenter = new GameObject("SimulatedHeadJoint");
        _futureRotationalCenter.transform.SetParent(_futureCamera.transform);
    }
    
    private void SimulateMovement()
    {
        // make a copy of the current transform, working as its prediction  
        _futureCameraRig.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
        _futureCamera.transform.SetPositionAndRotation(_camera.transform.position, _camera.transform.rotation);
        _futureRotationalCenter.transform.position = _locomotionInput.GetCenterofRotation();
        
        // TODO generalize
        float futureRsaturatiuonTimer = GetComponent<HyperJump>().GetSaturationTimer();

        for (int i = 0; i < 50; ++i)
        {
            // TODO generalize
            GetComponent<HyperJump>().Translate(0.04f, _futureCameraRig.transform, _futureCamera.transform, ref futureRsaturatiuonTimer, true);
            Vector3 spherePosition = _spheres[i].transform.position;
            spherePosition.x = _futureCameraRig.transform.position.x;
            spherePosition.z = _futureCameraRig.transform.position.z;
            _spheres[i].transform.position = spherePosition;   
        }
    }
}
