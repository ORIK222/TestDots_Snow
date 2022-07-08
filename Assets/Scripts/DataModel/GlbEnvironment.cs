using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GlbEnvironment
{
    [JsonProperty("background")]
    private ColorData backgroundColor;

    [JsonProperty("lightings")]
    private LightingData[] lighting;

    [JsonIgnore] public ColorData BackgroundColor => backgroundColor;

    [JsonIgnore] public LightingData[] LightingData => lighting;
}
