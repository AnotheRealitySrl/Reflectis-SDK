using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SPACS.Toolkit.Extensions.Runtime
{
    public static class ActionExtensions
    {
        public static void ToggleAction(InputActionReference actionReference, bool enableAction)
        {
            var action = GetInputAction(actionReference);
            if (action != null && enableAction && !action.enabled)
            {
                action.Enable();
            }
            if (action != null && !enableAction && action.enabled)
            {
                action.Disable();
            }
        }

        public static InputAction GetInputAction(InputActionReference actionReference)
        {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
            return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
        }
    }
}