using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    public class Urls
    {
        [JsonProperty(PropertyName = "glb")] private string glb = default;
        [JsonProperty(PropertyName = "glb-gz")] private string compressedGlb = default;
        [JsonProperty(PropertyName = "thumbnail")] private string icon = default;
        [JsonProperty(PropertyName = "glb-sha256")] private string glb_sha256 = default;
        [JsonProperty(PropertyName = "glb-gz-sha256")] private string glb_gz_sha256 = default;
        [JsonProperty(PropertyName = "cashedGlbUrl")] private string cashedGlbUrl = default;
        [JsonProperty(PropertyName = "cashedIconUrl")] private string cashedIconUrl = default;

        [JsonIgnore] public string Glb => glb;
        [JsonIgnore] public string CompressedGlb => compressedGlb;
        [JsonIgnore] public string Icon => icon;

        [JsonIgnore]
        public string CashedGlbUrl
        {
            get => cashedGlbUrl;
            set => cashedGlbUrl = value;
        }

        [JsonIgnore]
        public string CashedIconUrl
        {
            get => cashedIconUrl;
            set => cashedIconUrl = value;
        }

        [JsonIgnore] public string GlbSha256 => glb_sha256;
        [JsonIgnore] public string GlbGzSha256 => glb_gz_sha256;

        public Urls(string glb, string icon)
        {
            this.glb = glb;
            this.icon = icon;
        }
    }
}