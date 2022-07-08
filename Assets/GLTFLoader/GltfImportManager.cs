using System.IO;
using System.Text;
using Newtonsoft.Json;
using Siccity.GLTFUtility;
using UnityEngine;

//Master
namespace DefaultNamespace.GLTFLoader
{
    public static class GltfImportManager
    {
        public static GLTFObject GetGltfObject(string path)
        {
            long binChunkStart;
            string json;
            using (FileStream stream = File.OpenRead(path))
            {
                json = GetGLBJson(stream, out binChunkStart);
            }
            
            GLTFObject gltfObject = JsonConvert.DeserializeObject<GLTFObject>(json);
            Debug.Log(gltfObject.asset.generator);
            return gltfObject;
        }

        public static string GetGenerator(string glbAssetPath)
        {
            var obj = GetGltfObject(glbAssetPath);
            return obj.asset.generator;
        }
        private static string GetGLBJson(Stream stream, out long binChunkStart) {
            byte[] buffer = new byte[12];
            stream.Read(buffer, 0, 12);
            // 12 byte header
            // 0-4  - magic = "glTF"
            // 4-8  - version = 2
            // 8-12 - length = total length of glb, including Header and all Chunks, in bytes.
            string magic = Encoding.Default.GetString(buffer, 0, 4);
            if (magic != "glTF") {
                Debug.LogWarning("Input does not look like a .glb file");
                binChunkStart = 0;
                return null;
            }
            uint version = System.BitConverter.ToUInt32(buffer, 4);
            if (version != 2) {
                Debug.LogWarning("Importer does not support gltf version " + version);
                binChunkStart = 0;
                return null;
            }
            // What do we even need the length for.
            //uint length = System.BitConverter.ToUInt32(bytes, 8);

            // Chunk 0 (json)
            // 0-4  - chunkLength = total length of the chunkData
            // 4-8  - chunkType = "JSON"
            // 8-[chunkLength+8] - chunkData = json data.
            stream.Read(buffer, 0, 8);
            uint chunkLength = System.BitConverter.ToUInt32(buffer, 0);
            string json;
            using (TextReader reader = new StreamReader(stream))
            {
                char[] jsonChars = new char[chunkLength];
                reader.Read(jsonChars, 0, (int)chunkLength);
                json = new string(jsonChars);
            }

            // Chunk
            binChunkStart = chunkLength + 20;

            // Return json
            return json;
        }
    }
}

