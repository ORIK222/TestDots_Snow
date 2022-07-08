using Newtonsoft.Json;
using System;
using UnityEngine;

[Serializable]
public class ColorData
{
    [JsonProperty("color")]
    private string color = default;

    [JsonIgnore]
    public string Color => color;
}
