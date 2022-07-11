using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Download.Core.Editor;
using DataModels;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public class LocalTestDownload : MonoBehaviour
{
    [SerializeField] private Transform notDotsSpawnPosition;
    [SerializeField] private Transform oldDotsSpawnPosition;
    [SerializeField] private Transform newDotsSpawnPosition;
    
    private EntityManager _entityManager;
    private BlobAssetStore _blobAssetStore;
    private GameObjectConversionSettings _settings;
    private GameObject _modelObject;
    
    private string _state;
    
    private void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _blobAssetStore = new BlobAssetStore();

        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitForSeconds(20);
        UpdateJson("default");
        yield return new WaitForSeconds(10);
        UpdateJson("old");
        yield return new WaitForSeconds(10);
        UpdateJson("new");
    }
    

    public void UpdateJson(string state)
    {
        // if (_modelObject)
        //     Destroy(_modelObject);
        
        _state = state;
        var localJson = Resources.Load<TextAsset>("Collections holder").text;
        CollectionsHolder collectionsHolder = JsonConvert.DeserializeObject<CollectionsHolder>(localJson);
        var model = collectionsHolder.Collections[0].ArtWorks[1];
        var cashedData = Resources.Load<TextAsset>(model.Urls.CashedGlbUrl);
        print("Cashed url: " + model.Urls.CashedGlbUrl);
        model.CashedData = cashedData;
        print("Cashed data: " + model.CashedData);
        
        LoadModel(model);
    }
    private static string GetPathToFileOfExtension(FileInfo[] filesInfo, string extension, string directoriesName,bool fileWithoutExtension = true)
    {
        foreach (var file in filesInfo)
            if (file.Extension == extension)
                if(fileWithoutExtension)
                    return Path.Combine("DefaultCollection", directoriesName, Path.GetFileNameWithoutExtension(file.Name));
                else
                    return Path.Combine("DefaultCollection", directoriesName, file.Name);
        return string.Empty;
    }

    private void LoadModel(GLBData model)
    {
        print(model.CashedData);
        GLBLoader.LoadTiltBrush(new MemoryStream(model.CashedData.bytes), ProcessLoadModel, null);

    }

    private void ProcessLoadModel(GameObject receivedModel)
    {
        _modelObject = receivedModel;
        if (_state == "new")
        {
            receivedModel.transform.position = newDotsSpawnPosition.position;
            CreateDotsEntity(receivedModel);
        }
        else if (_state == "old")
        {
            receivedModel.transform.position = oldDotsSpawnPosition.position;
            _settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, _blobAssetStore);
            GameObjectConversionUtility.ConvertGameObjectHierarchy(receivedModel, _settings);
            Destroy(receivedModel);
        }
        else
        {
            receivedModel.transform.position = notDotsSpawnPosition.position;
        }
    }

    private void CreateDotsEntity(GameObject modelRoot)
    {
        modelRoot.SetActive(true);
        EntityArchetype childEntityArchetype = _entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(NonUniformScale),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(BuiltinMaterialPropertyUnity_LightData),
            typeof(BuiltinMaterialPropertyUnity_WorldTransformParams),
            typeof(BuiltinMaterialPropertyUnity_RenderingLayer));

        MeshFilter[] meshFilters = modelRoot.GetComponentsInChildren<MeshFilter>();
        NativeArray<Entity> entityArray = new NativeArray<Entity>(meshFilters.Length, Allocator.Temp);

            // create entities
            _entityManager.CreateEntity(childEntityArchetype, entityArray);
            _entityManager.AddComponent<WorldRenderBounds>(entityArray);

            Debug.Log("Creating DOTS entity: Local scale: " + modelRoot.transform.localScale);

            for (int i = 0; i < meshFilters.Length; i++)
            {
                var filter = meshFilters[i];
                Entity entity = entityArray[i];
                var renderer = filter.gameObject.GetComponent<Renderer>();
                if (renderer == null)
                {
                    throw new Exception("The mesh renderer was not found.");
                }

                _entityManager.SetSharedComponentData(entity, new RenderMesh
                {
                    mesh = filter.sharedMesh,
                    material = renderer.sharedMaterial,
                    layerMask = renderer.renderingLayerMask,
                    castShadows = renderer.shadowCastingMode,
                    receiveShadows = renderer.receiveShadows
                });

                _entityManager.SetComponentData(entity,
                    new NonUniformScale() { Value = modelRoot.transform.localScale });
                _entityManager.SetComponentData(entity, new Translation() { Value = renderer.transform.position });
                _entityManager.SetComponentData(entity, new Rotation() { Value = renderer.transform.rotation });
                Debug.Log("Creating DOTS entity (child): Position: " + renderer.transform.position + " rotation: " +
                          renderer.transform.rotation);
                _entityManager.SetComponentData(entity,
                    new BuiltinMaterialPropertyUnity_WorldTransformParams { Value = new float4(1, 1, 1, 1) });
                _entityManager.SetComponentData(entity,
                    new BuiltinMaterialPropertyUnity_LightData { Value = new float4(1, 1, 1, 1) });
                _entityManager.SetComponentData(entity,
                    new BuiltinMaterialPropertyUnity_RenderingLayer { Value = new uint4(1, 1, 1, 1) });

                var bounds = renderer.bounds;
                //bounds
                
                var rendererBounds = new RenderBounds();
                rendererBounds.Value.Center = bounds.center;
                //rendererBounds.Value.Extents = bounds.extents;
                // it was found that extends need to be set manually to 100,100,100 to avoid strange drawing cutoffs when camera is near the object.
                rendererBounds.Value.Extents = new float3(100, 100, 100);
                _entityManager.SetComponentData(entity, rendererBounds);
                _entityManager.SetEnabled(entity, true);
            }

            Destroy(modelRoot);
    }
}
