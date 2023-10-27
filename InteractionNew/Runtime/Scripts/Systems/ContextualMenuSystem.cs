using Reflectis.SDK.Core;
using Reflectis.SDK.Transitions;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Reflectis.SDK.InteractionNew
{
    public abstract class ContextualMenuSystem : BaseSystem
    {
        [SerializeField] private GameObject contextualMenuPrefab;
        [SerializeField] private float showTime = 1.5f;
        [SerializeField] private float hideTime = 1.5f;
        [SerializeField] private bool createMenuOnInit = true;
        [SerializeField] private bool dontDestroyOnLoad = false;
        [SerializeField] private InputActionReference contextualMenuInputActionRef;

        [Header("Scriptable actions")]
        [SerializeField] private List<AwaitableScriptableAction> onHoverEnterActions = new();
        [SerializeField] private List<AwaitableScriptableAction> onHoverExitActions = new();

        protected GameObject contextualMenu;
        protected ContextualMenuManageable selectedInteractable;


        public GameObject ContextualMenuInstance => contextualMenu;

        public float ShowToastTime { get => showTime; private set => showTime = value; }
        public float HideToastTime { get => hideTime; private set => hideTime = value; }

        public List<AwaitableScriptableAction> OnHoverEnterActions => onHoverEnterActions;
        public List<AwaitableScriptableAction> OnHoverExitActions => onHoverExitActions;

        public override void Init()
        {
            if (createMenuOnInit)
            {
                CreateMenu();
            }

            contextualMenuInputActionRef.action.Enable();

            contextualMenuInputActionRef.action.started += OnMenuActivate;
            contextualMenuInputActionRef.action.performed += OnMenuActivate;
            contextualMenuInputActionRef.action.canceled += OnMenuActivate;
        }

        private void OnDestroy()
        {
            contextualMenuInputActionRef.action.started -= OnMenuActivate;
            contextualMenuInputActionRef.action.performed -= OnMenuActivate;
            contextualMenuInputActionRef.action.canceled -= OnMenuActivate;
        }

        #region Input actions callbacks

        public abstract void OnMenuActivate(InputAction.CallbackContext context);

        #endregion

        #region API

        public void CreateMenu()
        {
            contextualMenu = Instantiate(contextualMenuPrefab);

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(contextualMenu);
        }

        public void DestroyMenu()
        {
            if (contextualMenu)
            {
                Destroy(contextualMenu);
                contextualMenu = null;
            }
        }

        public void ShowContextualMenu()
        {
            contextualMenu.GetComponent<AbstractTransitionProvider>().EnterTransition();
        }

        public void HideContextualMenu()
        {
            contextualMenu.GetComponent<AbstractTransitionProvider>().ExitTransition();
        }

        #endregion
    }
}
