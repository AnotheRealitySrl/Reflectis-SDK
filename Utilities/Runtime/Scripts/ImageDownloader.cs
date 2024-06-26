using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Reflectis.SDK.Utilities
{
    public static class ImageDownloader
    {
        public static Dictionary<string, Texture2D> userIconCached = new Dictionary<string, Texture2D>();

        public static void DownloadImage(string mediaUrl, Action<Texture2D> onCompletionCallback, Action onFailedCallback = null, string key = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = mediaUrl;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Trying to download an image with an empty url!");
                return;
            }
            if (userIconCached.TryGetValue(key, out Texture2D texture))
            {
                if (texture != null)
                {
                    onCompletionCallback(texture);
                }
                else
                {
                    CoroutineRunner.Instance.StartCoroutine(WaitForTextureDownload(mediaUrl, onCompletionCallback, onFailedCallback, key));
                }
            }
            else
            {
                CoroutineRunner.Instance.StartCoroutine(DownloadImageCoroutine(mediaUrl, onCompletionCallback, onFailedCallback, key));
            }
        }

        public static IEnumerator DownloadImageCoroutine(string mediaUrl, Action<Texture2D> onCompletionCallback, Action onFailedCallback, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = mediaUrl;
            }

            userIconCached.Add(key, null);
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(mediaUrl);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(request.error);
                userIconCached.Remove(mediaUrl);
                onFailedCallback?.Invoke();
            }
            else
            {
                onCompletionCallback(((DownloadHandlerTexture)request.downloadHandler).texture);
                userIconCached[key] = ((DownloadHandlerTexture)request.downloadHandler).texture;
            }
        }

        public static IEnumerator WaitForTextureDownload(string mediaUrl, Action<Texture2D> onCompletionCallback, Action onFailedCallback, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = mediaUrl;
            }
            Texture2D texture = null;
            yield return new WaitUntil(() => !userIconCached.ContainsKey(key) || (userIconCached.TryGetValue(key, out texture) && texture != null));
            if (!userIconCached.ContainsKey(key))
            {
                if (onFailedCallback != null)
                {
                    onFailedCallback();
                }
            }
            else
            {
                onCompletionCallback(texture);
            }
        }
    }
}
