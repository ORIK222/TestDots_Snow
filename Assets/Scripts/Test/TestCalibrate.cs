using System;
using System.Collections;
using System.Collections.Generic;
using Download.Core;
using UnityEngine;

public class TestCalibrate : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private string path;
    [SerializeField] private bool isDrawGizmos;
    [SerializeField] private float gizmosSize;

    private Mesh[] sideMeshes;
    private Vector3[] vertices;
    private Vector3 center;
    
    void Start()
    {
        GLBLoader.Load(path,
            (GameObject gameObject) =>
            {
                gameObject.transform.SetParent(parent);
                gameObject.transform.localPosition = Vector3.zero;
                CalibrateManager.Calibrate(gameObject, new Vector3(1, 1, 1));
                var meshes = CalibrateManager.GetMeshes(gameObject);
                CalibrateManager.FindSides(meshes, gameObject, out var sideMeshes, out var vertices, out var center);
                this.sideMeshes = sideMeshes;
                this.vertices = vertices;
                this.center = center;
            },
            (Exception exc) => Debug.LogException(exc));
    }

    private void OnDrawGizmos()
    {
        if (isDrawGizmos)
        {
            if (sideMeshes != null && vertices != null && center != null)
            {
                Gizmos.color = Color.red;
                foreach (var vector3 in vertices)
                {
                    Gizmos.DrawSphere(vector3, gizmosSize);
                }

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(center, gizmosSize);
            }
        }
    }
}
