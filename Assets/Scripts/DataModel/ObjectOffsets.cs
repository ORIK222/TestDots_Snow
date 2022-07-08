using System;
using Download.Core.CustomData;
using Newtonsoft.Json;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    public class ObjectOffsets
    {
        [JsonProperty("position")] private CustomVector3 position = default;
        [JsonProperty("localScale")] private CustomVector3 localScale = default;

        //public Vector3 Position => position.Value();
        //public Vector3 LocalScale => localScale.Value();
        [JsonIgnore] public CustomVector3 Position => position;
        [JsonIgnore] public CustomVector3 LocalScale => localScale;
    }
}