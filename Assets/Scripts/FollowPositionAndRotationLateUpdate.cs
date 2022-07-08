using UnityEngine;

public class FollowPositionAndRotationLateUpdate : MonoBehaviour
{
	[SerializeField] private Transform target;

	// Update is called once per frame
	void LateUpdate()
	{
		if( target != null )
		{
			transform.position = target.position;
			transform.rotation = target.rotation;
		}
	}
}
