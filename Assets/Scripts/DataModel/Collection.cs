using System;
using System.Collections.Generic;
using Download.Core.Editor;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace DataModels
{
    [Serializable]
    public class Collection
    {
        [JsonProperty("collection")] private CollectionInfo collectionInfo = new CollectionInfo();
        [JsonProperty("artworks")] private List<GLBData> artworks;

        [JsonIgnore] private float size;

        [JsonIgnore]
        public List<GLBData> ArtWorks
        {
            get => artworks;
            set => artworks = value;
        }

        [JsonIgnore] public CollectionInfo Info => collectionInfo;
        [JsonIgnore] public UnityEvent OnCollectionSorted = new UnityEvent();
        [JsonIgnore] public bool IsSorted { get; private set; }
        [JsonIgnore] public bool PreviewsLoaded { get; set; }

        public void Sort(GlbDataMinSizeComparer comparer)
        {
            artworks.Sort(comparer);
            IsSorted = true;
            OnCollectionSorted?.Invoke();
        }

        [JsonIgnore]

    public float Size
        {
            get
            {
                if (size == 0)
                {
                    var size = 0f;
                    foreach (var artwork in artworks)
                        size += artwork.Size;
                    
                    this.size = size;
                }

                return this.size;
            }
        }
    }
}