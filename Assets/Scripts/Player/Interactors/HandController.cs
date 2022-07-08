// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.InputSystem;
// using UnityEngine.InputSystem.XR;
// using UnityEngine.XR;
// using UnityEngine.XR.Interaction.Toolkit;
//
// namespace _Core.Scripts.Player.Interactors
// {
//     public class HandController : MonoBehaviour
//     {
//         [SerializeField] private InputActionAsset actionsAsset;
//         [SerializeField] private string actionsMap;
//         private XRBaseController xrController;
//         private Animator animator;
//         private Vector3 lastPosition;
//         private GameObject interactObject = null;
//
//         public Vector3 LastPosition => lastPosition;
//         public Vector3 Position => transform.position;
//         
//         public bool IsGripPressed { get; set; }
//
//         public UnityEvent OnInteractStart = new UnityEvent();
//         public UnityEvent OnInteractEnd = new UnityEvent();
//
//         public UnityEvent OnGripPress = new UnityEvent();
//         public UnityEvent OnGripRelease = new UnityEvent();
//
//         private void Start()
//         {
//             var holdAction = actionsAsset.FindActionMap(actionsMap).FindAction("Select");
//             
//             holdAction.performed += delegate(InputAction.CallbackContext context) { IsGripPressed = true;
//                 Debug.Log($"Press {gameObject.name}");};
//             holdAction.canceled += delegate(InputAction.CallbackContext context) { IsGripPressed = false;
//                 Debug.Log($"Cencel {gameObject.name}");};
//         }
//     }
// }