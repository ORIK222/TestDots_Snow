// using System;
// using System.IO;
// using System.Numerics;
// using Download.Core.Editor;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Rendering;
// using Unity.Transforms;
// using UnityEngine;
// using UnityEngine.XR.Interaction.Toolkit;
// using Quaternion = UnityEngine.Quaternion;
// using Vector2 = UnityEngine.Vector2;
// using Vector3 = UnityEngine.Vector3;
//
// namespace Download.Core.Room
// {
//     [ExecuteAlways]
//     public class ExhibitController : MayInitialize
//     {
//         [SerializeField] private Transform modelSpawnTransform;
//         [SerializeField] private ExhibitView exhibitView;
//         [SerializeField] private DescriptionPanel descriptionPanel;
//         [SerializeField] private Canvas canvas;
//         [SerializeField] private GLBData model;
//         [SerializeField] private ExhibitionTrigger trigger;
//
//         private GameObject _modelObject;
//         private Entity _modelEntity;
//         private Entity[] entities;
//         private EntityManager _entityManager;
//         private BlobAssetStore _blobAssetStore;
//         private GameObjectConversionSettings _settings;
//         private FollowToGameObject follow;
//         private bool isOccupied;
//
// #if UNITY_EDITOR || UNITY_WEBGL
//         private void Awake()
//         {
//             // quick setup check.
//             trigger = this.GetComponentInParent<ExhibitionTrigger>();
//             if (trigger == null)
//             {
//                 Debug.LogError(
//                     "Incorrect setup for exhibition controller. Should be below exhibition trigger component! Name: " +
//                     this.gameObject.name);
//             }
//         }
// #endif
//
//         protected override void OnInitialize()
//         {
//             canvas.worldCamera = Camera.main;
//             this.exhibitView.Initialize();
//             this.exhibitView.gameObject.SetActive(true);
//             this.exhibitView.DisablePreview();
//             this.exhibitView.SetLoadModelState("");
//             this.descriptionPanel.gameObject.SetActive(false);
//
//             // test code.
//             /*GLBData testData = new GLBData(DataModels.ObjectData.GetTestData(), new DataModels.Urls(@"c:\Projects\SnowX\snowx-quest_original\snowx-quest\Assets\TestDownload.glb", null));
//             string path = Path.Combine(Application.dataPath, @"Resources\Downloads\Glb\LIFE is inc. collection\AKA-ZONAE\AKA-ZONAE.glb");
//             Debug.Log(path);
//             testData.FilePath = path;
//             this.AddModel(testData);
//             LoadModel();*/
//         }
//
//         public bool IsOccupied
//         {
//             get => isOccupied;
//         }
//
//         public ExhibitionTrigger Trigger
//         {
//             get => trigger;
//         }
//
//         public GLBData Model => this.model;
//
//         public void RemoveModel(GLBData model)
//         {
//             if (model == null)
//             {
//                 throw new ArgumentNullException(nameof(model));
//             }
//
//             if (this.model == model)
//             {
//                 this.Clear(false);
//             }
//         }
//
//         public void AddModel(GLBData model)
//         {
//             this.model = model;
//             LoadModel();
//         }
//
//         public void ModelEntitySetActive(bool state)
//         {
//             if (_modelEntity != Entity.Null)
//             {
//                 _entityManager.SetEnabled(_modelEntity, state);
//             }
//         }
//
//         private void StateUpdate()
//         {
//             switch (model.State)
//             {
//                 case DataState.DecompressQueue:
//                     exhibitView.SetLoadModelState("Decompress queue");
//                     break;
//                 case DataState.Decompress:
//                     exhibitView.SetLoadModelState("Decompressing");
//                     break;
//                 case DataState.DownloadFile:
//                     exhibitView.SetLoadModelState("Downloading file");
//                     break;
//                 case DataState.DownloadPreview:
//                     exhibitView.SetLoadModelState("Downloading preview");
//                     break;
//                 case DataState.GetSize:
//                     exhibitView.SetLoadModelState("Getting size");
//                     break;
//                 case DataState.DownloadFileQueue:
//                     exhibitView.SetLoadModelState("Download file queue");
//                     break;
//                 case DataState.DownloadPreviewQueue:
//                     exhibitView.SetLoadModelState("Download preview queue");
//                     break;
//                 case DataState.GetSizeQueue:
//                     exhibitView.SetLoadModelState("Get size queue");
//                     break;
//                 case DataState.UpToDate:
//                     exhibitView.SetLoadModelState("Creating model");
//                     break;
//                 case DataState.Error:
//                     exhibitView.SetLoadModelErrorState(model.ErrorText);
//                     break;
//                 case DataState.Queued:
//                     exhibitView.SetLoadModelState("Waiting for processing...");
//                     break;
//                 case DataState.StartCashing:
//                     exhibitView.SetLoadModelState("Start cashing");
//                     break;
//                 case DataState.Cashed:
//                     exhibitView.SetLoadModelState("Cashed");
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }
//         }
//
//         private AsyncController controller;
//
//         private void LoadModel()
//         {
//             if (controller != null)
//             {
//                 // cancel controller.
//                 controller.DestroyMyself();
//                 controller = null;
//             }
//             print(model.CashedData);
//             GLBLoader.LoadTiltBrush(new MemoryStream(model.CashedData.bytes), ProcessLoadModel, FailedLoadModel);
//
//         }
//
//         private void ProcessLoadModel(GameObject receivedModel)
//         {
//             try
//             {
//                 if (model != null)
//                 {
//                     if (_modelObject != null)
//                     {
//                         Clear(true);
//                     }
//
//                     _modelObject = receivedModel;
//                     _modelObject.SetActive(true);
//                     if (model.Environment != null && model.Environment.BackgroundColor != null &&
//                         model.Environment.BackgroundColor.Color != null &&
//                         model.Environment.BackgroundColor.Color.Length == 7)
//                     {
//                         string col = model.Environment.BackgroundColor.Color.Substring(1,
//                             model.Environment.BackgroundColor.Color.Length - 1);
//                         int colInt = Convert.ToInt32(col, 16);
//                         float r = (float)((colInt & 0xff0000) >> 16) / 255.0f;
//                         float g = (float)((colInt & 0xff00) >> 8) / 255.0f;
//                         float b = (float)(colInt & 0xff) / 255.0f;
//                         Color unityColor = new Color(r, g, b);
//                         exhibitView.DisablePreview(unityColor);
//                     }
//                     else
//                     {
//                         exhibitView.DisablePreview();
//                     }
//
//                     SetUpModelObject(receivedModel, modelSpawnTransform.gameObject);
//                     exhibitView.SetLoadModelState("");
//                 }
//                 else
//                 {
//                     Destroy(receivedModel);
//                 }
//             }
//             finally
//             {
//                 controller = null;
//             }
//         }
//         private void FailedLoadModel(Exception ex)
//         {
//             if (controller != null && controller.gameObject != null)
//             {
//                 controller = null;
//                 model.ErrorText = ex.ToString();
//                 model.State = DataState.Error;
//                 isOccupied = false;
//             }
//         }
//         
//         private void SetPreviewImage()
//         {
//             if (model.CashedPreview != null)
//             {
//                 Rect spriteRect = new Rect(Vector2.zero, new Vector2(model.CashedPreview.width, model.CashedPreview.height));
//                 Sprite sprite = Sprite.Create(model.CashedPreview, spriteRect, new Vector2(0.5f, 0.5f));
//                 exhibitView.SetPreviewImage(sprite);
//                 exhibitView.gameObject.SetActive(true);
//             }
//             else
//             {
//                 Texture2D texture = new Texture2D(10, 10);
//                 byte[] imageBytes = File.ReadAllBytes(model.PreviewPath);
//                 texture.LoadImage(imageBytes);
//                 Rect spriteRect = new Rect(Vector2.zero, new Vector2(texture.width, texture.height));
//                 Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f));
//                 exhibitView.SetPreviewImage(sprite);
//                 exhibitView.gameObject.SetActive(true);
//             }
//         }
//         
//         private void SetUpModelObject(GameObject modelObject, GameObject parent)
//         {
//             try
//             {
//                 _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
//
//                 var startParent = parent.transform.parent;
//                 var startPosition = parent.transform.position;
//                 parent.transform.SetParent(null);
//                 parent.transform.position = Vector3.zero;
//                 parent.transform.localScale = Vector3.one;
//
//                 modelObject.transform.parent = parent.transform;
//                 modelObject.transform.localPosition = Vector3.zero;
//                 modelObject.transform.rotation = Quaternion.identity;
//                 CalibrateManager.Calibrate(modelObject, new Vector3(3, 3, 3));
//                 parent.gameObject.layer = 7;
//                 parent.gameObject.tag = "Grabbable";
//                 parent.transform.SetParent(startParent);
//                 parent.transform.position = startPosition;
//                 var xrGrabInteractable = parent.AddComponent<XRGrabInteractable>();
// #if UNITY_EDITOR || UNITY_WEBGL
//                 if (RuntimeControl.Instance.IsVR)
// #endif
//                 {
//                     xrGrabInteractable.interactionManager = XRInteractionManagerStaticAccess.Instance;
//                     xrGrabInteractable.interactionLayers = InteractionLayerMask.GetMask("Default");
//                 }
//
//                 parent.gameObject.name = "GltfParent";
//                 var boxCollider = parent.gameObject.AddComponent<BoxCollider>();
//                 boxCollider.size = Vector3.one;
//                 boxCollider.isTrigger = true;
//                 var parentRigidbody = parent.gameObject.GetComponent<Rigidbody>();
//                 parentRigidbody.isKinematic = true;
//                 parentRigidbody.useGravity = false;
//                 parentRigidbody.constraints = RigidbodyConstraints.FreezeAll;
//                 
//                 CreateDotsEntity(_modelObject,parent,Vector3.zero,Quaternion.identity);
//                 Destroy(modelObject);
//             }
//             catch(Exception exc)
//             {
//                 Debug.LogError("Exception! " + exc);
//             }
//         }
//         
//         private void DeleteEntities()
//         {
//             if (entities != null)
//             {
//                 if (this.follow != null)
//                 {
//                     this.follow.ClearEntityArray();
//                 }
//
//                 foreach (var entity in entities)
//                 {
//                     _entityManager.DestroyEntity(entity);
//                 }
//
//                 entities = null;
//             }
//         }
//
//         private void CreateDotsEntity(GameObject modelRoot, GameObject parent, Vector3 offsetPosition, Quaternion offsetRotation)
//         {
//             modelRoot.SetActive(true);
//             EntityArchetype childEntityArchetype = _entityManager.CreateArchetype(
//                 typeof(Translation),
//                 typeof(Rotation),
//                 typeof(NonUniformScale),
//                 typeof(RenderMesh),
//                 typeof(RenderBounds),
//                 typeof(LocalToWorld),
//                 typeof(BuiltinMaterialPropertyUnity_LightData),
//                 typeof(BuiltinMaterialPropertyUnity_WorldTransformParams),
//                 typeof(BuiltinMaterialPropertyUnity_RenderingLayer));
//             
//             MeshFilter[] meshFilters = modelRoot.GetComponentsInChildren<MeshFilter>();
//             NativeArray<Entity> entityArray = new NativeArray<Entity>(meshFilters.Length, Allocator.Temp);
//             DeleteEntities();
//             this.follow = parent.AddComponent<FollowToGameObject>();
//             this.entities = new Entity[entityArray.Length];
//             this.follow.SetEntityArray(entities, offsetPosition, offsetRotation);
//
//             try
//             {
//                 // create entities
//                 _entityManager.CreateEntity(childEntityArchetype, entityArray);
//                 _entityManager.AddComponent<WorldRenderBounds>(entityArray);
//
//                 Debug.Log("Creating DOTS entity: Local scale: " + modelRoot.transform.localScale);
//
//                 for (int i = 0; i < meshFilters.Length; i++)
//                 {
//                     var filter = meshFilters[i];
//                     Entity entity = entityArray[i];
//                     entities[i] = entity;
//                     var renderer = filter.gameObject.GetComponent<Renderer>();
//                     if (renderer == null)
//                     {
//                         throw new Exception("The mesh renderer was not found.");
//                     }
//
//                     _entityManager.SetSharedComponentData(entity, new RenderMesh
//                     {
//                         mesh = filter.sharedMesh,
//                         material = renderer.sharedMaterial,
//                         layerMask = renderer.renderingLayerMask,
//                         castShadows = renderer.shadowCastingMode,
//                         receiveShadows = renderer.receiveShadows
//                     });
//
//                     _entityManager.SetComponentData(entity, new NonUniformScale() { Value = modelRoot.transform.localScale });
//                     _entityManager.SetComponentData(entity, new Translation() {Value = renderer.transform.position});
//                     _entityManager.SetComponentData(entity, new Rotation() {Value = renderer.transform.rotation});
//                     Debug.Log("Creating DOTS entity (child): Position: " + renderer.transform.position + " rotation: " + renderer.transform.rotation);
//                     _entityManager.SetComponentData(entity,
//                         new BuiltinMaterialPropertyUnity_WorldTransformParams{Value = new float4(1,1,1,1)});
//                     _entityManager.SetComponentData(entity,
//                         new BuiltinMaterialPropertyUnity_LightData{Value = new float4(1,1,1,1)});
//                     _entityManager.SetComponentData(entity,
//                         new BuiltinMaterialPropertyUnity_RenderingLayer{Value = new uint4(1,1,1,1)});
//
//                     var bounds = renderer.bounds;
//                     //bounds
//
//                     _entityManager.SetName(entity, model.Name + "_" + filter.gameObject.name);
//                     _modelEntity = entity;
//                     var rendererBounds = new RenderBounds();
//                     rendererBounds.Value.Center = bounds.center;
//                     //rendererBounds.Value.Extents = bounds.extents;
//                     // it was found that extends need to be set manually to 100,100,100 to avoid strange drawing cutoffs when camera is near the object.
//                     rendererBounds.Value.Extents = new float3(100, 100, 100);
//                     _entityManager.SetComponentData(entity, rendererBounds);
//                     _entityManager.SetEnabled(entity, true);
//                 }
//                 model = null;
//             }
//             catch(Exception exc)
//             {
//                 Debug.LogError(exc);
//             }
//             finally
//             {
//                 Destroy(_modelObject);
//                 _modelObject = null;
//                 entityArray.Dispose();
//             }
//
//             if (controller != null)
//             {
//                 // cancel controller.
//                 controller.DestroyMyself();
//                 controller = null;
//             }
//
//             Destroy(modelRoot);
//             isOccupied = false;
//         }
//         public void Clear(bool removeErrorText = true)
//         {
//             if (_modelEntity != Entity.Null)
//             {
//                 _entityManager.SetEnabled(_modelEntity, false);
//             }
//
//             if (entities != null)
//             {
//                 foreach (var entity in entities)
//                 {
//                     _entityManager.SetEnabled(entity,false);
//                 }
//             }
//
//             //_blobAssetStore?.Dispose();
//             this.descriptionPanel.gameObject.SetActive(false);
//             descriptionPanel.ClearData();
//             this.exhibitView.DisablePreview();
//             if (removeErrorText)
//             {
//                 this.exhibitView.SetLoadModelState("");
//             }
//
//             if (model != null)
//             {
//                 model.OnStateUpdate.RemoveListener(StateUpdate);
//                 model.OnPreviewDownloaded.RemoveListener(SetPreviewImage);
//                 model.OnObjectDownloaded.RemoveListener(LoadModel);
//                 model = null;
//             }
//
//             if (_modelObject != null)
//             {
//                 Destroy(_modelObject);
//                 _modelObject = null;
//             }
//
//             if (controller != null)
//             {
//                 // cancel controller.
//                 controller.DestroyMyself();
//                 controller = null;
//             }
//
//             isOccupied = false;
//         }
//         private void OnDestroy()
//         {
//             Clear(false);
//             isOccupied = false;
//         }
//     }
//
// }