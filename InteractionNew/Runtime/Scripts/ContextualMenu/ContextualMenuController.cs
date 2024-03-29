using Reflectis.SDK.Core;
using Reflectis.SDK.Transitions;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;
using static Reflectis.SDK.InteractionNew.ContextualMenuManageable;

namespace Reflectis.SDK.InteractionNew
{
    public class ContextualMenuController : MonoBehaviour
    {
        [Serializable]
        private class ContextualMenuItem
        {
            public EContextualMenuOption key;
            public GameObject value;
        }

        [SerializeField] private List<ContextualMenuItem> contextualMenuItems = new();

        public UnityEvent onShow;
        public UnityEvent onHide;

        protected AbstractTransitionProvider transitionProvider;


        protected virtual void Awake()
        {
            transitionProvider = GetComponentInChildren<AbstractTransitionProvider>();
            Unsetup();
            Hide();
        }

        public virtual void Setup(EContextualMenuOption options)
        {
            contextualMenuItems.ForEach(x =>
            {
                if (x.value)
                    x.value.SetActive(options.HasFlag(x.key));
            });
        }

        public virtual void Unsetup()
        {
            contextualMenuItems.ForEach(x =>
            {
                if (x.value)
                    x.value.SetActive(false);
            });
        }

        public virtual async Task Show()
        {
            gameObject.SetActive(true);
            await transitionProvider.DoTransitionAsync(true);
            onShow?.Invoke();
        }


        public virtual async Task Hide()
        {
            await transitionProvider.DoTransitionAsync(false);
            onHide?.Invoke();
            gameObject.SetActive(false);
        }

        public void OnContextualMenuButtonClicked(int option)
        {
            SM.GetSystem<ContextualMenuSystem>().SelectedInteractable.OnContextualMenuButtonClicked((EContextualMenuOption)option);
        }
    }
}
