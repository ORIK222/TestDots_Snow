// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Download.Core.Editor;
// using UnityEngine;
//
// namespace Download.Core.Room
// {
//     public class RoomController : MayInitialize
//     {
//         [SerializeField] private List<ExhibitController> exhibitControllers;
//         private List<ExhibitionTrigger> exhibitionTriggers = new List<ExhibitionTrigger>();
//
//         [SerializeField] private GameObject endingWall;
//
//         public GameObject EndingWall => endingWall;
//         
//         /// <summary>
//         /// Gets the free space available.
//         /// </summary>
//         /// <value>
//         /// The free space.
//         /// </value>
//         public int FreeSpace => exhibitControllers.Count - exhibitControllers.Count(x => x.IsOccupied);
//
//         protected override void OnInitialize()
//         {
//             foreach (var controller in exhibitControllers)
//             {
//                 controller.Initialize();
//                 exhibitionTriggers.Add(controller.Trigger);
//             }
//         }
//
//         private void OnEnable()
//         {
//             foreach (var trigger in exhibitionTriggers)
//                 trigger.OnPlayerComes += CheckRoomState;
//         }
//         
//         private void OnDisable()
//         {
//             foreach (var trigger in exhibitionTriggers)
//                 trigger.OnPlayerComes -= CheckRoomState;
//         }
//
//         public bool RemoveModel(GLBData model)
//         {
//             bool removed = false;
//             var controller = exhibitControllers.FirstOrDefault(x => x.Model == model);
//             if (controller != null)
//             {
//                 controller.RemoveModel(model);
//                 removed = true;
//             }
//
//             return removed;
//         }
//
//         public bool AddModel(GLBData model)
//         {   
//             Debug.Log("Add model");
//             bool added = false;
//             if (FreeSpace > 0)
//             {
//                 exhibitControllers.First(x => !x.IsOccupied).AddModel(model);
//                 added = true;
//             }
//
//             return added;
//         }
//
//         private void CheckRoomState()
//         {
//             /*int playerComesRoomIndex = 0;
//             
//             for (int i = 0; i < rooms.Count; i++)
//             {
//                 if (rooms[i].CheckRoomState())
//                 {
//                     playerComesRoomIndex = i;
//                     break;
//                 }
//             }
//             
//             for (int i = 0; i < rooms.Count; i++)
//             {
//                 rooms[i].ArtworkSetActive(Mathf.Abs(playerComesRoomIndex - i) <= 1);
//                 exhibitControllers[i].ModelEntitySetActive(Mathf.Abs(playerComesRoomIndex - i) <= 1);
//             }*/
//         }
//
//         public void ClearRoom()
//         {
//             foreach (var controller in exhibitControllers)
//             {
//                 controller.Clear();
//             }
//         }
//     }
// }