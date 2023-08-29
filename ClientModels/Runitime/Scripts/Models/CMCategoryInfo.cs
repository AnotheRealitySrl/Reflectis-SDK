using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reflectis.SDK.ClientModels
{
    public class CMCategoryInfo
    {
        [SerializeField] private CMCategory category;
        [SerializeField] private CMCategory[] subcategories;

        public CMCategoryInfo(CMCategory category, CMCategory[] subcategories)
        {
            this.category = category;
            this.subcategories = subcategories;
        }

        public CMCategory Category { get => category; set => category = value; }
        public CMCategory[] Subcategories { get => subcategories; set => subcategories = value; }

    }
}
