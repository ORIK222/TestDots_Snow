// using System;
// using System.Collections.Generic;
// using DataModels;
// using UnityEngine;
//
// namespace Download.Core.Room
// {
//     public class RoomsManager : MonoBehaviour
//     {
//         [SerializeField] private RoomController lightBakedRoom;
//         [SerializeField] private RoomController roomForCopy;
//
//         private List<RoomController> allRooms = new List<RoomController>();
//         private Vector3 roomOffset;
//
//         private Collection currentCollection;
//
//         public static RoomsManager Instance;
//
//         private void Awake()
//         {
//             if (!Instance)
//             {
//                 Instance = this;
//                 allRooms.Add(lightBakedRoom);
//                 allRooms.Add(roomForCopy);
//                 roomOffset = roomForCopy.transform.position - lightBakedRoom.transform.position;
//             }
//             else
//                 Destroy(this);
//         }
//
//         public Collection Current => currentCollection;
//
//         public void OpenCollection(Collection collection)
//         {
//             Debug.Log("Open collection " + collection.Info.Name);
//             currentCollection = collection;
//             int roomIndex = 0;
//             foreach (var artWork in collection.ArtWorks)
//             {
//                 AddRoomIfEnded(roomIndex);
//
//                 // even if artwork comes with issues - we need to see them.
//                 while (roomIndex < allRooms.Count && allRooms[roomIndex].FreeSpace == 0)
//                 {
//                     roomIndex++;
//                 }
//
//                 AddRoomIfEnded(roomIndex);
//                 var roomController = allRooms[roomIndex];
//                 roomController.AddModel(artWork);
//             }
//
//             if (roomIndex >= allRooms.Count)
//                 roomIndex = allRooms.Count - 1;
//
//             // if collection is small - disable other rooms since we do not need them at the moment.
//             for (int i=0; i< allRooms.Count; i++)
//             {
//                 allRooms[i].EndingWall.SetActive(i == roomIndex);
//                 allRooms[i].gameObject.SetActive(i <= roomIndex);
//             }
//         }
//
//         public void ClearAllRooms()
//         {
//             foreach (var room in allRooms)
//             {
//                 room.ClearRoom();
//             }
//         }
//
//         private void AddRoomIfEnded(int roomIndex)
//         {
//             if (roomIndex >= allRooms.Count)
//             {
//                 // need to create additional room.
//                 var newRoom = Instantiate(roomForCopy);
//                 newRoom.transform.parent = roomForCopy.transform.parent;
//                 newRoom.transform.position = allRooms[roomIndex - 1].transform.position + roomOffset;
//                 newRoom.name = "SmallRoom_" + allRooms.Count;
//                 newRoom.Initialize();
//                 allRooms.Add(newRoom);
//             }
//         }
//     }
// }