using UnityEngine;

namespace Download.Core.Editor
{
    public class GizmosSphere : MonoBehaviour
    {
        [SerializeField] private float radius;
        [SerializeField] private Color color;
        [SerializeField] private bool isDraw;


        private void OnDrawGizmos()
        {
            if (isDraw)
            {
                Gizmos.color = color;
                Gizmos.DrawSphere(transform.position,radius);
            }
        }
    }
}