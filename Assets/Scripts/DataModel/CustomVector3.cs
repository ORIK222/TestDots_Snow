using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Download.Core.CustomData
{
    [Serializable]
    public class CustomVector3
    {
        [JsonProperty("x")] public float x = default;
        [JsonProperty("y")] public float y = default;
        [JsonProperty("z")] public float z = default;

        public Vector3 Value()
        {
                var vector = new Vector3
                {
                    x = x,
                    y = y,
                    z = z
                };
                
                return vector;
        }

        public CustomVector3(Vector3 origin)
        {
            x = origin.x;
            y = origin.y;
            z = origin.z;
        }
        public CustomVector3(){}

        public static Vector3 operator +(Vector3 vector, CustomVector3 customVector)
        {
            return vector + customVector.Value();
        }

        public static Vector3 operator -(Vector3 vector, CustomVector3 customVector)
        {
            return vector - customVector.Value();
        }
    }

    public static class CustomVector3Extension
    {
        public static bool IsNull(this CustomVector3 custom) => custom == null;
        public static bool IsZero(this CustomVector3 custom) => custom.Value() == Vector3.zero;
    }
}
