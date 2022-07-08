using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    public class ObjectData
    {
        [JsonProperty("name")] private string name = default;
        [JsonProperty("offsets")] private ObjectOffsets offsets = default;

        [JsonIgnore] public string Name => name;
        [JsonIgnore] public ObjectOffsets Offsets => offsets;

        public static ObjectData GetTestData()
        {
            return new ObjectData() { name = "wqeqe" };
        }

    }
}