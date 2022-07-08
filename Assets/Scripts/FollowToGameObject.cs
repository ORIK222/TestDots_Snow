using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Download.Core
{
    public class FollowToGameObject : MonoBehaviour
    {
        private Entity[] entities;
        private Vector3 offsetPosition;
        private Quaternion offsetRotation;
        private EntityManager entityManager;

        public void SetEntityArray(Entity[] entities, Vector3 offsetPosition, Quaternion offsetRotation)
        {
            this.entities = entities;
            this.offsetPosition = offsetPosition;
            this.offsetRotation = offsetRotation;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public void ClearEntityArray()
        {
            this.entities = null;
        }

        private void Update()
        {
            if (this.entities != null)
            {
                foreach (var entity in this.entities)
                {
                    entityManager.SetComponentData(entity, new Translation() { Value = transform.position + offsetPosition });
                    entityManager.SetComponentData(entity, new Rotation() { Value = transform.rotation * offsetRotation });
                }
            }
        }

        private void OnDestroy()
        {
            this.entities = null;
        }
    }
}