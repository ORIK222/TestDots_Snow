using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    public class Creator
    {
        [JsonProperty("name")] private string name = default;
        [JsonProperty("description")] private string description = default;
        [JsonProperty("urls")] private CreatorUrls urls = default;

        [JsonIgnore] public string Name => name;
        [JsonIgnore] public string Description => description;
        [JsonIgnore] public CreatorUrls Urls => urls;
    }
}