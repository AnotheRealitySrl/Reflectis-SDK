using Reflectis.SDK.Core;
using Reflectis.SDK.Platform;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Reflectis.SDK.InteractionNew
{
    //[RequireComponent(typeof(BaseInteractable))]
    public abstract class GenericInteractable : InteractableBehaviourBase, IInteractableBehaviour
    {
        [Flags]
        public enum EGenericInteractableState
        {
            Idle = 0,
            SelectEntering = 1,
            Selected = 2,
            Interacting = 3,
            SelectExiting = 4,
        }

        [Flags]
        public enum EAllowedGenericInteractableState
        {
            Selected = 1,
            Interacting = 2,
            Hovered = 4,
        }


        [SerializeField] private ScriptMachine interactionScriptMachine = null;

        [Header("Allowed states")]
        [SerializeField] private EAllowedGenericInteractableState desktopAllowedStates = EAllowedGenericInteractableState.Selected | EAllowedGenericInteractableState.Interacting;
        [SerializeField] private EAllowedGenericInteractableState vrAllowedStates = EAllowedGenericInteractableState.Selected | EAllowedGenericInteractableState.Interacting;

        public Action<GameObject> OnSelectedActionVisualScripting;

        public ScriptMachine InteractionScriptMachine { get => interactionScriptMachine; set => interactionScriptMachine = value; }

        public EAllowedGenericInteractableState DesktopAllowedStates { get => desktopAllowedStates; set => desktopAllowedStates = value; }
        public EAllowedGenericInteractableState VRAllowedStates { get => vrAllowedStates; set => vrAllowedStates = value; }


        public override bool IsIdleState => CurrentInteractionState == EGenericInteractableState.Idle;

        private EGenericInteractableState currentInteractionState;
        public EGenericInteractableState CurrentInteractionState
        {
            get => currentInteractionState;
            set
            {
                currentInteractionState = value;
                if (currentInteractionState == EGenericInteractableState.Idle)
                {
                    InteractableRef.InteractionState = IInteractable.EInteractionState.Hovered;
                }
            }
        }


        private bool hasHoveredState = false;
        private bool skipSelectState = false;
        private bool hasInteractState = false;

        private List<HoverEnterEventUnit> hoverEnterEventUnits = new List<HoverEnterEventUnit>();

        private List<HoverExitEventUnit> hoverExitEventUnits = new List<HoverExitEventUnit>();

        private List<SelectEnterEventUnit> selectEnterEventUnits = new List<SelectEnterEventUnit>();

        private List<SelectExitEventUnit> selectExitEventUnits = new List<SelectExitEventUnit>();

        private List<InteractEventUnit> interactEventUnits = new List<InteractEventUnit>();

        private void OnDestroy()
        {
            if (!IsIdleState && CurrentInteractionState != EGenericInteractableState.SelectExiting)
            {
                foreach (var unit in selectExitEventUnits)
                {
                    unit.AwaitableTrigger(interactionScriptMachine.GetReference().AsReference(), this);
                }
            }
        }

        public override Task Setup()
        {
            switch (SM.GetSystem<IPlatformSystem>().RuntimePlatform)
            {
                case RuntimePlatform.WebGLPlayer:
                    skipSelectState = !DesktopAllowedStates.HasFlag(EAllowedGenericInteractableState.Selected);
                    hasInteractState = DesktopAllowedStates.HasFlag(EAllowedGenericInteractableState.Interacting);
                    hasHoveredState = DesktopAllowedStates.HasFlag(EAllowedGenericInteractableState.Hovered);
                    break;
                case RuntimePlatform.Android:
                    skipSelectState = !VRAllowedStates.HasFlag(EAllowedGenericInteractableState.Selected);
                    hasInteractState = VRAllowedStates.HasFlag(EAllowedGenericInteractableState.Interacting);
                    hasHoveredState = VRAllowedStates.HasFlag(EAllowedGenericInteractableState.Hovered);
                    break;
            }

            if (interactionScriptMachine != null)
            {
                foreach (var unit in interactionScriptMachine.graph.units)
                {
                    if (unit is HoverEnterEventUnit hoverEnterEventUnit)
                    {
                        hoverEnterEventUnits.Add(hoverEnterEventUnit);
                    }
                    if (unit is HoverExitEventUnit hoverExitEventUnit)
                    {
                        hoverExitEventUnits.Add(hoverExitEventUnit);
                    }
                    if (unit is SelectEnterEventUnit selectEnterEventUnit)
                    {
                        selectEnterEventUnits.Add(selectEnterEventUnit);
                    }
                    if (unit is SelectExitEventUnit selectExitEventUnit)
                    {
                        selectExitEventUnits.Add(selectExitEventUnit);
                    }
                    if (unit is InteractEventUnit interactEventUnit)
                    {
                        interactEventUnits.Add(interactEventUnit);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public override async void OnHoverStateEntered()
        {
            //if (!CanInteract || !hasHoveredState)
            if (CurrentBlockedState != 0 || !hasHoveredState)
                return;

            IEnumerable<Task> hoverEnterUnitsTask = hoverEnterEventUnits.Select(async unit =>
            {
                await unit.AwaitableTrigger(interactionScriptMachine.GetReference().AsReference(), this);
            });

            await Task.WhenAll(hoverEnterUnitsTask);

        }

        public override async void OnHoverStateExited()
        {
            //if (!CanInteract || !hasHoveredState)
            if (CurrentBlockedState != 0 || !hasHoveredState)
                return;

            IEnumerable<Task> hoverExitUnitsTask = hoverExitEventUnits.Select(async unit =>
            {
                await unit.AwaitableTrigger(interactionScriptMachine.GetReference().AsReference(), this);
            });

            await Task.WhenAll(hoverExitUnitsTask);

        }

        public override async Task EnterInteractionState()
        {
            //if (!CanInteract)
            if (CurrentBlockedState != 0)
                return;

            await base.EnterInteractionState();

            CurrentInteractionState = EGenericInteractableState.SelectEntering;


            IEnumerable<Task> selectEnterUnitsTasks = selectEnterEventUnits.Select(async unit =>
            {
                await unit.AwaitableTrigger(interactionScriptMachine.GetReference().AsReference(), this);
            });

            await Task.WhenAll(selectEnterUnitsTasks);

            CurrentInteractionState = EGenericInteractableState.Selected;

            if (skipSelectState)
            {
                await Interact();
            }
        }

        public override async Task ExitInteractionState()
        {
            //if (!CanInteract)
            if (CurrentBlockedState != 0)
                return;

            await base.ExitInteractionState();

            CurrentInteractionState = EGenericInteractableState.SelectExiting;

            IEnumerable<Task> selectExitUnitsTasks = selectExitEventUnits.Select(async unit =>
            {
                await unit.AwaitableTrigger(interactionScriptMachine.GetReference().AsReference(), this);
            });

            await Task.WhenAll(selectExitUnitsTasks);

            CurrentInteractionState = EGenericInteractableState.Idle;

        }

        public async Task Interact()
        {
            //if (!CanInteract)
            if (CurrentBlockedState != 0)
                return;

            if (CurrentInteractionState != EGenericInteractableState.Selected && hasInteractState)
                return;

            CurrentInteractionState = EGenericInteractableState.Interacting;

            IEnumerable<Task> interactUnitsTasks = interactEventUnits.Select(async unit =>
            {
                await unit.AwaitableTrigger(interactionScriptMachine.GetReference().AsReference(), this);
            });

            await Task.WhenAll(interactUnitsTasks);

            CurrentInteractionState = EGenericInteractableState.Selected;

            if (skipSelectState)
            {
                await ExitInteractionState();
            }
        }


#if UNITY_EDITOR

        [CustomEditor(typeof(GenericInteractable))]
        public class GenericInteractableEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                GenericInteractable interactable = (GenericInteractable)target;

                GUIStyle style = new(EditorStyles.label)
                {
                    richText = true
                };

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField($"<b>Current state:</b> {interactable.CurrentInteractionState}", style);
                    //EditorGUILayout.LabelField($"<b>Can interact:</b> {interactable.CanInteract}", style);
                }
            }
        }

#endif
    }
}
