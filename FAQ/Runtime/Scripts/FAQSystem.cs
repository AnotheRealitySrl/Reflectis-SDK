using Reflectis.SDK.Core;
using Reflectis.SDK.Utilities.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Reflectis.SDK.FAQ
{
    [CreateAssetMenu(menuName = "Reflectis/SDK-FAQ/FAQSystem", fileName = "FAQSystem")]
    public class FAQSystem : BaseSystem, IFAQSystem
    {
        [SerializeField]
        [TextArea(1,30)]
        private string mockFAQResponse;

        private FAQCategoryData[] faqs;

        public FAQCategoryData GetFAQs(string category)
        {
            //Not async method we download the faq on init
            //If necessary it has to be reworked
            var requestedFaq = faqs.FirstOrDefault(x => x.CategoryTitle == category);
            if (requestedFaq != null)
            {
                return requestedFaq;
            }
            else
            {
                Debug.LogError($"There are no faq for the given category: {category}");
                return null;
            }
        }

        public override async void Init()
        {
            await DownloadFAQs();
        }

        private async Task DownloadFAQs()
        {
            faqs = await Task.FromResult(JsonArrayHelper.FromJson<FAQCategoryData>(mockFAQResponse));
            foreach (var faq in faqs)
            {
                Debug.Log("Category" + faq.CategoryTitle);
            }
        }
    }
}
