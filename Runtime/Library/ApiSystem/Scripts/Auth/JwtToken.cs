using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Virtuademy.SDK.Core.Utilities
{
    /// <summary>
    /// JWT token used to identify the user
    /// </summary>
    [Serializable, Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    public class JwtToken
    {
        [SerializeField] private string bearer;
        [SerializeField] private DateTime expiry;
        [SerializeField] private string apiLabel;
        [SerializeField] private List<string> apiUrls;

        public string Bearer { get => bearer; set => bearer = value; }
        public DateTime Expiry { get => expiry.ToUniversalTime(); }
        public string ApiLabel { get => apiLabel; set => apiLabel = value; }
        public IEnumerable<Uri> ApiUrls { get => apiUrls.Select(x => new Uri(x)); set => apiUrls = value.Select(x => x.ToString()).ToList(); }

        public bool IsExpired() => DateTime.UtcNow > expiry;
        public bool IsExpired(TimeSpan timeOffset) => DateTime.UtcNow - timeOffset > expiry;
    }
}