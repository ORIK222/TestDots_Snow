// #if UNITY_EDITOR
//
// using Download.Core.Room;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
//
// public class EditorExibitionRoomMEnu : MonoBehaviour
// {
//     [MenuItem("GameObject/Fix exhibition controller.", false, 10)]
//     public static void FixController()
//     {
//         var objects = Selection.gameObjects;
//         int propertiesUpdated = 0;
//         foreach (var obj in objects)
//         {
//             RoomController controller = obj.GetComponent<RoomController>();
//             var roomSerialized = new SerializedObject(controller);
//             var propertyRoom = roomSerialized.FindProperty("exhibitControllers");
//             var controllers = new List<ExhibitController>();
//             if (propertyRoom == null)
//             {
//                 Debug.LogError("Missing property exhibitControllers in RoomController!");
//             }
//
//             var allExhibitControllers = controller.GetComponentsInChildren<ExhibitController>(true);
//             foreach(var c in allExhibitControllers)
//             {
//                 if (c.transform.parent.gameObject.activeSelf)
//                 {
//                     var name = c.transform.parent.gameObject.name;
//                     int i1 = name.LastIndexOf('_');
//                     if (i1 >= 0)
//                     {
//                         string nameFix = name.Substring(i1, name.Length - i1);
//                         if (!c.name.EndsWith(nameFix))
//                         {
//                             c.name += nameFix;
//                         }
//                     }
//
//                     controllers.Add(c);
//                 }
//
//                 var so = new SerializedObject(c);
//                 var property = so.FindProperty("trigger");
//                 if (property!=null)
//                 {
//                     if (property.objectReferenceValue == null && c.transform.parent != null)
//                     {
//                         var trigger = c.transform.parent.GetComponent<ExhibitionTrigger>();
//                         if (trigger != null)
//                         {
//                             property.objectReferenceValue = trigger;
//                             so.ApplyModifiedProperties();
//                             propertiesUpdated++;
//                         }
//                         else
//                         {
//                             Debug.LogError("Incorrect ExhibitController controller setup - should be a child of ExhibitionTrigger.");
//                         }
//                     }
//                 }
//             }
//
//             // fixing room controller.
//             propertyRoom.arraySize = controllers.Count;
//             for (int i=0; i<controllers.Count; i++)
//             {
//                 var item = propertyRoom.GetArrayElementAtIndex(i);
//                 item.objectReferenceValue = controllers[i];
//                 propertiesUpdated++;
//             }
//
//             roomSerialized.ApplyModifiedProperties();
//         }
//
//         Debug.Log("Success. Updated " + propertiesUpdated + " properties.");
//     }
// }
// #endif
