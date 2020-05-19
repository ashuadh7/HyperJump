using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CoucheronManager : MonoBehaviour
{
    private static CoucheronManager _instance = null;

    public GameObject framingObject;

    private List<GameObject> frame;

    public static CoucheronManager Instance
    {
        get
        {
            return _instance;
        }
    }

    private void Awake()
    {
        // if the singleton hasn't been initialized yet
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        frame = new List<GameObject>(); 
    }


    public void AddSphere(Vector3 position)
    {
        GameObject go = Instantiate(framingObject, position, Quaternion.identity, this.transform);
        frame.Add(go);

        if (frame.Count == 1)
        {
            go.GetComponent<FrameSphere>().SetFirstOnePlaced();
        }
    }

    public void HandleTriggerOnCollision(Collider other)
    {
        // user wants to delete an existing sphere to correct its positon
        if (!other.transform.gameObject.GetComponent<FrameSphere>().GetIsFirstPlaced())
        {
            Destroy(other.transform.gameObject);
        }
        // user has finished to define a surface
        // 1. create a volume
        // 2. delete all spheres and reinit data structures
        else
        {
            // create a polygon from the set of definded spheres. Whatch out! Some spheres might got deleted
            List<Vector3> surfacePolygon = new List<Vector3>();
            foreach(GameObject elem in frame)
            {
                if(elem != null)
                {
                    surfacePolygon.Add(elem.transform.position);
                    Destroy(elem.transform.gameObject);
                }
            }

            if(surfacePolygon.Count < 3)
            {
                Debug.Log("Error: Surface is underspecified");
                ResetFrame();
                return;
            }

            GameObject newGameObject = new GameObject("Coucheron");
            newGameObject.transform.parent = this.transform;
            DrawMeshFromPolygon(newGameObject, surfacePolygon, null, 0.1f);
        }
        ResetFrame();
    }

    private void ResetFrame()
    {
        frame = new List<GameObject>();
    }

    private void DrawMeshFromPolygon(GameObject go, List<Vector3> polygon, Material mat, float height)
    {
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.mesh = Triangulator.CreateExtrudedMeshFromPolygon(polygon, height);
        Renderer rend = go.AddComponent<MeshRenderer>();
        rend.material = mat;
    }
}


