using SPACS.Core;
using SPACS.SDK.CharacterController;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPACS.SDK.Avatars
{
    public abstract class AvatarConfigControllerBase : MonoBehaviour, IAvatarConfigController
    {
        #region Inspector variables

        [Header("Avatar elements")]

        [SerializeField, Tooltip("Sorted list of avatar loaders")]
        private AvatarLoadersController avatarLoadersController;

        [SerializeField] protected GameObject fullBodyAvatarReference;

        

        #endregion

        #region Protected variables

        protected AvatarSystem avatarSystem;
        protected ICharacter character;

        protected readonly List<Renderer> handsMeshes = new();
        protected TMP_Text avatarNameText;
        private AvatarLoaderBase avatarLoader;
        #endregion

        #region Properties
        public IAvatarConfig AvatarConfig { get; private set; }

        public AvatarLoaderBase AvatarLoader { get => avatarLoader; set => avatarLoader = value; }
        #endregion

        #region Unity events

        public UnityEvent OnBeforeInstantiation { get; } = new();
        public UnityEvent<GameObject> OnAvatarIstantiated { get; } = new();
        
        protected AvatarLoadersController AvatarLoadersController { get => avatarLoadersController; set => avatarLoadersController = value; }
        

        #endregion

        #region Unity callbacks

        protected virtual void Awake()
        {
            avatarSystem = SM.GetSystem<AvatarSystem>();
            character = GetComponent<ICharacter>();

            AddToHandMeshes(character.LeftInteractorReference);
            AddToHandMeshes(character.RightInteractorReference);

            if (character.LabelReference)
            {
                avatarNameText = character.LabelReference.GetComponentInChildren<TMP_Text>();
            }
        }

        #endregion

        #region Public methods

        public async virtual Task UpdateAvatarCustomization(IAvatarConfig config, Action onBeforeAction = null, Action onAfterAction = null)
        {
            onBeforeAction?.Invoke();

            AvatarLoader = AvatarLoadersController.GetAvatarLoader(config);

            AvatarLoader.onLoadingAvatarComplete.AddListener(OnAvatarLoadCompletion);

            AvatarLoader.onLoadingAvatarComplete.AddListener((_) => { onAfterAction?.Invoke(); AvatarLoader.onLoadingAvatarComplete.RemoveAllListeners();} );

            await AvatarLoader.LoadAvatar(config);

            onAfterAction?.Invoke();

        }

        public abstract void OnAvatarLoadCompletion(GameObject avatar);

        public virtual void EnableAvatarMeshes(bool enable)
        {
            if (character.LabelReference)
            {
                character.LabelReference.gameObject.SetActive(enable);
            }
        }

        public virtual void EnableHandMeshes(bool enable)
        {
            EnableHandMesh(0, enable);
            EnableHandMesh(1, enable);
        }

        public virtual void EnableHandMesh(int id, bool enable)
        {
            handsMeshes[id].enabled = enable;
        }


        public virtual void UpdateAvatarNickName(string newName)
        {
            if (avatarNameText)
            {
                avatarNameText.text = newName;
                avatarNameText.gameObject.SetActive(false);
                StartCoroutine(UpdateTextTransform());

                IEnumerator UpdateTextTransform()
                {
                    yield return new WaitForEndOfFrame();
                    avatarNameText.gameObject.SetActive(true);
                    avatarNameText.GetComponent<RectTransform>().ForceUpdateRectTransforms();
                }
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void HideMeshesToPlayer()
        {
            int layerHiddenToPlayer = LayerMask.NameToLayer(avatarSystem.LayerNameHiddenToPlayer);

            foreach (var toHide in fullBodyAvatarReference.GetComponentsInChildren<Transform>())
            {
                toHide.gameObject.layer = layerHiddenToPlayer;
            }
            if (character.LabelReference)
            {
                character.LabelReference.gameObject.layer = layerHiddenToPlayer;
            }
        }

        protected void AddToHandMeshes(Transform transform)
        {
            if (transform != null)
            {
                Renderer renderer = transform.GetComponentInChildren<Renderer>();
                if(renderer != null)
                {
                    handsMeshes.Add(renderer);
                    Debug.Log(renderer.gameObject.name + " renderer ", renderer.gameObject);
                }
                
            }
        } 
        #endregion
    }
}
