
using Reflectis.SDK.CharacterController;
using Reflectis.SDK.Core;
using UnityEngine;

namespace Reflectis.SDK.Avatars
{
    public class AvatarConfigControllerDesktop : AvatarConfigControllerBase
    {
        #region Inspector variables

        [SerializeField, Tooltip("Default animator for masculine avatars")]
        private RuntimeAnimatorController masculineAnimatorController;

        [SerializeField, Tooltip("Default animator for feminine avatars")]
        private RuntimeAnimatorController feminineAnimatorController;
        #endregion

        public override void OnAvatarLoadCompletion(GameObject avatar, AvatarData avatarData)
        {
            Transform currentPivotParent = null;
            GameObject prevRef = null;
            if (FullBodyAvatarReference != null)
            {
                currentPivotParent = FullBodyAvatarReference.transform.parent;
                prevRef = FullBodyAvatarReference;
            }
            FullBodyAvatarReference = avatar;
            FullBodyAvatarReference.transform.SetParent(currentPivotParent, false);
            FullBodyAvatarReference.transform.SetAsFirstSibling();
            FullBodyAvatarReference.name = "Armature";

            foreach (Transform child in FullBodyAvatarReference.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("AvatarWelcomeRoom");
            }

            Animator parentAnimator = FullBodyAvatarReference?.transform.parent?.GetComponent<Animator>();
            if (!parentAnimator)
            {
                parentAnimator = FullBodyAvatarReference?.transform.parent?.gameObject.AddComponent<Animator>();
            }

            parentAnimator.Rebind();

            RuntimeAnimatorController animatorControllerReference;

            if (avatarData.gender == AvatarGender.Feminine)
            {
                animatorControllerReference = feminineAnimatorController;
            }
            else
            {
                animatorControllerReference = masculineAnimatorController;
            }

            if (parentAnimator != null)
            {
                parentAnimator.runtimeAnimatorController = animatorControllerReference;

                parentAnimator.avatar = avatarData.animatorAvatar;
            }


            OnAvatarIstantiated?.Invoke(avatar);

            if (GetComponent<AvatarControllerBase>() == avatarSystem.AvatarInstance)
            {
                SM.GetSystem<CharacterControllerSystem>().OnCharacterControllerSetupComplete.Invoke(GetComponent<AvatarControllerBase>().CharacterReference);

            }

            GetComponent<CharacterBase>().Setup();

            if (prevRef != avatar)
            {
                Destroy(prevRef);
            }

            //onAfterAction?.Invoke();
        }

        public override void EnableAvatarMeshes(bool enable)
        {
            // This call may be done after run closure. This line prevents the error.
            if (!Application.isPlaying)
                return;
            if (FullBodyAvatarReference != null)
            {
                foreach (Renderer rend in FullBodyAvatarReference?.GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = enable;
                }
            }
            base.EnableAvatarMeshes(enable);

        }

    }
}
