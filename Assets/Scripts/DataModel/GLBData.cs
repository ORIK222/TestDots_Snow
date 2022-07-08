using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DataModels;
using Download.Core.Utils.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Download.Core.Editor
{
    [Serializable]
    public class GLBData 
    {
        [JsonProperty("title")]
        private string name = default;
        [JsonProperty("price")]
        private float price;
        [JsonProperty("priceType")]
        private string priceType;
        [JsonProperty("creator")] 
        private Creator creator = default;
        [JsonProperty("owner")]
        private Creator owner = default;
        [JsonProperty("description")]
        private string description =default;
        [JsonProperty("originFileGenerator")]
        private string originFileGenerator = default;
        [JsonProperty("objectData")]
        private ObjectData objectData = default;
        [JsonProperty("urls")]
        private Urls urls = default;
        [JsonProperty("environments")]
        private GlbEnvironment environment;
        [JsonProperty("purchaseTransactionData")]
        private PurchaseTransactionData purchaseTransactionData;
        
        private TextAsset cashedData;
        private Texture2D cashedPreview;
        
        [JsonIgnore] private Collection rootCollection; 
        [JsonIgnore] private string previewPath;
        [JsonIgnore] private string filePath;
        [JsonIgnore] private int size;
        [JsonIgnore] private DataState state;

        [JsonIgnore]
        public PurchaseTransactionData PurchaseData
        {
            get => purchaseTransactionData;
            set => purchaseTransactionData = value;
        }

        [JsonIgnore]
        public GlbEnvironment Environment => environment;


        [JsonIgnore]
        public string Name => name;
        [JsonIgnore] public float Price => price;
        [JsonIgnore] public string PriceType => priceType;
        [JsonIgnore] public Creator Creator => creator;
        [JsonIgnore] public Creator Owner => owner;
        [JsonIgnore] public string Description => description ??= string.Empty;
        [JsonIgnore] public string OriginFileGenerator => originFileGenerator ??= string.Empty;
        [JsonIgnore] public Collection RootCollection => rootCollection;

        [JsonIgnore]
        public TextAsset CashedData
        {
            get => cashedData;
            set
            {
                cashedData = value;
                if(cashedData != null)
                    OnObjectDownloaded?.Invoke();
            }
        }

        [JsonIgnore] public bool IsCompressed => Urls != null && !Urls.CompressedGlb.IsNullOrEmpty();

        [HideInInspector] [JsonIgnore] public UnityEvent OnObjectDownloaded = new UnityEvent();
        [HideInInspector] [JsonIgnore] public UnityEvent OnPreviewDownloaded = new UnityEvent();
        [HideInInspector] [JsonIgnore] public UnityEvent OnStateUpdate = new UnityEvent();


        [JsonIgnore] public ObjectData ObjectData => objectData;
        [JsonIgnore] public Urls Urls => urls;

        [JsonIgnore]
        public DataState State
        {
            get => state;
            set
            {
                state = value;
                OnStateUpdate?.Invoke();
                if (IsObjectDownloaded)
                {
                    OnObjectDownloaded?.Invoke();
                }
            }
        }

        [JsonIgnore]
        public bool IsStateSuccess
        {
            get;
            set;
        } = true;

        [JsonIgnore] public int Size
        {
            get => size;
            set => size = value;
        }

        [JsonIgnore]
        public string CompressedFilePath
        {
            get; set;
        }

        [JsonIgnore] public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                if (IsObjectDownloaded)
                {
                    OnObjectDownloaded?.Invoke();
                }
            }
        }

        [JsonIgnore] public string PreviewPath
        {
            get => previewPath;
            set
            {
                previewPath = value;
                OnPreviewDownloaded?.Invoke();
            }
        }
        [JsonIgnore] public Texture2D CashedPreview
        {
            get => cashedPreview;
            set
            {
                cashedPreview = value;
                OnPreviewDownloaded?.Invoke();
            }
        }

        [JsonIgnore] public string ErrorText { get; set; }

        [JsonIgnore] public bool IsObjectDownloaded => !filePath.IsNullOrEmpty() && this.State == DataState.UpToDate;
        [JsonIgnore] public bool IsPreviewDownloaded => !previewPath.IsNullOrEmpty();

        public void InitializeLocalData(Collection rootCollection)
        {
            this.rootCollection = rootCollection;
        }

        public GLBData(ObjectData objectData, Urls urls)
        {
            this.objectData = objectData;
            this.urls = urls;
        }
        /// <summary>
        /// Gets the artwork information.
        /// </summary>
        /// <returns>The artwork info to identify it.</returns>
        public string GetArtworkInfo()
        {
            string artworkInfo = null;
            if (this.Urls != null)
            {
                if (this.Urls.Glb != null)
                {
                    artworkInfo = "Artwork Glb url: " + this.Urls.Glb;
                }
                else if (this.Urls.CompressedGlb != null)
                {
                    artworkInfo = "Artwork compressed Glb url: " + this.Urls.CompressedGlb;
                }
                else if (this.Urls.Icon != null)
                {
                    artworkInfo = "Artwork icon url: " + this.Urls.Icon;
                }
            }

            if (artworkInfo == null)
            {
                if (this.PreviewPath != null)
                {
                    artworkInfo = "Artwork preview url: " + this.PreviewPath;
                }
                else if (!string.IsNullOrEmpty(this.Name))
                {
                    artworkInfo = "Artwork name: " + this.Name;
                }
                else if (this.ObjectData != null && !string.IsNullOrEmpty(this.ObjectData.Name))
                {
                    artworkInfo = "Artwork object name: " + this.ObjectData.Name;
                }
                else if (this.Description != null && !string.IsNullOrEmpty(this.Description))
                {
                    artworkInfo = "Artwork description: " + this.Description;
                }
            }

            return artworkInfo;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.name;
        }
    }

    public enum DataState
    {
        Queued,
        GetSizeQueue,
        GetSize,
        DownloadPreviewQueue,
        DownloadPreview,
        DownloadFileQueue,
        DownloadFile,
        DecompressQueue,
        Decompress,
        StartCashing,
        Cashed,
        UpToDate,
        Error
    }
    
}