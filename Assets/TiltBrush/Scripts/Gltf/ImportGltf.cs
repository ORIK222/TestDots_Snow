// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Semantic = TiltBrushToolkit.BrushDescriptor.Semantic;

namespace TiltBrushToolkit
{

    public class Null
    {
        protected Null() { }
    }

    /// A callback that allows users to know when the import process has processed
    /// a gltf material into a Unity material.
    public interface IImportMaterialCollector
    {
        // Pass:
        //   unityMaterial - the template material, and the actual material used
        //     (which may be identical to the template material)
        //   gltfMaterial - the gltf node from which unityMaterial was looked up or created
        //
        // It's acceptable to call this redundantly.
        void Add(GltfMaterialConverter.UnityMaterial unityMaterial,
                 GltfMaterialBase gltfMaterial);
    }

    public class ImportGltf
    {
        /// <summary>
        /// Minimum sane bounding box size to perform automatic fitting to a given size.
        /// If the biggest side of the bounding box is smaller than this, we won't attempt to fit it to
        /// a given size.
        /// </summary>
        private const float MIN_BOUNDING_BOX_SIZE_FOR_SIZE_FIT = 0.001f;

        /// <summary>
        /// Minimum valid magnitude for a normal vector that's considered valid. Technically, all normals
        /// should have magnitude close to 1, but we tolerate other magnitudes. Once we go below this
        /// threshold, however, we consider the normal to be invalid and regenerate it on import.
        /// </summary>
        private const float MIN_VALID_NORMAL_SQRMAGNITUDE = float.Epsilon;  // Only fix normals that are exactly the 0 vector.

        // Attributes intended only for the Tilt Brush Toolkit are prefixed with this, eg
        // "_TB_UNITY_TEXCOORD_0". This distinguishes them a true TEXCOORD_0 that matches gltf's semantics
        private const string kTookitAttributePrefix = "_TB_UNITY_";

        private static readonly JsonSerializer kSerializer = new JsonSerializer
        {
            ContractResolver = new GltfJsonContractResolver()
        };

        const int kUnityMeshMaxVerts = 65534;
        const float kExtremeSizeMeters = 371f;

        static Dictionary<GltfImportResult, Mesh.MeshDataArray?> registeredNative = new Dictionary<GltfImportResult, Mesh.MeshDataArray?>();

        public class GltfImportResult
        {
            public GameObject root;
            public List<Mesh> meshes = new List<Mesh>();
            public List<Material> materials = new List<Material>();
            public List<Texture2D> textures = new List<Texture2D>();
            public IImportMaterialCollector materialCollector = null;
        }

        /// <summary>State data used by the import process.</summary>
        public sealed class ImportState : IDisposable
        {
            // Change-of-basis matrix
            internal readonly Matrix4x4 gltfFromUnity;
            // Change-of-basis matrix
            internal readonly Matrix4x4 unityFromGltf;
            // The parsed gltf; filled in by BeginImport
            internal GltfRootBase root;
            // Which gltf scene to import; filled in by BeginImport
            internal GltfSceneBase desiredScene;
            // Scale factor by which to scale the geometry.
            // Computed by BeginImport().
            internal float scaleFactor = 1.0f;
            // Scale factor by which to scale the top node.
            // Computed by BeginImport().
            internal float nodeScaleFactor = 1.0f;
            // Translation to apply to positions in the geometry.
            // This happens AFTER the scale.
            // Computed by BeginImport().
            internal Vector3 translation = Vector3.zero;

            internal ImportState(AxisConvention axes)
            {
                gltfFromUnity = AxisConvention.GetFromUnity(axes);
                unityFromGltf = AxisConvention.GetToUnity(axes);
            }

            public void Dispose()
            {
                if (root != null) { root.Dispose(); }
            }
        }

        public class GltfFileInfo : IDisposable
        {
            public GltfSchemaVersion Version { get; private set; }
            public bool IsGlb { get; private set; }
            public TextReader Reader { get; private set; }

            public GltfFileInfo(Stream stream)
            {
                var encoding = GlbParser.GetFileEncoding(stream);
                if (GlbParser.GetGlbVersion(stream, encoding) is uint glbFormatVersion)
                {
                    IsGlb = true;
                    Version = (glbFormatVersion == 1 ? GltfSchemaVersion.GLTF1 : GltfSchemaVersion.GLTF2);
                    Reader = new StringReader(GlbParser.GetJsonChunkAsString(stream, encoding));
                    Debug.Log($"It is glb, {Version}");
                }
                else
                {
                    IsGlb = false;
                    byte[] buffer = new byte[stream.Length];
                    stream.Position = 0;
                    stream.Read(buffer, 0, buffer.Length);
                    string json = encoding.GetString(buffer);
                    var gltf1Or2 = JsonConvert.DeserializeObject<Gltf1Or2>(json);
                    var versionString = gltf1Or2?.asset?.version ?? "2";  // default to 2, I guess?
                    Version = versionString.StartsWith("1") ? GltfSchemaVersion.GLTF1 : GltfSchemaVersion.GLTF2;
                    Debug.Log($"It is gltf, {Version}");
                    Reader = new StringReader(json);
                }
            }

            public void Dispose()
            {
                if (Reader != null)
                {
                    Reader.Dispose();
                }
            }
        }

        // Used only by GetVersionAndReader. The layout of a gltf file changes significantly
        // between gltf v1 and v2, but thankfully the location of the version info didn't change.
#pragma warning disable 0649
        [Serializable]
        private class Gltf1Or2
        {
            public GltfAsset asset;
        }
#pragma warning restore 0649

