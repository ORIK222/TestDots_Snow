using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    public class CollectionInfo
    {
        [JsonProperty("title")] private string name =  "default";
        [JsonProperty("cover")] private string cover = "url";
        [JsonProperty("icon")] private string iconUrl = "url";

        [JsonIgnore] public string Name => name;
        [JsonIgnore] public string Cover => cover;
        [JsonIgnore] public string IconUrl => iconUrl;
    }
}