using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    public class CreatorUrls
    {
        [JsonProperty("picture")] private string iconUrl = default;
        [JsonProperty("twitter")]private string twitter = default;
        [JsonProperty("instagram")]private string instagram = default;
        [JsonProperty("discord")]private string discord = default;

        [JsonIgnore] public string IconUrl => iconUrl;
        [JsonIgnore] public string Twitter => twitter;
        [JsonIgnore] public string Instagram => instagram;
        [JsonIgnore] public string Discord => discord;
    }
}