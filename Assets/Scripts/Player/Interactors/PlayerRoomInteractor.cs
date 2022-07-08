// using System;
// using _Core.Scripts.Player.Interactors;
// using UnityEngine;
//
// namespace DefaultNamespace
// {
//     public class PlayerRoomInteractor : MonoBehaviour
//     {
//         [SerializeField] private float resetDistance;
//         [SerializeField] private HandController leftHand;
//         [SerializeField] private HandController rightHand;
//         [SerializeField] private Transform interactHolder;
//
//
//
//         private Transform interactObjectStartParrent;
//         private Vector3 interactObjectStartScale;
//
//         private Vector3 interactHolderOffset;
//         private Vector3 startScaleValue;
//         private Quaternion startRotation;
//
//         private bool isDragLastObject;
//
//         private Vector3 startPosition;
//         public GameObject InteractObject { get; set; } = null;
//
//         private bool IsGripsPressed => leftHand.IsGripPressed && rightHand.IsGripPressed;
//         private bool IsRightGripPressed => rightHand.IsGripPressed;
//
//         private bool lastRightGripState;
//         private bool lastGripsState;
//         private float startDistance;
//
//         private bool isInteractWithOneHand;
//         
//         private void Update()
//         {
//
//             if (!lastGripsState && IsGripsPressed && InteractObject)
//             {
//                 if(isInteractWithOneHand)
//                     StopInteract_OneHand();
//                 StartInteract();
//             }
//             else if (!lastRightGripState && IsRightGripPressed)
//             {
//                 if(lastGripsState && !IsGripsPressed)
//                     StopInteract();
//                 StartInteract_OneHand();
//             }
//             
//             if (IsGripsPressed && InteractObject && !isInteractWithOneHand)
//                 UpdateScale();
//             else if (IsRightGripPressed && InteractObject && IsRightGripPressed)
//                 UpdatePositionAndRotation_OneHand();
//             
//             if (lastGripsState && !IsGripsPressed && InteractObject && !isInteractWithOneHand)
//                 StopInteract();
//             else if(lastRightGripState && !IsRightGripPressed && isInteractWithOneHand)
//                 StopInteract_OneHand();
//
//             lastGripsState = IsGripsPressed;
//             lastRightGripState = IsRightGripPressed;
//         }
//
//         public void ResetObjectData()
//         {
//             InteractObject.transform.SetParent(interactObjectStartParrent);
//             InteractObject.transform.localPosition = startPosition;
//             InteractObject.transform.localRotation = startRotation;
//             InteractObject.transform.localScale = interactObjectStartScale;
//             InteractObject = null;
//             isDragLastObject = false;
//         }
//
//         private void StopInteract()
//         {
//             isDragLastObject = true;
//         }
//         private void StopInteract_OneHand()
//         {
//             InteractObject.transform.SetParent(interactObjectStartParrent, true);
//             isDragLastObject = true;
//         }
//
//         void Scale(float value)
//         {
//             var change = value - startDistance;
//             if (change > 0.03f || change < -0.03f)
//             {
//                 InteractObject.transform.localScale = new Vector3(startScaleValue.x + change,
//                     startScaleValue.y + change, startScaleValue.z + change);
//             }
//         }
//
//         void StartInteract()
//         {
//             //Debug.Log("Start Interact");
//             
//             interactObjectStartScale = InteractObject.transform.localScale;
//             startDistance = Vector3.Distance(leftHand.Position,rightHand.Position);
//             startScaleValue = InteractObject.transform.localScale;
//             startPosition = InteractObject.transform.position;
//             isInteractWithOneHand = false;
//         }
//         void StartInteract_OneHand()
//         {
//             //Debug.Log("Start Interact");
//             interactHolder.transform.position = rightHand.transform.position;
//             interactHolder.transform.rotation = rightHand.transform.rotation;
//             
//             interactObjectStartParrent = InteractObject.transform.parent;
//             startRotation = InteractObject.transform.rotation;
//             interactObjectStartScale = InteractObject.transform.localScale;
//             
//             InteractObject.transform.SetParent(interactHolder,true);
//             startDistance = Vector3.Distance(leftHand.Position,rightHand.Position);
//             startScaleValue = InteractObject.transform.localScale;
//             startPosition = InteractObject.transform.position;
//             isInteractWithOneHand = true;
//         }
//
//
//         void UpdateScale()
//         {
//             //Debug.Log("Update position and rotation");
//             var distanceBetweenHands = Vector3.Distance(leftHand.Position, rightHand.Position);
//                 Scale(distanceBetweenHands);
//         }
//         void UpdatePositionAndRotation_OneHand()
//         {
//             //Debug.Log("Update position and rotation");
//             interactHolder.transform.position = rightHand.transform.position;
//             interactHolder.transform.rotation = rightHand.transform.rotation;
//         }
//     }
// }