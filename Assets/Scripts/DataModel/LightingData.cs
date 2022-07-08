using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Download.Core.CustomData;
using UnityEngine;

[Serializable]
public class LightingData 
{
    [JsonProperty("color")]
    private string color = default;

    [JsonProperty("distance")]
    private float distance = default;

    [JsonProperty("offset")]
    private CustomVector3 offset = default;

    [JsonIgnore]
    public string Color => color;

    [JsonIgnore]
    public float Distance => distance;

    [JsonIgnore]
    public CustomVector3 Offset => offset;
    
}