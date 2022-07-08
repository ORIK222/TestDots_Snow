using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    public class CollectionsHolder
    {
        [JsonProperty("version")] private string version = default;

        [JsonProperty("chainId")]
        private int chainId = default;
        [JsonProperty("collections")] List<Collection> collections = default;

        [JsonIgnore]
        public List<Collection> Collections
        {
            get => collections;
            set => collections = value;
        }
        [JsonIgnore] public string Version => version;
        [JsonIgnore] public int ChainId => chainId;

        [JsonIgnore]
        public int TotalObjects
        {
            get
            {
                if (collections == null) return 0;
                int totalCount = 0;
                foreach (var collection in collections)
                {
                    if(collection.ArtWorks == null) continue;
                    totalCount += collection.ArtWorks.Count;
                }

                return totalCount;
            }
        }
    }
}