using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

namespace Virtuademy.SDK.Http
{
    /// <summary>
    /// Static helper for creating HTTP requests.
    /// Replaces the instance-based HttpSystem with static methods.
    /// </summary>
    public static class HttpHelper
    {
        public enum ERequestBodyType
        {
            None,
            RawString,
            RawBytes,
            MultipartFormData
        }

        public static readonly string GET = UnityWebRequest.kHttpVerbGET;
        public static readonly string POST = UnityWebRequest.kHttpVerbPOST;
        public static readonly string PUT = UnityWebRequest.kHttpVerbPUT;
        public static readonly string DELETE = UnityWebRequest.kHttpVerbDELETE;
        public static readonly string CREATE = UnityWebRequest.kHttpVerbCREATE;
        public static readonly string HEAD = UnityWebRequest.kHttpVerbHEAD;

        /// <summary>
        /// Creates a UnityWebRequest for performing an HTTP request.
        /// </summary>
        public static UnityWebRequest CreateHttpRequest(
            string method,
            string uri,
            ERequestBodyType requestBodyType = ERequestBodyType.None,
            object body = null,
            Dictionary<string, string> queryParams = null,
            Dictionary<string, string> headers = null,
            CertificateHandler certificateHandler = default,
            bool secureConnection = true)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri parsedUri) ||
                (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            {
                uri = $"http{(secureConnection ? "s" : string.Empty)}://{uri}";
            }

            if (queryParams != null)
            {
                uri += CreateQueryString(queryParams);
            }

            string inferredContentType = null;
            body ??= string.Empty;

            UnityWebRequest request;

            switch (requestBodyType)
            {
                case ERequestBodyType.None:
                    request = new UnityWebRequest(uri, method);
                    break;

                case ERequestBodyType.RawString:
                    if (body is string stringBody)
                    {
                        request = new UnityWebRequest(uri, method)
                        {
                            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(stringBody))
                        };
                        inferredContentType = "application/json";
                    }
                    else
                    {
                        Debug.LogError($"Body type for ERequestBodyType.RawString must be a string. Got {body.GetType()} type on request {uri}!");
                        return null;
                    }
                    break;

                case ERequestBodyType.RawBytes:
                    if (body is byte[] byteBody)
                    {
                        request = new UnityWebRequest(uri, method)
                        {
                            uploadHandler = new UploadHandlerRaw(byteBody)
                        };
                        inferredContentType = "application/octet-stream";
                    }
                    else
                    {
                        Debug.LogError("Body type for ERequestBodyType.RawBytes must be a byte array.");
                        return null;
                    }
                    break;

                case ERequestBodyType.MultipartFormData:
                    if (body is List<IMultipartFormSection> multipartSections)
                    {
                        if (multipartSections.Count > 0)
                        {
                            request = UnityWebRequest.Post(uri, multipartSections);

                            if (method != POST)
                            {
                                request.method = method;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("ERequestBodyType.MultipartFormData specified but the list of IMultipartFormSection is empty. Treating as no body.");
                            request = new UnityWebRequest(uri, method);
                        }
                    }
                    else
                    {
                        Debug.LogError("Body type for ERequestBodyType.MultipartFormData must be List<IMultipartFormSection>.");
                        return null;
                    }
                    break;

                default:
                    Debug.LogError($"Unsupported request body type: {requestBodyType}");
                    return null;
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = certificateHandler;

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        if (requestBodyType == ERequestBodyType.MultipartFormData)
                        {
                            Debug.LogWarning("You've manually specified a Content-Type header for a multipart/form-data request. Unity handles this automatically. Your custom Content-Type will be ignored.");
                            continue;
                        }
                        request.SetRequestHeader(header.Key, header.Value);
                        inferredContentType = null;
                    }
                    else
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }
            }

            if (requestBodyType != ERequestBodyType.None && requestBodyType != ERequestBodyType.MultipartFormData)
            {
                if (request.GetRequestHeader("Content-Type") == null)
                {
                    if (!string.IsNullOrEmpty(inferredContentType))
                    {
                        request.SetRequestHeader("Content-Type", inferredContentType);
                    }
                    else
                    {
                        request.SetRequestHeader("Content-Type", "application/octet-stream");
                    }
                }
            }

            return request;
        }

        public static string CreateQueryString(Dictionary<string, string> queryParams)
        {
            string queryString = string.Empty;
            bool isFirst = true;
            foreach (KeyValuePair<string, string> item in queryParams)
            {
                queryString += (isFirst ? "?" : "&") + UnityWebRequest.EscapeURL(item.Key) + "=" + UnityWebRequest.EscapeURL(item.Value);
                isFirst = false;
            }
            return queryString;
        }
    }
}
