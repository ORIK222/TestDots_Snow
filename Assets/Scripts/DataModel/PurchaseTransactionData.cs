using Newtonsoft.Json;
using System;
using UnityEngine;

[Serializable]
public class PurchaseTransactionData
{
    [JsonProperty("data")] private string data = default;
    [JsonProperty("to")] private string to = default;
    [JsonProperty("value")] private string value = default;
    [JsonProperty("chainId")] private int chainId = default;

    [JsonIgnore]
    public string Data => data;

    [JsonIgnore]
    public string To => to;

    [JsonIgnore]
    public string Value => value;

    [JsonIgnore]
    public int ChainId => chainId;
}
