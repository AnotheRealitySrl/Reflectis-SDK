using System;

using UnityEngine;

namespace Virtuademy.SDK.Core
{
    [Serializable, Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    public class ApiInfo
    {
        [SerializeField] private string label;
        [SerializeField] private DateTime serverTime;

        public string Label => label;
        public DateTime ServerTime => serverTime;
    }
}