        /// <summary>
        /// Import a gltf model.
        /// If you would like to perform some of the work off the main thread, use
        /// the referenced functions instead.
        /// </summary>
        /// <param name="gltfVersion">The glTF format version to use.</param>
        /// <param name="gltfStream">A stream containing the contents of the .gltf file. Ownership of the stream is transferred; it will be closed after the import.</param>
        /// <param name="uriLoader">For fetching relative URIs referenced by the .gltf file.</param>
        /// <param name="materialCollector">May be null if you don't need it.</param>
        /// <param name="options">The options to import the model with.</param>
        /// <seealso cref="ImportGltf.BeginImport" />
        /// <seealso cref="ImportGltf.EndImport" />
        public static AsyncController Import(
          string gltfOrGlbPath, string name, IUriLoader uriLoader,
          IImportMaterialCollector materialCollector, Action<AsyncController> onLoaded, Action<Exception> onFailed,
          GltfImportOptions options)
        {
            var stream = File.OpenRead(gltfOrGlbPath);
            try
            {
                return Import(stream, name, uriLoader, materialCollector, onLoaded, onFailed, options);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Import a gltf model.
        /// If you would like to perform some of the work off the main thread, use
        /// the referenced functions instead.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="name">The name.</param>
        /// <param name="uriLoader">For fetching relative URIs referenced by the .gltf file.</param>
        /// <param name="materialCollector">May be null if you don't need it.</param>
        /// <param name="onLoaded">The on loaded.</param>
        /// <param name="onFailed">The on failed.</param>
        /// <param name="options">The options to import the model with.</param>
        /// <exception cref="System.ArgumentNullException">uriLoader</exception>
        /// <seealso cref="ImportGltf.BeginImport" />
        /// <seealso cref="ImportGltf.EndImport" />
        public static AsyncController Import(
          Stream stream, string name, IUriLoader uriLoader,
          IImportMaterialCollector materialCollector, Action<AsyncController> onLoaded, Action<Exception> onFailed,
          GltfImportOptions options)
        {
            SanityCheckImportOptions(options);
            GameObject controller = new GameObject("controller_" + name);
            AsyncController a = controller.AddComponent<AsyncController>();
            a.Result = new GltfImportResult();
            a.Result.root = new GameObject(name);
            a.Result.root.SetActive(false);
            a.Result.materialCollector = materialCollector;
            a.UriLoader = uriLoader;
            a.Options = options;
            a.Success = onLoaded;
            a.Failure = onFailed;
            a.Stream = stream;
            Task.Run(() => AsyncImport(a, options));
            return a;
        }

        private static GltfRootBase DeserializeGltfRoot(GltfSchemaVersion gltfVersion, JsonTextReader reader)
        {
            switch (gltfVersion)
            {
                case GltfSchemaVersion.GLTF1:
                    {
                        var gltf1Root = kSerializer.Deserialize<Gltf1Root>(reader);
                        if (gltf1Root == null || gltf1Root.nodes == null)
                        {
                            throw new Exception("Failed to parse GLTF1. File is empty or in the wrong format.");
                        }

                        // Some historical Tilt Brush assets use multiple meshes per node, but the importer
                        // assumes single-mesh-per-node.
                        PostProcessRemoveMultipleMeshes(gltf1Root);
                        return gltf1Root;
                    }
                case GltfSchemaVersion.GLTF2:
                    var gltf2Root = kSerializer.Deserialize<Gltf2Root>(reader);
                    if (gltf2Root == null || gltf2Root.nodes == null)
                    {
                        throw new Exception("Failed to parse GLTF2. File is empty or in the wrong format.");
                    }
                    return gltf2Root;
                default:
                    throw new ArgumentException("Invalid gltfVersion" + gltfVersion);
            }
        }

        private static void SanityCheckImportOptions(GltfImportOptions options)
        {
            if (options.rescalingMode == GltfImportOptions.RescalingMode.CONVERT &&
                options.scaleFactor == 0.0f)
            {
                // If scaleFactor is exactly zero (not just close), it's PROBABLY because of user error,
                // because this is what happens when you do "new GltfImportOptions()" and forget to set
                // scaleFactor. GltfImportOptions is a struct so we can't have a default value.
                throw new Exception("scaleFactor must be != 0 for GltfImportOptions CONVERT mode. " +
                    "Did you forget to set scaleFactor?");
            }
            else if (options.rescalingMode == GltfImportOptions.RescalingMode.FIT &&
              options.desiredSize <= 0.0f)
            {
                throw new Exception("desiredSize must be > 0 for GltfImportOptions FIT mode. " +
                    "Did you forget to set desiredSize?");
            }

        }

        private static void CheckCompatibility(
            GltfRootBase root, string versionKey, Version currentVersion)
        {
            var extras = root.asset.extras;
            if (extras == null) { return; }

            string requiredVersion;
            extras.TryGetValue(versionKey, out requiredVersion);
            if (requiredVersion != null)
            {
                Match match = Regex.Match(requiredVersion, @"^([0-9]+)\.([0-9]+)");
                if (match.Success)
                {
                    var required = new Version
                    {
                        major = int.Parse(match.Groups[1].Value),
                        minor = int.Parse(match.Groups[2].Value)
                    };
                    if (required > currentVersion)
                    {
                        Debug.LogWarningFormat(
                            "This file specifies {0} {1}; you are currently using {2}",
                            versionKey, required, currentVersion);
                    }
                }
            }
        }

        /// <summary>
        /// The portion of <seealso cref="ImportGltf.Import" /> that can be
        /// performed off the main thread. The parameters are the same as for Import.
        /// </summary>
        /// <param name="asyncController">The asynchronous controller.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// An object which should be passed to <seealso cref="ImportGltf.EndImport" /> and then disposed
        /// </returns>
        /// <exception cref="System.NullReferenceException">root</exception>
        private static void AsyncImport(AsyncController asyncController, GltfImportOptions options)
        {
            try
            {
                asyncController.State = new ImportState(options.axisConventionOverride ?? AxisConvention.kGltf2);
                using (var info = new GltfFileInfo(asyncController.Stream))
                using (var reader = new JsonTextReader(info.Reader))
                {
                    var root = DeserializeGltfRoot(info.Version, reader);
                    if (root == null)
                    {
                        throw new NullReferenceException("root");
                    }

                    root.Dereference(info.IsGlb, asyncController.UriLoader);

                    // Convert attribute names to the ones we expect.
                    // This may eg overwrite TEXCOORD_0 with _TB_UNITY_TEXCOORD_0 (which will contain more data)
                    foreach (var mesh in root.Meshes)
                    {
                        foreach (var prim in mesh.Primitives)
                        {
                            string[] attrs = prim.GetAttributeNames()
                                .Where(n => n.StartsWith(kTookitAttributePrefix)).ToArray();
                            foreach (var tbAttr in attrs)
                            {
                                string renamed = tbAttr.Substring(kTookitAttributePrefix.Length);
                                prim.ReplaceAttribute(tbAttr, renamed);
                            }
                        }
                    }

                    // Extract Google-specific information.
                    if (root.asset != null)
                    {
                        // Get the generator information so we can tell which app and version generated this file.
                        if (root.asset.generator != null)
                        {
                            Version version;
                            if (GetTiltBrushVersion(root.asset.generator, out version))
                            {
                                root.tiltBrushVersion = version;
                            }
                            else if (GetBlocksVersion(root.asset.generator, out version))
                            {
                                root.blocksVersion = version;
                            }
                        }

                        // Allow the glTF to define the limit of our data forward-compatibility
                        CheckCompatibility(root, "requiredTiltBrushToolkitVersion", TbtSettings.TbtVersion);
                    }

                    asyncController.State.root = root;
                    asyncController.State.desiredScene = root.ScenePtr;
                }

                Bounds? sceneBoundsInGltfSpace;
                sceneBoundsInGltfSpace = ComputeSceneBoundingBoxInGltfSpace(asyncController.State.root.ScenePtr, approximate: false);
                if (sceneBoundsInGltfSpace != null)
                {
                    // Figure out what scale factor to apply.
                    ComputeScaleFactor(asyncController.Options, sceneBoundsInGltfSpace.Value,
                        out asyncController.State.scaleFactor, out asyncController.State.nodeScaleFactor);
                    if (asyncController.Options.recenter)
                    {
                        // The bounding box is calculated in glTF space, so first calculate the translation
                        // in glTF space.
                        var translationInGltfSpace = -sceneBoundsInGltfSpace.Value.center * asyncController.State.scaleFactor;
                        // Now convert the translation from glTF space to Unity space.
                        asyncController.State.translation = asyncController.State.unityFromGltf * translationInGltfSpace;
                    }
                }

                if (asyncController.UriLoader != null && asyncController.UriLoader.CanLoadImages())
                {
                    foreach (var image in GltfMaterialConverter
                      .NecessaryTextures(asyncController.State.root)
                      .Select(tex => tex.SourcePtr)
                      .Where(image => image != null))
                    {
                        // Absolute URIs should now never get through; they will only confuse the uri loader
                        if (image.uri.Contains("://"))
                        {
                            Debug.LogWarningFormat("Incorrectly considered image {0} as necessary", image.uri);
                            continue;
                        }

                        image.data = asyncController.UriLoader.LoadAsImage(image.uri);
                    }
                }

                asyncController.ExecuteCoroutine(FinishImport);
            }
            catch (Exception exc)
            {
                if (asyncController != null && asyncController.UriLoader is IDisposable)
                {
                    ((IDisposable)asyncController.UriLoader).Dispose();
                    asyncController.UriLoader = null;
                }

                asyncController.ProcessingFailure(exc);
            }
        }

        private static IEnumerator FinishImport(AsyncController asyncController)
        {
            var root = asyncController.Result.root;
            CreateMeshPrecursorsFromScene(asyncController);
            yield return null;
            var meshCreator = CreateGameObjectsFromNodes(asyncController);
            foreach (var unused in meshCreator)
            {
                // create meshes!
                yield return null;
            }

            if (asyncController != null && asyncController.Result != null)
            {
                asyncController.Finish();
            }
            else
            {
                // cancelled.
                GameObject.Destroy(root);
            }
        }

        // Returns a factor to apply directly to geometry, and a factor to apply to the top node.
        // The latter will be non-unit only in extreme circumstances, when we detect that
        // the dimensions of the geometry are so extreme that we need to artificially scale
        // the object's geometry to keep within Unity's modelview scale limits, and as a
        // result have to compensate for the scale at the top node.
        static void ComputeScaleFactor(
            GltfImportOptions options, Bounds boundsInGltfSpace,
            out float directScaleFactor, out float nodeScaleFactor)
        {
            switch (options.rescalingMode)
            {
                case GltfImportOptions.RescalingMode.CONVERT:
                    directScaleFactor = options.scaleFactor;
                    nodeScaleFactor = 1;
                    float requiredShrink;
                    if (ComputeScaleFactorToFit(boundsInGltfSpace, kExtremeSizeMeters, out requiredShrink))
                    {
                        if (requiredShrink < 1)
                        {
                            // Object is quite large. Push some extra shrink into the vert data, and compensate
                            // by adding some grow to the node. The assumption is that the user will likely try
                            // to shrink the object; the extra grow on the node helps keep the modelview scale
                            // from getting smaller than Unity's limit of ~6e-5.
                            directScaleFactor *= requiredShrink;
                            nodeScaleFactor /= requiredShrink;
                        }
                        else
                        {
                            // Object is smaller than the extreme limit -- no need for any worries.
                        }
                    }
                    return;
                case GltfImportOptions.RescalingMode.FIT:
                    // User wants a specific size, so derive it from the bounding box.
                    if (!ComputeScaleFactorToFit(boundsInGltfSpace, options.desiredSize, out directScaleFactor))
                    {
                        Debug.LogWarningFormat("Could not automatically resize object; object is too small or empty.");
                        directScaleFactor = nodeScaleFactor = 1;
                        return;
                    }
                    nodeScaleFactor = 1;
                    return;
                default:
                    throw new Exception("Invalid rescaling mode: " + options.rescalingMode);
            }
        }

        static bool GetTiltBrushVersion(string generatorString, out Version version)
        {
            Match match = Regex.Match(generatorString, @"^Tilt Brush ([0-9]+)\.([0-9]+)");
            if (match.Success)
            {
                version = new Version
                {
                    major = int.Parse(match.Groups[1].Value),
                    minor = int.Parse(match.Groups[2].Value)
                };
                return true;
            }
            version = new Version { major = 0, minor = 0 };
            return false;
        }

        static bool GetBlocksVersion(string generatorString, out Version version)
        {
            // Some Blocks version have a version number, some of them don't, due to a bug.
            // But they all start with "Blocks".
            Match match = Regex.Match(generatorString, @"^Blocks ([0-9]+)\.([0-9]+)");
            if (match.Success)
            {
                version = new Version
                {
                    major = int.Parse(match.Groups[1].Value),
                    minor = int.Parse(match.Groups[2].Value),
                };
                return true;
            }
            if (generatorString.StartsWith("Blocks"))
            {
                // "Blocks" without a version number was generated by old versions of Blocks,
                // and can also be generated by debug builds. For these cases, assume the baseline
                // version of blocks.
                version = new Version { major = 1, minor = 0 };
                return true;
            }
            // Likely it's "glTF 1-to-2 Upgrader for Google Blocks"
            // No other Blocks version info, so assume 1.0
            if (generatorString.Contains("Google Blocks"))
            {
                version = new Version { major = 1, minor = 0 };
                return true;
            }
            version = new Version { major = 0, minor = 0 };
            return false;
        }


        /// Converts gltf data to MeshPrecursors
        /// This is mildly compute-intenstive, but can happen off the main thread
        static void CreateMeshPrecursorsFromScene(AsyncController asyncController)
        {
            ImportState state = asyncController.State;
            foreach (var node in state.desiredScene.Nodes)
            {
                CreateMeshPrecursorsFromNode(state, node, asyncController);
            }
        }

        static void CreateMeshPrecursorsFromNode(ImportState state, GltfNodeBase node, AsyncController asyncController)
        {
            if (node.Mesh != null)
            {
                CreateMeshPrecursorsFromMesh(state, node.Mesh, asyncController);
            }

            foreach (var childNode in node.Children)
            {
                CreateMeshPrecursorsFromNode(state, childNode, asyncController);
            }
        }

        static void CreateMeshPrecursorsFromMesh(ImportState state, GltfMeshBase mesh, AsyncController asyncController)
        {
            foreach (var prim in mesh.Primitives)
            {
                if (prim.precursorMeshes == null)
                {
                    prim.precursorMeshes = CreateMeshPrecursorsFromPrimitive(state, prim, asyncController);
                }
            }
        }

        /// <summary>
        /// Computes the scale factor to make the given bounds fit in a bounding cube whose side
        /// measures <c>desiredSize</c>.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="desiredSize">The desired size (bounding cube).</param>
        /// <param name="scaleFactor">The resulting scaling factor.</param>
        /// <returns>True if the factor was computed successfully (in which case it is returned in
        /// <c>scaleFactor</c>), false if it failed (in which case <c>scaleFactor</c> is undefined).</returns>
        static bool ComputeScaleFactorToFit(Bounds bounds, float desiredSize, out float scaleFactor)
        {
            float biggestSide = Math.Max(bounds.size.x, Math.Max(bounds.size.y, bounds.size.z));
            if (biggestSide < MIN_BOUNDING_BOX_SIZE_FOR_SIZE_FIT)
            {
                scaleFactor = 1.0f;
                return false;
            }
            scaleFactor = desiredSize / biggestSide;
            return true;
        }

        static IEnumerable<Null> CreateGameObjectsFromNodes(AsyncController asyncController)
        {
            ImportState state = asyncController.State;
            GltfImportResult result = asyncController.Result;
            IUriLoader uriLoader = asyncController.UriLoader;
            var loaded = new List<Texture2D>();
            foreach (var unused in GltfMaterialConverter.LoadTexturesCoroutine(
                         state.root, uriLoader, loaded))
            {
                yield return null;
            }
            result.textures = loaded;

            GltfMaterialConverter matConverter = new GltfMaterialConverter();
            var rootTransform = result.root.transform;
            Debug.Assert(rootTransform.childCount == 0);
            foreach (var node in state.desiredScene.Nodes)
            {
                foreach (var unused in
                  CreateGameObjectsFromNode(state, rootTransform, node, result, matConverter, state.translation))
                {
                    yield return null;
                }
            }
            foreach (Transform child in rootTransform)
            {
                // child = MatrixScale(nodeScaleFactor) * child;
                child.localScale *= state.nodeScaleFactor;
                child.localPosition *= state.nodeScaleFactor;
            }
            result.materials = matConverter.GetGeneratedMaterials();
        }

        /// Creates a GameObject tree from node and attaches it to parent.
        /// <param name="translationToApply">Translation to apply to the created node. This translation
        /// is NOT applied recursively to child nodes, it's only applied to the top node.</param>
        static IEnumerable CreateGameObjectsFromNode(
            ImportState state, Transform parent, GltfNodeBase node, GltfImportResult result,
            GltfMaterialConverter matConverter, Vector3 translationToApply)
        {
            if (node.Mesh == null && !node.Children.Any())
            {
                if (node.name != null && node.name.StartsWith("empty_"))
                {
                    // explicitly-empty nodes are used to mark things like non-exportable models
                    /* fall through */
                }
                else
                {
                    // Other empty nodes can creep in, like the SceneLight_ nodes. Don't want those.
                    yield break;
                }
            }

            GameObject obj = new GameObject(node.name);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }

            if (node.matrix != null)
            {
                Matrix4x4 unityMatrix = ChangeBasisAndApplyScale(
                    node.matrix.Value, state.scaleFactor,
                    unityFromGltf: state.unityFromGltf,
                    gltfFromUnity: state.gltfFromUnity);
                Vector3 translation, scale;
                Quaternion rotation;
                MathUtils.DecomposeToTrs(unityMatrix, out translation, out rotation, out scale);
                obj.transform.localPosition = translation;
                obj.transform.localRotation = rotation;
                obj.transform.localScale = scale;
            }
            else
            {
                // Default to identity. (obj.localTransform is already identity)
            }

            // TODO(pld): Maybe better to have caller apply it and simplify the parameter out of the API
            obj.transform.localPosition += translationToApply;

            if (node.Mesh != null)
            {
                foreach (var unused in CreateGameObjectsFromMesh(
                    obj.transform, node.Mesh, result, matConverter, allowMeshInParent: true))
                {
                    yield return null;
                }
            }

            foreach (var childNode in node.Children)
            {
                foreach (var unused in
                         CreateGameObjectsFromNode(state, obj.transform, childNode, result, matConverter, Vector3.zero))
                {
                    yield return null;
                }
            }
        }

        public static void UnregisterResult(GltfImportResult result)
        {
            if (registeredNative.ContainsKey(result))
            {
                var meshDataArray = registeredNative[result];
                if (meshDataArray != null)
                {
                    meshDataArray.Value.Dispose();
                }

                registeredNative.Remove(result);
            }
        }

        static IEnumerable CreateGameObjectsFromMesh(
            Transform parent, GltfMeshBase mesh, GltfImportResult result,
            GltfMaterialConverter matConverter, bool allowMeshInParent)
        {
            // A mesh may have more than one primitive, although in the common case
            // there will be only one. Each needs its own GameObject, because the primitives
            // may have different materials.

            // For more discussion on multiple-mesh vs multiple-primitive,
            // see https://github.com/KhronosGroup/glTF/issues/821
            // and regarding creating an extra node level for primitives and meshes
            // https://github.com/KhronosGroup/glTF/issues/1065
            for (int iP = 0; iP < mesh.PrimitiveCount; ++iP)
            {
                GltfPrimitiveBase prim = mesh.GetPrimitiveAt(iP);
                GltfMaterialConverter.UnityMaterial? unityMaterial =
                    matConverter.GetMaterial(prim.MaterialPtr);
                if (result.materialCollector != null && unityMaterial != null)
                {
                    Debug.Assert(unityMaterial.Value.material != null);
                    // Generate IExportableMaterial for later exports.
                    result.materialCollector.Add(unityMaterial.Value, prim.MaterialPtr);
                }

                if (prim.precursorMeshes == null)
                {
                    continue;
                }

                string primName = mesh.name;
                if (mesh.PrimitiveCount > 1)
                {
                    primName += string.Format("_p{0}", iP);
                }

                if (prim.unityMeshes == null)
                {
                    prim.unityMeshes = new List<Mesh>();
                    var meshDataArray = Mesh.AllocateWritableMeshData(prim.precursorMeshes.Count);
                    registeredNative[result] = meshDataArray;
                    bool disposed = false;
                    try
                    {
                        for (int iM = 0; iM < prim.precursorMeshes.Count; ++iM)
                        {
                            string meshName = primName;
                            if (prim.precursorMeshes.Count > 1)
                            {
                                meshName += string.Format("_m{0}", iM);
                            }

                            Mesh umesh = new Mesh();
                            //UnityFromPrecursorOld(umesh, prim.precursorMeshes[iM]);
                            Bounds bounds = new Bounds();
                            string error = null;
                            Task t = Task.Run(() => UnityFromPrecursorNew(ref bounds, meshDataArray[iM], prim.precursorMeshes[iM], ref error));
                            while (!t.IsCompleted)
                            {
                                if (error != null)
                                {
                                    break;
                                }

                                yield return null;
                            }

                            if (error != null)
                            {
                                Debug.LogError(error);
                            }

                            if (prim.precursorMeshes[iM].normals == null)
                            {
                                umesh.RecalculateNormals();
                                yield return null;
                            }

                            umesh.bounds = bounds;
                            umesh.name = meshName;
                            prim.unityMeshes.Add(umesh);
                            if (result.meshes != null)
                            {
                                result.meshes.Add(umesh);
                            }

                            yield return null;
                        }

                        if (registeredNative.ContainsKey(result) && registeredNative[result] != null)
                        {
                            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, prim.unityMeshes, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
                        }

                        disposed = true;
                    }
                    finally
                    {
                        if (!disposed)
                        {
                            meshDataArray.Dispose();
                        }

                        registeredNative.Remove(result);
                    }

                    yield return null;
                }

                // Unity meshes may not have > 2^16 verts. Having to break the
                // prim up into multiple meshes is unexpected, since Blocks and TB break
                // up their geometry into chunks much smaller than the Unity limit, and since
                // the exporter they use only supports 16-bit indices
                foreach (var unityMesh in prim.unityMeshes)
                {
                    GameObject obj;
                    if (allowMeshInParent && mesh.PrimitiveCount == 1 && prim.unityMeshes.Count == 1)
                    {
                        obj = parent.gameObject;
                    }
                    else
                    {
                        // If more than one, put them all in as children
                        obj = new GameObject(unityMesh.name);
                        obj.transform.SetParent(parent, false);
                    }

                    MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                    if (unityMaterial != null)
                    {
                        renderer.sharedMaterial = unityMaterial.Value.material;
                    }

                    MeshFilter filter = obj.AddComponent<MeshFilter>();
                    filter.sharedMesh = unityMesh;
                }
            }
        }

        //static void UnityFromPrecursorOld(Mesh mesh, MeshPrecursor precursor)
        //{
        //    mesh.vertices = precursor.vertices;
        //    if (precursor.normals != null) { mesh.normals = precursor.normals; }
        //    if (precursor.colors != null) { mesh.colors = precursor.colors; }
        //    if (precursor.colors32 != null) { mesh.colors32 = precursor.colors32; }
        //    if (precursor.tangents != null) { mesh.tangents = precursor.tangents; }
        //    for (int i = 0; i < precursor.uvSets.Length; ++i)
        //    {
        //        if (precursor.uvSets[i] != null)
        //        {
        //            GenericSetUv(mesh, i, precursor.uvSets[i]);
        //        }
        //    }
        //    mesh.triangles = precursor.triangles;

        //    if (precursor.normals == null)
        //    {
        //        mesh.RecalculateNormals();
        //    }
        //}

        static void UnityFromPrecursorNew(ref Bounds meshBounds, Mesh.MeshData meshData, MeshPrecursor precursor, ref string error)
        {
            try
            {
                List<VertexAttributeDescriptor> descriptors = new List<VertexAttributeDescriptor>();
                descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
                int vertexLength = 3;
                int normalsOffset = vertexLength;
                if (precursor.normals != null)
                {
                    vertexLength += 3;
                    descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.Normal));
                }

                int tangentsOffset = vertexLength;
                if (precursor.tangents != null)
                {
                    vertexLength += 4;
                    descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4));
                }

