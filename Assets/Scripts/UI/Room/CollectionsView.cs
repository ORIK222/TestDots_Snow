// using System.Collections.Generic;
// using DataModels;
// using Download.Core.Room;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace Download.Core.UI.Room
// {
//     public class CollectionsView : MonoBehaviour
//     {
//         [SerializeField] private GameObject buttonPrefab;
//         [SerializeField] private Transform buttonsParent;
//         [SerializeField] private RoomsManager roomsManager;
//         [SerializeField] private bool isSelectOnStart;
//         [SerializeField] private int selectIndex;
//
//         private List<CollectionView> _allCollectionViews;
//         private Data.Core.DataManager dataManager => Data.Core.DataManager.Instance;
//
//         private void Start()
//         {
//             if (dataManager.IsArtworksInitialized)
//             {
//                 UpdateCollectionList();
//                 if(isSelectOnStart)
//                     SelectStartCollection();
//             }
//             else
//             {
//                 dataManager.OnArtworksInitialized.AddListener(UpdateCollectionList);
//                 //if(isSelectOnStart)
//                 //    dataManager.OnArtworksInitialized.AddListener(SelectStartCollection);
//             }
//         }
//
//         void SelectStartCollection()
//         {
//             SelectCollection(dataManager.CollectionHolder.Collections[selectIndex]);
//         }
//
//         void UpdateCollectionList()
//         {
//             _allCollectionViews ??= new List<CollectionView>();
//
//             if (_allCollectionViews.Count != 0)
//             {
//                 foreach (var view in _allCollectionViews)
//                     Destroy(view.gameObject);
//                 _allCollectionViews.Clear();
//             }
//
//             foreach (var collection in dataManager.CollectionHolder.Collections)
//             {
//                 var panelObject = Instantiate(buttonPrefab, buttonsParent);
//                 var panel = panelObject.GetComponent<CollectionView>();
//                 _allCollectionViews.Add(panel);
//                 panel.Init(collection, () => SelectCollection(collection));
//             }
//             if(isSelectOnStart)
//                 SelectCollection(dataManager.CollectionHolder.Collections[selectIndex]);
//         }
//
//         void SelectCollection(Collection collection)
//         {
//             if (collection != roomsManager.Current)
//             {
//                 roomsManager.ClearAllRooms();
//                 roomsManager.OpenCollection(collection);
//                 dataManager.LoadCollection(collection);
//             }
//         }
//     }
// }