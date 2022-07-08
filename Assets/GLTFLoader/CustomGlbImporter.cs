#if UNITY_EDITOR
using DefaultNamespace.GLTFLoader;
using UnityEditor.AssetImporters;
using UnityEngine;

//Master
namespace DefaultNamespace
{
    [ScriptedImporter(1, "glb")]
    public class CustomGlbImporter : ScriptedImporter
    {

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var obj = GltfImportManager.GetGltfObject(ctx.assetPath);
            Debug.Log($"Imported file generator is {obj.asset.generator}");
        }
    }
}
#endif