                int colorsOffset = vertexLength;
                if (precursor.colors32 != null || precursor.colors != null)
                {
                    vertexLength += 4;
                    descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4));
                }

                int uvOffset = vertexLength;
                for (int i = 0; i < precursor.uvSets.Length; ++i)
                {
                    if (precursor.uvSets[i] != null)
                    {
                        var array = precursor.uvSets[i];
                        int dimension = 2;
                        vertexLength += 2;
                        if (array is Vector3[])
                        {
                            vertexLength += 1;
                            dimension = 3;
                        }
                        else if (array is Vector4[])
                        {
                            vertexLength += 2;
                            dimension = 4;
                        }

                        if (i == 0)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension));
                        }
                        else if (i == 1)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, dimension));
                        }
                        else if (i == 2)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, dimension));
                        }
                        else if (i == 3)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, dimension));
                        }
                        else if (i == 4)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, dimension));
                        }
                        else if (i == 5)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.Float32, dimension));
                        }
                        else if (i == 6)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, dimension));
                        }
                        else if (i == 7)
                        {
                            descriptors.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord7, VertexAttributeFormat.Float32, dimension));
                        }
                    }
                }

                // set vertices.
                meshData.SetVertexBufferParams(precursor.vertices.Length, descriptors.ToArray());
                var vectorData = meshData.GetVertexData<float>();
                bool first = true;
                Vector3 lowestPoint = Vector3.zero;
                Vector3 highestPoint = Vector3.zero;
                for (int i = 0; i < precursor.vertices.Length; i++)
                {
                    int index = i * vertexLength;
                    var vert = precursor.vertices[i];
                    vectorData[index] = vert.x;
                    vectorData[index + 1] = vert.y;
                    vectorData[index + 2] = vert.z;
                    if (first)
                    {
                        lowestPoint = vert;
                        highestPoint = vert;
                        first = false;
                    }
                    else
                    {
                        lowestPoint = new Vector3(Mathf.Min(lowestPoint.x, vert.x), Mathf.Min(lowestPoint.y, vert.y), Mathf.Min(lowestPoint.z, vert.z));
                        highestPoint = new Vector3(Mathf.Max(highestPoint.x, vert.x), Mathf.Max(highestPoint.y, vert.y), Mathf.Max(highestPoint.z, vert.z));
                    }
                }

                // set triangles.
                meshData.SetIndexBufferParams(precursor.triangles.Length, IndexFormat.UInt32);
                var indices = meshData.GetIndexData<uint>();
                for (int i = 0; i < indices.Length; ++i)
                {
                    indices[i] = (uint)precursor.triangles[i];
                }

                // if there are normals set them.
                if (precursor.normals != null)
                {
                    for (int i = 0; i < precursor.normals.Length; i++)
                    {
                        int index = (i * vertexLength) + normalsOffset;
                        vectorData[index] = precursor.normals[i].x;
                        vectorData[index + 1] = precursor.normals[i].y;
                        vectorData[index + 2] = precursor.normals[i].z;
                    }
                }

                // if there are colors set them too.
                if (precursor.colors != null)
                {
                    for (int i = 0; i < precursor.colors.Length; i++)
                    {
                        int index = (i * vertexLength) + colorsOffset;
                        vectorData[index] = precursor.colors[i].r;
                        vectorData[index + 1] = precursor.colors[i].g;
                        vectorData[index + 2] = precursor.colors[i].b;
                        vectorData[index + 3] = precursor.colors[i].a;
                    }
                }
                else if (precursor.colors32 != null)
                {
                    for (int i = 0; i < precursor.colors32.Length; i++)
                    {
                        int index = (i * vertexLength) + colorsOffset;
                        vectorData[index] = precursor.colors32[i].r / 255.0f;
                        vectorData[index + 1] = precursor.colors32[i].g / 255.0f;
                        vectorData[index + 2] = precursor.colors32[i].b / 255.0f;
                        vectorData[index + 3] = precursor.colors32[i].a / 255.0f;
                    }
                }

                // tangents setup
                if (precursor.tangents != null)
                {
                    for (int i = 0; i < precursor.tangents.Length; i++)
                    {
                        int index = (i * vertexLength) + tangentsOffset;
                        vectorData[index] = precursor.tangents[i].x;
                        vectorData[index + 1] = precursor.tangents[i].y;
                        vectorData[index + 2] = precursor.tangents[i].z;
                        vectorData[index + 3] = precursor.tangents[i].w;
                    }
                }

                // uv setup
                for (int i = 0; i < precursor.uvSets.Length; ++i)
                {
                    if (precursor.uvSets[i] != null)
                    {
                        var array = precursor.uvSets[i];
                        if (array is Vector2[])
                        {
                            var vec2 = (Vector2[])array;
                            for (int j = 0; j < vec2.Length; j++)
                            {
                                int index = (j * vertexLength) + uvOffset;
                                vectorData[index] = vec2[j].x;
                                vectorData[index + 1] = vec2[j].y;
                            }
                            
                            uvOffset += 2;
                        }
                        else if (array is Vector3[])
                        {
                            var vec3 = (Vector3[])array;
                            for (int j = 0; j < vec3.Length; j++)
                            {
                                int index = (j * vertexLength) + uvOffset;
                                vectorData[index] = vec3[j].x;
                                vectorData[index + 1] = vec3[j].y;
                                vectorData[index + 2] = vec3[j].z;
                            }

                            uvOffset += 3;
                        }
                        else if (array is Vector4[])
                        {
                            var vec4 = (Vector4[])array;
                            for (int j = 0; j < vec4.Length; j++)
                            {
                                int index = (j * vertexLength) + uvOffset;
                                vectorData[index] = vec4[j].x;
                                vectorData[index + 1] = vec4[j].y;
                                vectorData[index + 2] = vec4[j].z;
                                vectorData[index + 3] = vec4[j].w;
                            }

                            uvOffset += 4;
                        }
                    }
                }

                meshData.subMeshCount = 1;
                var descriptor = new SubMeshDescriptor(0, precursor.triangles.Length);
                var center = (lowestPoint + highestPoint) / 2;
                descriptor.bounds = new Bounds(center, (highestPoint - lowestPoint));
                meshBounds = new Bounds(center, (highestPoint - lowestPoint));
                meshData.SetSubMesh(0, descriptor, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
            }
            catch(Exception exc)
            {
                error = exc.ToString();
            }
        }

        struct MeshSubset
        {
            public IntRange vertices;
            public IntRange triangles;
        }

        // Breaks a mesh up into contiguous ranges of triangles and vertices such that:
        // - no range of vertices has size greater than maxSubsetVerts.
        // - triangle ranges are disjoint
        // - triangle ranges together cover the entire range of triangles[]
        //
        // This decomposition is only possible if the topology satisfies certain
        // assumptions (commented inline). It will work for Tilt Brush topology,
        // but is definitely not suitable for arbitrary meshes.
        static IEnumerable<MeshSubset> GenerateMeshSubsets(
            ushort[] triangles, int numVerts, int maxSubsetVerts = kUnityMeshMaxVerts)
        {

            // Early out if there's no split -- saves time figuring out which verts are used.
            if (numVerts <= maxSubsetVerts)
            {
                yield return new MeshSubset
                {
                    vertices = new IntRange { min = 0, max = numVerts },
                    triangles = new IntRange { min = 0, max = triangles.Length }
                };
                yield break;
            }

            int count = triangles.Length;

            IntRange? vertsUsed = null;
            IntRange? trisUsed = null;

            for (int iTri = 0; iTri < count; /* manual loop advance */)
            {
                // Assumption #1: triVerts.Size << maxSubsetVerts
                IntRange triVerts;
                {
                    int t0 = triangles[iTri];
                    int t1 = triangles[iTri + 1];
                    int t2 = triangles[iTri + 2];
                    triVerts = new IntRange
                    {
                        min = Mathf.Min(t0, t1, t2),
                        max = Mathf.Max(t0, t1, t2) + 1
                    };
                }

                IntRange newVertsUsed = IntRange.Union(vertsUsed, triVerts);
                // Simplifying assumption #2: indices in triangles[] are mostly monotonic
                IntRange newTrisUsed = IntRange.Union(trisUsed, new IntRange { min = iTri, max = iTri + 3 });

                // If tri doesn't fit, emit a subset and re-attempt the triangle.
                if (newVertsUsed.Size > maxSubsetVerts)
                {
                    if (vertsUsed == null)
                    {
                        Debug.LogWarningFormat("No forward progress");
                        yield break;
                    }
                    yield return new MeshSubset
                    {
                        vertices = vertsUsed.Value,
                        triangles = trisUsed.Value
                    };

                    vertsUsed = null;
                    trisUsed = null;
                    // Did not consume a triangle; do not advance iTri
                }
                else
                {
                    vertsUsed = newVertsUsed;
                    trisUsed = newTrisUsed;
                    iTri += 3;
                }
            }

            if (vertsUsed != null)
            {
                yield return new MeshSubset
                {
                    vertices = vertsUsed.Value,
                    triangles = trisUsed.Value
                };
            }
        }

        // Switches basis from gltf -> Unity.
        // Applies a scale change directly to the transforms + mesh data.
        static Matrix4x4 ChangeBasisAndApplyScale(
            Matrix4x4 gltfMatrix, float scaleFactor,
            Matrix4x4 unityFromGltf, Matrix4x4 gltfFromUnity)
        {
            Matrix4x4 inTargetBasis = unityFromGltf * gltfMatrix * gltfFromUnity;
            // The similarity transform is the canonical way to make arbitrary
            // transforms to a matrix. If you expand out the scale math, all it
            // does it modify the translation -- which makes sense, since the
            // translation is the only distance-like portion of the 4x4.
            // Doing it this way avoids multiplying then dividing the upper-left 3x3
            // by the same factor (which can introduce perturbations)
#if false
    Matrix4x4 destFromSource = Matrix4x4.Scale(Vector3.one * scaleFactor);
    Matrix4x4 sourceFromDest = Matrix4x4.Scale(Vector3.one / scaleFactor);
    return destFromSource * inTargetBasis * sourceFromDest;
#else
            inTargetBasis[12] *= scaleFactor;
            inTargetBasis[13] *= scaleFactor;
            inTargetBasis[14] *= scaleFactor;
#endif
            return inTargetBasis;
        }

        // Switches texture origin from glTF style (upper-left) to Unity style (lower-left)
        static void ChangeUvBasis(Array data, Semantic semantic)
        {
            if (semantic == Semantic.XyIsUv ||
                semantic == Semantic.XyIsUvZIsDistance)
            {
                if (data is Vector2[])
                {
                    Vector2[] vData = (Vector2[])data;
                    for (int i = 0; i < vData.Length; ++i)
                    {
                        var tmp = vData[i]; tmp.y = 1 - tmp.y; vData[i] = tmp;
                    }
                }
                else if (data is Vector3[])
                {
                    Vector3[] vData = (Vector3[])data;
                    for (int i = 0; i < vData.Length; ++i)
                    {
                        var tmp = vData[i]; tmp.y = 1 - tmp.y; vData[i] = tmp;
                    }
                }
                else if (data is Vector4[])
                {
                    Vector4[] vData = (Vector4[])data;
                    for (int i = 0; i < vData.Length; ++i)
                    {
                        var tmp = vData[i]; tmp.y = 1 - tmp.y; vData[i] = tmp;
                    }
                }
            }
        }

        // Switches position basis from gltf -> Unity.
        // Applies a scale change directly to the transforms + mesh data.
        // basisChange is a unityFromGltf matrix
        static void ChangeBasisAndApplyScale(
            Array data, Semantic semantic, float scaleFactor, Matrix4x4 basisChange)
        {
            if (semantic == Semantic.Position ||
                semantic == Semantic.Vector)
            {
                // Assume incoming data is meters
                basisChange *= Matrix4x4.Scale(Vector3.one * scaleFactor);
            }
            else if (semantic == Semantic.UnitlessVector)
            {
                // use basisChange as-is
            }
            else if (semantic == Semantic.XyIsUvZIsDistance)
            {
                // Do unit change
                if (scaleFactor == 1)
                {
                    // Don't bother
                }
                else if (data is Vector3[])
                {
                    Vector3[] vData = (Vector3[])data;
                    for (int i = 0; i < vData.Length; ++i)
                    {
                        var tmp = vData[i]; tmp.z *= scaleFactor; vData[i] = tmp;
                    }
                }
                else if (data is Vector4[])
                {
                    Vector4[] vData = (Vector4[])data;
                    for (int i = 0; i < vData.Length; ++i)
                    {
                        var tmp = vData[i]; tmp.z *= scaleFactor; vData[i] = tmp;
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Cannot change basis of type {0}", data.GetType());
                }
                // no basis-change needed
                return;
            }
            else
            {
                // no basis-change or unit-change needed
                return;
            }

            if (data is Vector3[])
            {
                Vector3[] vData = (Vector3[])data;
                for (int i = 0; i < vData.Length; ++i)
                {
                    vData[i] = basisChange.MultiplyVector(vData[i]);
                }
            }
            else if (data is Vector4[])
            {
                Vector4[] vData = (Vector4[])data;
                for (int i = 0; i < vData.Length; ++i)
                {
                    vData[i] = basisChange.MultiplyVector(vData[i]);
                }
            }
            else
            {
                Debug.LogWarningFormat("Cannot change basis of type {0}", data.GetType());
            }
        }

        // Annotates GltfPrimitive with materials, precursorMeshes
        static List<MeshPrecursor> CreateMeshPrecursorsFromPrimitive(
            ImportState state, GltfPrimitiveBase prim, AsyncController asyncController)
        {
            if (prim.mode != GltfPrimitiveBase.Mode.TRIANGLES)
            {
                Debug.LogWarningFormat("Cannot create mesh from {0}", prim.mode);
                return null;
            }

            BrushDescriptor desc = GltfMaterialConverter.LookupBrushDescriptor(prim.MaterialPtr);
            if (desc != null)
            {
                if (desc.m_bFbxExportNormalAsTexcoord1)
                {
                    // Because of historical compat issues with fbx, some TBT shaders are slightly different
                    // from TB's shaders; they read from TEXCOORD1 instead of NORMAL, and there is a
                    // corresponding data change.
                    // - For fbx we move NORMAL -> TEXCOORD1 at export time.
                    // - For gltf we are moving NORMAL -> TEXCOORD1 at import time.
                    // If we ever fix our TBT shaders to not be different from TB shaders, we can skip this.
                    prim.ReplaceAttribute("NORMAL", "TEXCOORD_1");
                }
            }

            int numVerts = prim.GetAttributePtr("POSITION").count;

            ushort[] triangles;
            {
                GltfAccessorBase accessor = prim.IndicesPtr;
                IntRange range = new IntRange { max = accessor.count };
                Array genericTriangles = GetDataAsArray(accessor, range, null);
                // I believe Unity doesn't allow index to be 0xffff
                if (genericTriangles is UInt32[] && numVerts <= 0xfffe)
                {
                    // If Unity has API support _and_ the hardware has runtime support
                    // for 32-bit indices, maybe we can avoid this.
                    UInt32[] typedTriangles = (UInt32[])genericTriangles;
                    triangles = new ushort[typedTriangles.Length];
                    unchecked
                    {
                        for (int i = 0; i < typedTriangles.Length; ++i)
                        {
                            triangles[i] = (ushort)typedTriangles[i];
                        }
                    }
                }
                else if (genericTriangles is ushort[])
                {
                    triangles = (ushort[])genericTriangles;
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format("Unsupported: {0} indices", genericTriangles.GetType()));
                }
            }

            List<MeshPrecursor> meshes = new List<MeshPrecursor>();

            // Tilt Brush particle meshes are sensitive to being broken up.
            // The verts may shift, but the shader requires that (vertexId % 4) not change.
            int maxSubsetVerts = kUnityMeshMaxVerts - 4;
            foreach (var readonlySubset in GenerateMeshSubsets(triangles, numVerts, maxSubsetVerts))
            {
                var mesh = new MeshPrecursor();

                // Protect the above invariant. (The copy here is required by C#)
                var subset = readonlySubset;
                subset.vertices.min -= (subset.vertices.min % 4);

                // Vertex data
                HashSet<string> attributeNames = prim.GetAttributeNames();
                foreach (var semantic in attributeNames)
                {
                    GltfAccessorBase accessor = prim.GetAttributePtr(semantic);
                    IntRange attribRange = subset.vertices;

                    // Note: some GLTF files incorrectly[?] have accessors with shorter counts than the vertex
                    // count. In these cases, be lenient and ignore the missing data.
                    if (attribRange.min >= accessor.count)
                    {
                        // No data at all for this accessor (entirely out of range).
                        Debug.LogWarningFormat("Attribute {0} has no data: wanted range {1}, count was {2}",
                            semantic, attribRange, accessor.count);
                        // Ignore this attribute.
                        continue;
                    }
                    else if (attribRange.max > accessor.count)
                    {
                        // Attribute is present, but is missing some data.
                        // This is a data error, so print a warning.
                        Debug.LogWarningFormat("Attribute {0} is missing data: wanted range {1}, count was {2}",
                            semantic, attribRange, accessor.count);
                        // Cap the read range to what's available.
                        attribRange.max = accessor.count;
                    }
                    Array data;
                    if (!TryGetDataAsArray(accessor, attribRange, semantic, out data))
                    {
                        // Unrecognized semantic or format. Skip. Warning was already printed.
                        continue;
                    }
                    // PadArrayToSize() guarantees that, no matter what, the data array will have enough data to cover
                    // all the vertices, even if (due to the problems described above) the accessor is missing
                    // some elements. The missing elements will be initialized to zero.
                    data = PadArrayToSize(data, subset.vertices.Size);
                    StoreDataInMesh(state, prim.MaterialPtr, semantic, data, mesh);
                }

                {
                    int[] triangleSubset = new int[subset.triangles.Size];
                    int triangleSubsetLength = triangleSubset.Length;
                    Debug.Assert(triangleSubsetLength % 3 == 0);
                    // Re-index verts. Also reverse winding since the basis-change matrix has a mirroring.
                    // Also change from ushort -> int
                    for (int i = 0; i < triangleSubsetLength; i += 3)
                    {
                        triangleSubset[i] = triangles[subset.triangles.min + i] - subset.vertices.min;
                        triangleSubset[i + 1] = triangles[subset.triangles.min + i + 2] - subset.vertices.min;
                        triangleSubset[i + 2] = triangles[subset.triangles.min + i + 1] - subset.vertices.min;
                    }
                    mesh.triangles = triangleSubset;
                }

                meshes.Add(mesh);
            }

            // This was added in PT to address bad data in Poly, but it can force the data
            // to be unit-length; don't do it unless we know that's a valid fix.
            if (GetNormalSemantic(state, prim.MaterialPtr) == Semantic.UnitlessVector)
            {
                foreach (MeshPrecursor mesh in meshes)
                {
                    FixInvalidNormals(mesh);
                }
            }
            return meshes;
        }

        // Takes raw data from glTF (data), then:
        // - Looks up precise semantics from semantic and desc.uvNSemantic
        // - Uses semantic to make basis, unit, scale transformations
        // - Uses semantic to determine where to store the data
        private static void StoreDataInMesh(
            ImportState state,
            GltfMaterialBase material, string semantic, Array data,
            MeshPrecursor mesh)
        {
            int txcChannel;
            switch (semantic)
            {
                case "POSITION":
                    ChangeBasisAndApplyScale(data, Semantic.Position, state.scaleFactor, state.unityFromGltf);
                    mesh.vertices = (Vector3[])data;
                    break;
                case "NORMAL":
                    ChangeBasisAndApplyScale(
                        data, GetNormalSemantic(state, material), state.scaleFactor, state.unityFromGltf);
                    mesh.normals = (Vector3[])data;
                    break;
                case "COLOR":
                case "COLOR_0":
                    {
                        if (!(data is Color[] || data is Color32[]))
                        {
                            Debug.LogWarning($"Unsupported: color buffer of type {data?.GetType()}");
                            break;
                        }

                        var desiredSpace = GetDesiredColorSpace(state.root);
                        var actualSpace = GetActualColorSpace(state.root);
                        if (actualSpace == ColorSpace.Unknown)
                        {
                            Debug.LogWarning("Reading color buffer in unknown color space");
                            // Guess at something, so we at least offer consistent results.
                            // sRGB is the most likely.
                            actualSpace = ColorSpace.Srgb;
                        }

                        if (desiredSpace != actualSpace)
                        {
                            Color[] colors = data as Color[];
                            if (colors == null)
                            {
                                // Explicitly widen so we get more precision in the colorspace conversion
                                data = colors = ((Color32[])data).Select(c32 => (Color)c32).ToArray();
                            }
                            if (desiredSpace == ColorSpace.Srgb && actualSpace == ColorSpace.Linear)
                            {
                                for (int i = 0; i < colors.Length; ++i)
                                {
                                    colors[i] = colors[i].gamma;
                                }
                            }
                            else if (desiredSpace == ColorSpace.Linear && actualSpace == ColorSpace.Srgb)
                            {
                                for (int i = 0; i < colors.Length; ++i)
                                {
                                    colors[i] = colors[i].linear;
                                }
                            }
                        }

                        if (data is Color32[] colors32)
                        {
                            mesh.colors32 = colors32;
                        }
                        else
                        {
                            mesh.colors = (Color[])data;
                        }
                        break;
                    }
                case "TANGENT":
                    ChangeBasisAndApplyScale(
                        data, Semantic.UnitlessVector, state.scaleFactor, state.unityFromGltf);
                    mesh.tangents = (Vector4[])data;
                    break;
                case "TEXCOORD_0":
                    txcChannel = 0;
                    goto GenericTexcoord;
                case "TEXCOORD_1":
                    txcChannel = 1;
                    goto GenericTexcoord;
                case "TEXCOORD_2":
                    txcChannel = 2;
                    goto GenericTexcoord;
                case "TEXCOORD_3":
                    txcChannel = 3;
                    goto GenericTexcoord;
                GenericTexcoord:
                    var ptSemantic = GetTexcoordSemantic(state, material, txcChannel);
                    ChangeBasisAndApplyScale(data, ptSemantic, state.scaleFactor, state.unityFromGltf);
                    ChangeUvBasis(data, ptSemantic);
                    mesh.uvSets[txcChannel] = data;
                    break;
                case "VERTEXID":
                    // This was an attempt to get vertex id in webgl, but it didn't work out.
                    // The data is not fully hooked-up in the gltf, and it doesn't make its way to THREE.
                    // So: ignore it.
                    break;
                case "_TB_TIMESTAMP":
                    // Tilt Brush .glb files don't ever have txc2, so this is a safe place to stuff timestamps
                    mesh.uvSets[2] = data;
                    break;
                default:
                    Debug.LogWarningFormat("Unhandled attribute {0}", semantic);
                    break;
            }
        }

        /// <summary>
        /// Pads the given array with the array type's default value so that it's at least
        /// the given size.
        /// </summary>
        /// <param name="array">The array to pad.</param>
        /// <param name="size">The desired size</param>
        private static Array PadArrayToSize(Array array, int size)
        {
            if (array.Length >= size) return array;
            Array paddedArray = Array.CreateInstance(array.GetType().GetElementType(), size);
            Array.Copy(array, paddedArray, array.Length);
            return paddedArray;
        }

        /// <summary>
        /// Detects and fixes any invalid normals in the given mesh.
        /// Also handles the case where the normals buffer is not the correct length.
        /// </summary>
        private static void FixInvalidNormals(MeshPrecursor mesh)
        {
            if (mesh.normals == null)
            {
                // No normals! We have to generate all of them -- but let Unity do it in C++
                return;
            }
            else if (mesh.normals.Length < mesh.vertices.Length)
            {
                // Weird case where we don't have normals for all vertices.
                // Leave null so they get regenerated later in C++
                Debug.LogWarning("Unexpected: not enough data in mesh.normals. Regenerating.");
                mesh.normals = null;
                return;
            }

            List<int> invalidNormalIndices = new List<int>();
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                if (mesh.normals[i].sqrMagnitude < MIN_VALID_NORMAL_SQRMAGNITUDE)
                {
                    invalidNormalIndices.Add(i);
                }
            }

            // If we didn't find any invalid normals, stop here. No more work needed.
            if (invalidNormalIndices.Count == 0) return;

            // For each vertex index i, vertexToTriangle[i] is the index in mesh.triangles
            // where one (arbitrarily chosen, if there's more than one) triangle that contains
            // the vertex is located.
            Dictionary<int, int> vertexToTriangle = new Dictionary<int, int>();

            // Build the map from vertex to containing triangles.
            for (int i = 0; i < mesh.triangles.Length; i++)
            {
                // Every triplet is a triangle so if we're at index i, the triangle starts at index i - i % 3.
                vertexToTriangle[mesh.triangles[i]] = i - i % 3;
            }

            for (int j = 0; j < invalidNormalIndices.Count; j++)
            {
                int i = invalidNormalIndices[j];
                // Replace this normal by an automatically computed triangle normal.
                // It might not be right, but it's better than an invalid one.
                int triStart;
                if (!vertexToTriangle.TryGetValue(i, out triStart))
                {
                    // This vertex doesn't belong to any triangles, which is weird, but in this case
                    // we don't have to fix the normal since the vertex doesn't render.
                    continue;
                }
                // The triangle to which the vertex belongs is given by the indices triStart, triStart + 1
                // and triStart + 2 in mesh.triangles. So we just have to get the corresponding vertices
                // and calculate the normal:
                mesh.normals[i] = MathUtils.CalculateNormal(
                  mesh.vertices[mesh.triangles[triStart]],
                  mesh.vertices[mesh.triangles[triStart + 1]],
                  mesh.vertices[mesh.triangles[triStart + 2]]);
            }
        }

        // Within TB, TB brush materials will return Semantic.Unspecified or Semantic.Position.
        // This method is useful for skipping overzealous code that wants to "fix" TB's normals.
        static Semantic GetNormalSemantic(ImportState state, GltfMaterialBase material)
        {
#if TILT_BRUSH
    // "normalSemantic" doesn't exist in the stripped-down BrushDescriptor used by TBT.
    // Only particles use a non-standard semantic for Normals.
    // TBT puts vert.normal -> vert.txc1 at import time (see CreateMeshPrecursorsFromPrimitive).
    // So BD.normalSemantic in TB becomes BD.txc1Semantic in TBT.
    if (state.root.tiltBrushVersion != null) {
      BrushDescriptor desc = GltfMaterialConverter.LookupBrushDescriptor(material);
      if (desc != null) {
        return desc.VertexLayout.normalSemantic;
      }
    }
#endif
            return Semantic.UnitlessVector;
        }

        // Returns a Semantic which tells us how to manipulate the uv to convert it
        // from glTF conventions to Unity conventions.
        static Semantic GetTexcoordSemantic(
            ImportState state, GltfMaterialBase material, int uvChannel)
        {
            // GL and Unity use the convention "texture origin is lower-left"
            // glTF, DX, Metal, and modern APIs use the convention "texture origin is upper-left"
            // We want to match the logic used by the exporter which generated this gltf, down to its bugs.
            // If we don't know which exporter it was, assume it was written correctly.
            if (state.root.tiltBrushVersion == null)
            {
                // Not Tilt Brush, so we can sort-of safely assume texcoord.xy is a UV coordinate.
                return Semantic.XyIsUv;
            }
            else
            {
                BrushDescriptor desc = GltfMaterialConverter.LookupBrushDescriptor(material);
                // Tilt Brush doesn't use "correct" logic (flip y of every texcoord) because that
                // fails if any importers choose the strategy "flip texture" rather than "flip texcoord.y".
                // Thus it needs to be data-driven.
                if (desc == null)
                {
                    // Might happen in gltf2
                    if (!(material is Gltf2Material material2))
                    {
                        Debug.LogWarning("Unexpected: Non-BrushDescriptor geometry in gltf1");
                        return Semantic.Unspecified;
                    }
                    else
                    {
                        if (material2.pbrMetallicRoughness == null)
                        {
                            Debug.LogWarning("Unexpected: Non-BrushDescriptor, non-PBR geometry in gltf2");
                        }
                        // Pretty safe assumption, since gltf non-texcoord data should be in non-TEXCOORD
                        // semantics (if you're following the spec)
                        return Semantic.XyIsUv;
                    }
                }
                else
                {
                    // VERY SUBTLE incorrectness here -- TB pre-15 didn't flip y on Semantic.XyIsUvZIsDistance
                    // because of an exporter bug. To be perfectly correct, if version <= 14,
                    // this should convert XyIsUvZIsDistance to the (mythical) semantic ZIsDistance.
                    if (uvChannel == 0)
                    {
                        return desc.m_uv0Semantic;
                    }
                    else if (uvChannel == 1)
                    {
                        return desc.m_uv1Semantic;
                    }
                    else
                    {
                        Debug.LogWarningFormat("Unexpected TB texcoord: {0}", uvChannel);
                        return Semantic.Unspecified;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the specified range of data from an accessor.
        /// The result is a copy and is safe to mutate.
        /// </summary>
        /// <param name="accessor">The accessor to read.</param>
        /// <param name="eltRange">The range of elements to read.</param>
        /// <param name="semantic">The semantic to use when reading.</param>
        /// <param name="result">(Out) the result of the read. If this method returns true,
        /// then this will be an array of the appropriate type for the accessor type
        /// (Vector2[] for VEC2, for example). If this method returns false, this will be null.</param>
        /// <returns>True if the data could be fetched; false on failure.</returns>
        static unsafe bool TryGetDataAsArray(GltfAccessorBase accessor, IntRange eltRange, string semantic, out Array result)
        {
            const GltfAccessorBase.ComponentType FLOAT = GltfAccessorBase.ComponentType.FLOAT;
            if (accessor.type == "VEC2" && accessor.componentType == FLOAT)
            {
                var destination = new Vector2[eltRange.Size];
                fixed (void* destPtr = destination)
                {
                    ReadAccessorData(accessor, eltRange, sizeof(Vector2), (IntPtr)destPtr);
                }
                result = destination;
                return true;
            }
            else if (accessor.type == "VEC3" && accessor.componentType == FLOAT)
            {
                var destination = new Vector3[eltRange.Size];
                fixed (void* destPtr = destination)
                {
                    ReadAccessorData(accessor, eltRange, sizeof(Vector3), (IntPtr)destPtr);
                }
                result = destination;
                return true;
            }
            else if (accessor.type == "VEC4" && accessor.componentType == FLOAT)
            {
                if (semantic.StartsWith("COLOR"))
                {
                    var destination = new Color[eltRange.Size];
                    fixed (void* destPtr = destination)
                    {
                        ReadAccessorData(accessor, eltRange, sizeof(Color), (IntPtr)destPtr);
                    }
                    result = destination;
                    return true;
                }
                else
                {
                    var destination = new Vector4[eltRange.Size];
                    fixed (void* destPtr = destination)
                    {
                        ReadAccessorData(accessor, eltRange, sizeof(Vector4), (IntPtr)destPtr);
                    }
                    result = destination;
                    return true;
                }
            }
            else if (accessor.type == "VEC4"
                     && accessor.componentType == GltfAccessorBase.ComponentType.UNSIGNED_BYTE
                     && semantic.StartsWith("COLOR"))
            {
                var destination = new Color32[eltRange.Size];
                fixed (void* destPtr = destination)
                {
                    ReadAccessorData(accessor, eltRange, sizeof(Color32), (IntPtr)destPtr);
                }
                result = destination;
                return true;
            }
            else if (accessor.type == "SCALAR" && accessor.componentType == FLOAT)
            {
                var destination = new float[eltRange.Size];
                fixed (void* destPtr = destination)
                {
                    ReadAccessorData(accessor, eltRange, sizeof(float), (IntPtr)destPtr);
                }
                result = destination;
                return true;
            }
            else if (accessor.type == "SCALAR"
                     && accessor.componentType == GltfAccessorBase.ComponentType.UNSIGNED_SHORT)
            {
                var destination = new ushort[eltRange.Size];
                fixed (void* destPtr = destination)
                {
                    ReadAccessorData(accessor, eltRange, sizeof(ushort), (IntPtr)destPtr);
                }
                result = destination;
                return true;
            }
            else if (accessor.type == "SCALAR"
                     && accessor.componentType == GltfAccessorBase.ComponentType.UNSIGNED_INT)
            {
                var destination = new UInt32[eltRange.Size];
                fixed (void* destPtr = destination)
                {
                    ReadAccessorData(accessor, eltRange, sizeof(UInt32), (IntPtr)destPtr);
                }
                result = destination;
                return true;
            }
            else
            {
                Debug.LogWarningFormat(
                    "Unknown accessor type {0} componentType {1} for {2}",
                    accessor.type, accessor.componentType, semantic);
            }
            result = null;
            return false;
        }

        static Array GetDataAsArray(GltfAccessorBase accessor, IntRange eltRange, string semantic)
        {
            Array result;
            if (!TryGetDataAsArray(accessor, eltRange, semantic, out result))
            {
                throw new Exception(string.Format(
                    "Failed to get GLTF data as array from accessor type {0} {1} with semantic '{2}'",
                    accessor.type, accessor.componentType, semantic));
            }
            return result;
        }

        /// Reads (range.Size * elementLength) bytes from the specified point
        /// in the bufferview, into the specified pointer.
        ///
        /// eltRange      Half-open range of elements to fetch
        /// elementLength Length of a single element, in bytes
        /// destination   Pointer to copy to
        static void ReadAccessorData(
            GltfAccessorBase accessor, IntRange eltRange, int elementLength, IntPtr destination)
        {
            GltfBufferViewBase view = accessor.BufferViewPtr;

            IntRange viewRange = new IntRange
            {
                min = view.byteOffset,
                max = view.byteOffset + view.byteLength
            };

            IntRange readRange = new IntRange
            {
                min = view.byteOffset + accessor.byteOffset + eltRange.min * elementLength,
                max = view.byteOffset + accessor.byteOffset + eltRange.max * elementLength
            };

            if (IntRange.Union(viewRange, readRange) != viewRange)
            {
                Debug.LogErrorFormat("Read error: {0} doesn't contain {1}", viewRange, readRange);
                return;
            }

            view.BufferPtr.data.Read(destination, readRange.min, readRange.Size);
        }

        static void GenericSetUv(Mesh mesh, int channel, Array array)
        {
            // This is kind of terrible; Unity's API requires a List<>
            // but under the hood it converts the List<> straight back into a [].
            if (array is Vector2[])
            {
                mesh.SetUVs(channel, ((Vector2[])array).ToList());
            }
            else if (array is Vector3[])
            {
                mesh.SetUVs(channel, ((Vector3[])array).ToList());
            }
            else if (array is Vector4[])
            {
                mesh.SetUVs(channel, ((Vector4[])array).ToList());
            }
            else
            {
                Debug.LogWarningFormat("Cannot assign {0} to uv{1}", array.GetType(), channel);
            }
        }

        enum ColorSpace { Unknown, Srgb, Linear };

        /// Returns the color space expected by any shaders which consume the vertex color channel.
        static ColorSpace GetDesiredColorSpace(GltfRootBase root)
        {
            if (root.tiltBrushVersion != null)
            {
                // Tilt Brush Toolkit shaders want linear color
                return ColorSpace.Linear;
            }
            else if (root.blocksVersion != null)
            {
                // Blocks import currently borrows Tilt Brush shaders, which want linear color
                // This may change when Blocks shaders are imported.
                return ColorSpace.Linear;
            }
            return ColorSpace.Unknown;
        }

        /// Returns the color space used by the gltf's vertex color channel.
        static ColorSpace GetActualColorSpace(GltfRootBase root)
        {
            // There is no metadata in gltf to tell us, but we can infer it based on versions
            if (root.tiltBrushVersion != null)
            {
                // all known versions of Tilt Brush emit srgb colors to gltf
                return ColorSpace.Srgb;
            }
            else if (root.blocksVersion != null)
            {
                // all known versions of Blocks emit srgb colors to gltf
                return ColorSpace.Srgb;
            }
            return ColorSpace.Unknown;
        }

        /// Removes and returns a value from the list
        private static T ListPop<T>(List<T> list)
        {
            int idx = list.Count - 1;
            if (idx < 0) { throw new InvalidOperationException("empty"); }
            T ret = list[idx];
            list.RemoveAt(idx);
            return ret;
        }

        /// Modifies node tree to ensure that nodes contain at most one mesh
        private static void PostProcessRemoveMultipleMeshes(Gltf1Root root)
        {
            // Reify the dict iterator because we're going to mutate the dict
            foreach (var keyvalue in root.nodes.ToArray())
            {
                Gltf1Node node = keyvalue.Value;
                if (node.meshes == null) { continue; }
                while (node.meshes.Count > 1)
                {
                    string meshName = ListPop(node.meshes);

                    string childNodeName;
                    for (int i = 0; ; ++i)
                    {
                        childNodeName = string.Format("{0}_{1}", meshName, i);
                        if (!root.nodes.ContainsKey(childNodeName)) { break; }
                    }

                    var child = new Gltf1Node
                    {
                        name = childNodeName,
                        meshes = new List<string> { meshName },
                    };

                    root.nodes.Add(childNodeName, child);
                    if (node.children == null)
                    {
                        node.children = new List<string>();
                    }
                    node.children.Add(child.name);
                }
            }
        }

        /// <summary>
        /// Computes and returns the given scene's bounding box (AABB).
        /// </summary>
        /// <param name="scene">The scene whose bounding box to compute.</param>
        /// <param name="approximate">Computes more quickly, but the result is not perfectly tight</param>
        /// <returns>The bounding box.</returns>
        public static Bounds? ComputeSceneBoundingBoxInGltfSpace(GltfSceneBase scene, bool approximate)
        {
            Bounds? bounds = null;
            Matrix4x4 matrix = Matrix4x4.identity;
            foreach (GltfNodeBase node in scene.Nodes)
            {
                EncapsulateNodeInBoundingBox(matrix, node, approximate, ref bounds);
            }
            return bounds;
        }

        private static void EncapsulateNodeInBoundingBox(
            Matrix4x4 rootFromParent, GltfNodeBase node, bool approximate, ref Bounds? bounds)
        {
            Matrix4x4 rootFromNode = node.matrix != null ? (rootFromParent * node.matrix.Value) : rootFromParent;
            if (node.Mesh != null)
            {
                foreach (GltfPrimitiveBase prim in node.Mesh.Primitives)
                {
                    if (approximate)
                    {
                        EncapsulatePrimInBoundingBoxApproximate(rootFromNode, prim, ref bounds);
                    }
                    else
                    {
                        EncapsulatePrimInBoundingBoxExact(rootFromNode, prim, ref bounds);
                    }
                }
            }
            foreach (GltfNodeBase child in node.Children)
            {
                EncapsulateNodeInBoundingBox(rootFromNode, child, approximate, ref bounds);
            }
        }

        // Returns the smallest axis-aligned bounding box which contains
        // b's corners transformed by m.
        private static Bounds Transform(Matrix4x4 m, Bounds b)
        {
            Vector3 halfExtent = b.extents;
            Vector3 mc = m.MultiplyPoint(b.center);
            Vector3 mx = m * new Vector4(halfExtent.x, 0, 0, 1);
            Vector3 my = m * new Vector4(0, halfExtent.y, 0, 1);
            Vector3 mz = m * new Vector4(0, 0, halfExtent.z, 1);
            Bounds mb = new Bounds(center: mc, size: Vector3.zero);
            mb.Encapsulate(mc + mx + my + mz);
            mb.Encapsulate(mc + mx + my - mz);
            mb.Encapsulate(mc + mx - my + mz);
            mb.Encapsulate(mc + mx - my - mz);
            mb.Encapsulate(mc - mx + my + mz);
            mb.Encapsulate(mc - mx + my - mz);
            mb.Encapsulate(mc - mx - my + mz);
            mb.Encapsulate(mc - mx - my - mz);
            return mb;
        }

        private static void EncapsulatePrimInBoundingBoxApproximate(
            Matrix4x4 rootFromNode, GltfPrimitiveBase prim, ref Bounds? bounds)
        {
            GltfAccessorBase pos = prim.GetAttributePtr("POSITION");
            if (pos.componentType != GltfAccessorBase.ComponentType.FLOAT) { return; }
            if (pos.type != "VEC3") { return; }

            Bounds boundsInNodeSpace = new Bounds();
            if (pos.min != null && pos.min.Count == 3)
            {
                boundsInNodeSpace.min = new Vector3(pos.min[0], pos.min[1], pos.min[2]);
            }
            if (pos.max != null && pos.max.Count == 3)
            {
                boundsInNodeSpace.max = new Vector3(pos.max[0], pos.max[1], pos.max[2]);
            }
            Bounds boundsInRootSpace = Transform(rootFromNode, boundsInNodeSpace);
            if (bounds != null)
            {
                boundsInRootSpace.Encapsulate(bounds.Value.min);
                boundsInRootSpace.Encapsulate(bounds.Value.max);
            }
            bounds = boundsInRootSpace;
        }

        private static void EncapsulatePrimInBoundingBoxExact(
            Matrix4x4 rootFromNode, GltfPrimitiveBase prim, ref Bounds? bounds)
        {
            GltfAccessorBase vertexAccessor = prim.GetAttributePtr("POSITION");

            IntRange vertRange = new IntRange();
            vertRange.min = 0;
            vertRange.max = vertexAccessor.count - 1;
            Array data = GetDataAsArray(vertexAccessor, vertRange, null);
            if (!(data is Vector3[]))
            {
                throw new Exception("Vertex accessor didn't convert to Vector3[]. We got: " + data.GetType());
            }
            Vector3[] vertices = (Vector3[])data;
            foreach (Vector3 vertex in vertices)
            {
                Vector3 transformed = rootFromNode * vertex;
                if (bounds != null)
                {
                    Bounds newBounds = bounds.Value;
                    newBounds.Encapsulate(transformed);
                    bounds = newBounds;
                }
                else
                {
                    bounds = new Bounds(transformed, Vector3.zero);
                }
            }
        }
    }

}