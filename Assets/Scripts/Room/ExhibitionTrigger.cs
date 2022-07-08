// using System;
// using DefaultNamespace;
// using UnityEngine;
//
// public class ExhibitionTrigger : MonoBehaviour
// {
//     public Action OnPlayerComes;
//     
//     [SerializeField] private GameObject interactObject;
//     
//     private bool playerComes;
//     private PlayerRoomInteractor interactor;
//     private Vector3 startInteractPosition;
//     private Quaternion startInteractorRotation;
//
//     private void OnTriggerEnter(Collider other)
//     {
//         Debug.Log($"Enter {other.gameObject.name}");
//         bool isPlayer = other.gameObject.CompareTag("Player");
//         bool isMainCamera = other.gameObject.CompareTag("MainCamera");
// #if UNITY_EDITOR || UNITY_WEBGL
//         if (isPlayer || isMainCamera)
// #else
//         if (isPlayer)
// #endif
//         {
//             playerComes = true;
//             Debug.Log("Interactor");
//             startInteractPosition = interactObject.transform.position;
//             startInteractorRotation = interactObject.transform.rotation;
// #if UNITY_EDITOR || UNITY_WEBGL
//             if (RuntimeControl.Instance.IsVR)
// #endif
//             {
//                 interactor = other.GetComponentInChildren<PlayerRoomInteractor>();
//                 interactor.InteractObject = interactObject;
//             } 
//             
//             OnPlayerComes?.Invoke();
//             Data.Core.DataManager.Instance.LoadNextPendingArts();
//         }
//     }
//
//     private void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             playerComes = false;
//             if (interactObject != null)
//             {
//                 interactObject.transform.position = startInteractPosition;
//                 interactObject.transform.rotation = startInteractorRotation;
//             }
//         }
//     }
//
//     public bool CheckRoomState()
//     {
//         return playerComes;   
//     }
//
//     public void ArtworkSetActive(bool state)
//     {
//         if(interactObject != null)
//             interactObject.SetActive(state);
//     }
// }
