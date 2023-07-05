using Sirenix.OdinInspector;

using Reflectis.SDK.Core;
using Reflectis.SDK.CharacterController;

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Reflectis.SDK.Avatars
{
    [CreateAssetMenu(menuName = "Reflectis/SDK-Avatars/AvatarSystem", fileName = "AvatarSystemConfig")]
    public class AvatarSystem : BaseSystem, IAvatarSystem
    {
        #region Inspector variables

        [Header("Initialization")]
        [SerializeField, Tooltip("Create an avatar instance on system init")]
        private bool createAvatarInstanceOnInit = true;

        [SerializeField, Tooltip("Is the avatar already in scene or should be instantiated from a prefab?")]
        private bool avatarAlreadyInScene;

        [Header("Avatar instantiation")]
#if ODIN_INSPECTOR
        [HideIf(nameof(avatarAlreadyInScene))]
#endif
        [SerializeField, Tooltip("Reference to the avatar prefab")]
        private AvatarControllerBase avatarPrefab;

        [Header("General settings")]
        [SerializeField, Tooltip("If true, once a new avatar instance is created, the method Setup of its SourceCharacter is called")]
        private bool setupAvatarInstanceAutomatically;

        [SerializeField, Tooltip("Objects in this layer are hidden to the player (useful for VR avatars)")]
        private string layerNameHiddenToPlayer = "HiddenToPlayer";

        [SerializeField, Tooltip("Default masculine animation avatar")]
        private Avatar defaultMasculineAvatar;

        [SerializeField, Tooltip("Default feminine animation avatar")] 
        private Avatar defaultFeminineAvatar;

        [SerializeField, Tooltip("Default height for feminine players")] 
        private float defaultFeminineHeight = 1.60f;

        [SerializeField, Tooltip("Default height for anonymous players")]
        private float defaultHeight = 1.65f;

        [SerializeField, Tooltip("Default height for masculine players")] 
        private float defaultMasculineHeight = 1.70f;

        [SerializeField, Tooltip("Default animator for masculine avatars")]
        private RuntimeAnimatorController masculineAnimatorController;

        [SerializeField, Tooltip("Default animator for feminine avatars")]
        private RuntimeAnimatorController feminineAnimatorController;

        
        #endregion

        #region Properties

        public AvatarControllerBase AvatarInstance { get; private set; }
        public string LayerNameHiddenToPlayer => layerNameHiddenToPlayer;
        public Avatar DefaultMasculineAvatar { get => defaultMasculineAvatar; }
        public Avatar DefaultFeminineAvatar { get => defaultFeminineAvatar; }
        public float DefaultFeminineHeight { get => defaultFeminineHeight; }
        public float DefaultHeight { get => defaultHeight; }
        public float DefaultMasculineHeight { get => defaultMasculineHeight; }
        public RuntimeAnimatorController FeminineAnimatorController { get => feminineAnimatorController; }
        public RuntimeAnimatorController MasculineAnimatorController { get => masculineAnimatorController; }

        #endregion

        #region Unity events

        public UnityEvent<IAvatarConfig> AvatarConfigChanged { get; } = new();
        public UnityEvent<string> PlayerNickNameChanged { get; } = new();


        #endregion

        #region Private variables

        // The config manager associated to the avatar instance
        private IAvatarConfigController avatarInstanceConfigManager;

        #endregion

        #region System implementation

        public override async void Init()
        {
            if (createAvatarInstanceOnInit)
            {
                if (avatarAlreadyInScene)
                {
                    if (FindObjectOfType<AvatarControllerBase>() is AvatarControllerBase avatarController)
                    {
                        await CreateAvatarInstance(avatarController);
                    }
                    else
                    {
                        throw new System.Exception("Avatar not found in scene");
                    }
                }
                else
                {
                    if (avatarPrefab)
                    {
                        await CreateAvatarInstance(avatarPrefab);
                    }
                    else
                    {
                        throw new System.Exception("Avatar prefab not specified");
                    }
                }
            }

            AvatarConfigChanged.AddListener(UpdateAvatarInstanceCustomization);
            PlayerNickNameChanged.AddListener(UpdateAvatarInstanceNickName);
        }

        #endregion

        #region Public API

        public async Task CreateAvatarInstance(AvatarControllerBase avatar)
        {
            // Destroys the old avatar instance
            if (AvatarInstance)
            {
                await DestroyAvatarInstance();
            }

            CharacterControllerSystem ccs = SM.GetSystem<CharacterControllerSystem>();

            // Checks if the referenced avatar is already in scene
            AvatarInstance = string.IsNullOrEmpty(avatar.gameObject.scene.name)
                ? Instantiate(avatar).GetComponent<AvatarControllerBase>()
                : avatar;
            // Attaches the new avatar instance to the character controller instance
            await AvatarInstance.Setup(ccs.CharacterControllerInstance);

            avatarInstanceConfigManager = AvatarInstance.GetComponent<IAvatarConfigController>();

            if (setupAvatarInstanceAutomatically)
            {
                await AvatarInstance.CharacterReference.Setup();
            }
        }

        public async Task DestroyAvatarInstance()
        {
            if (AvatarInstance)
            {
                await AvatarInstance.Unsetup();
            }

            AvatarInstance = null;
            avatarInstanceConfigManager = null;
        }

        public void UpdateAvatarInstanceCustomization(IAvatarConfig config) => avatarInstanceConfigManager?.UpdateAvatarCustomization(config);
        public void UpdateAvatarInstanceNickName(string newName) => avatarInstanceConfigManager?.UpdateAvatarNickName(newName);
        public void EnableAvatarInstanceMeshes(bool enable) => avatarInstanceConfigManager?.EnableAvatarMeshes(enable);
        public void EnableAvatarInstanceHandMeshes(bool enable) => avatarInstanceConfigManager?.EnableHandMeshes(enable);
        public void EnableAvatarInstanceHandMesh(int id, bool enable) => avatarInstanceConfigManager?.EnableHandMesh(id, enable);

        #endregion
    }
}
