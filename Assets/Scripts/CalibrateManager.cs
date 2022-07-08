 using System.Collections.Generic;
using UnityEngine;

namespace Download.Core
{
    public static class CalibrateManager
    {
        public static void Calibrate(GameObject obj, Vector3 maxSize) => GatherAllData(obj, maxSize);

        private static void GatherAllData(GameObject obj, Vector3 maxSize)
        {
            var meshes = GetMeshes(obj);

            FindSides(meshes,obj,out  _, out var sideVertices,out var center);

            Vector3 left = new Vector3(sideVertices[0].x,0,0);
            Vector3 right = new Vector3(sideVertices[1].x, 0, 0);
            Vector3 up = new Vector3(0, sideVertices[2].y, 0);
            Vector3 down = new Vector3(0, sideVertices[3].y, 0);
            Vector3 forward = new Vector3(0, 0, sideVertices[4].z);
            Vector3 backward = new Vector3(0, 0, sideVertices[5].z);
            

            var sizeX = Vector3.Distance(left, right);
            var sizeY = Vector3.Distance(up, down);
            var sizeZ = Vector3.Distance(forward, backward);

            var size = new Vector3(sizeX, sizeY, sizeZ);
            ChangeSize(obj, size, maxSize,center);
        }
        private static void ChangeSize(GameObject obj, Vector3 currentSize, Vector3 maxSize, Vector3 center)
        {
            //========= Set position to center =========

            var offset = obj.transform.localPosition - center;
            obj.transform.localPosition = offset;
            
            
            //========= Set scale ======================
            //Debug.Log("Size " + currentSize);
            var x = currentSize.x / maxSize.x;
            var y = currentSize.y / maxSize.y;
            var z = currentSize.z / maxSize.z;
            
            Debug.Log($"X({x})Y({y})Z({z})");
            var max = GetMax(x, y, z);
            
            if (max > 1)
                obj.transform.localScale /= max;
            
            var worldCenter = obj.transform.TransformVector(center);
            
            obj.transform.localPosition = -worldCenter;
        }
        private static float GetMax(float a, float b, float c)
        {
            var max = a;
            if (b > max) max = b;
            if (c > max) max = c;
            
            return max;
        }
        public  static  void FindSides(Mesh[] meshes,GameObject obj,out Mesh[] sideMeshes, out Vector3[] vertices,out Vector3 center)
        {
            vertices = new Vector3[] {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero,Vector3.zero, Vector3.zero, };
            sideMeshes = new Mesh[6];
            center = Vector3.zero;
            int vertexCount = 0;
            foreach (var mesh in meshes)
            {
                foreach (var vertex in mesh.vertices)
                {
                    if (vertex.x < obj.transform.TransformPoint(vertices[0]).x) //Left
                    {
                        vertices[0] = vertex;
                        sideMeshes[0] = mesh;
                    }
                    if (vertex.x > obj.transform.TransformPoint(vertices[1]).x) //Right
                    {
                        vertices[1] = vertex;
                        sideMeshes[1] = mesh;
                    }
                    if (vertex.y > obj.transform.TransformPoint(vertices[2]).y) //Up
                    {
                        vertices[2] = vertex;
                        sideMeshes[2] = mesh;
                    }
                    if (vertex.y < obj.transform.TransformPoint(vertices[3]).y) //Down
                    {
                        vertices[3] = vertex;
                        sideMeshes[3] = mesh;
                    }
                    if (vertex.z < obj.transform.TransformPoint(vertices[4]).z) //Forward
                    {
                        vertices[4] = vertex;
                        sideMeshes[4] = mesh;
                    }
                    if (vertex.z > obj.transform.TransformPoint(vertices[5]).z) //Backward
                    {
                        vertices[5] = vertex;
                        sideMeshes[5] = mesh;
                    }

                    center += vertex;
                    vertexCount++;
                }
            }
            center /= vertexCount;
        }
        public static  Mesh[] GetMeshes(GameObject obj)
        {
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            var meshes = new List<Mesh>();
            foreach (var meshFilter in meshFilters)
                meshes.Add(meshFilter.sharedMesh);
            return meshes.ToArray();
        }
        
        
    }
}