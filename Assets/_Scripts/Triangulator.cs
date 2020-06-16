using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangulator
{
    public static Mesh CreateExtrudedMeshFromPolygon(List<Vector3> polygon, float height)
    {
        List<int> triangleList = new List<int>();

        // create vertices for floor, roof and side
        List<Vector3> vertListFloor = new List<Vector3>();
        List<Vector3> vertListRoof = new List<Vector3>();
        List<Vector3> vertListSide = new List<Vector3>();

        foreach (Vector3 vertex in polygon)
        {
            vertListFloor.Add(vertex);
            vertListRoof.Add(new Vector3(vertex.x, vertex.y + height, vertex.z));
        }
        vertListSide.AddRange(vertListFloor);
        vertListSide.AddRange(vertListRoof);

        // compute polygon center and add it to verts
        Vector3 center = FindPolygonCenter(polygon);
        vertListFloor.Add(center);
        vertListRoof.Add(new Vector3(center.x, center.y + height, center.z));

        // add floor triangles
        for (int i = 0; i < vertListFloor.Count; i++)
        {
            triangleList.Add(i);
            triangleList.Add((i + 1) % (vertListFloor.Count - 1));
            triangleList.Add(vertListFloor.Count - 1); // center           
        }

        // add roof triangles
        int maxCurrGobalVertID = vertListFloor.Count;
        for (int i = 0; i < vertListRoof.Count - 1; i++)
        {
            triangleList.Add(i + maxCurrGobalVertID);
            triangleList.Add(vertListRoof.Count - 1 + maxCurrGobalVertID); // upper center 
            triangleList.Add((i + 1) % (vertListRoof.Count - 1) + maxCurrGobalVertID);
        }

        // add side triangles 
        maxCurrGobalVertID = vertListFloor.Count + vertListRoof.Count;
        int numOfBaseVert = polygon.Count;
        for (int j = 0; j < numOfBaseVert; ++j)
        {
            triangleList.Add(j + 0 + maxCurrGobalVertID);
            triangleList.Add((j + 0 + numOfBaseVert) + maxCurrGobalVertID);
            triangleList.Add((j + 1) % numOfBaseVert + numOfBaseVert + maxCurrGobalVertID);

            triangleList.Add(j + 0 + maxCurrGobalVertID);
            triangleList.Add((j + 1) % numOfBaseVert + numOfBaseVert + maxCurrGobalVertID);
            triangleList.Add((j + 1) % numOfBaseVert + maxCurrGobalVertID);
        }

        List<Vector3> vertList = new List<Vector3>();
        vertList.AddRange(vertListFloor);
        vertList.AddRange(vertListRoof);
        vertList.AddRange(vertListSide);

        //Vector2[] test = UVcalculator.CalculateUVs(vertList.ToArray(), 1f);

        Mesh mesh = new Mesh();
        mesh.vertices = vertList.ToArray();
        mesh.triangles = triangleList.ToArray();
        //mesh.uv = test;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    public static Mesh CreateExtrudedMeshFromPolygon(List<Vector2> polygon, float height)
    {
        List<Vector3> extendedPolygon = new List<Vector3>();
        foreach (Vector2 vertex in polygon)
        {
            extendedPolygon.Add(new Vector3(vertex.x, 0.01f, vertex.y));
        }

        return CreateExtrudedMeshFromPolygon(extendedPolygon, height);
    }

    private static Vector3 FindPolygonCenter(List<Vector3> verts)
    {
        Vector3 center = Vector3.zero;
        // Only need to check every other spot since the odd indexed vertices are in the air, but have same XZ as previous
        for (int i = 0; i < verts.Count; ++i)
        {
            center += verts[i];
        }
        return center / verts.Count;
    }
}
