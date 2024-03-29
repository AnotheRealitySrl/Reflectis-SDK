using Reflectis.SDK.Core;
using Reflectis.SDK.CreatorKit;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace Reflectis.SDK.ObjectSpawner
{
    public abstract class ObjectSpawnerController : MonoBehaviour, IRuntimeComponent
    {
        public SpawnableData spawnableData;

        protected InputActionReference inputActionReference;

        public Task Init(SceneComponentPlaceholderBase placeholder)
        {
            var objSpawnerPlaceholder = (placeholder as ObjectSpawnerPlaceholder);

            if (objSpawnerPlaceholder == null)
                return Task.CompletedTask;

            if (objSpawnerPlaceholder.Data != null)
            {
                spawnableData = objSpawnerPlaceholder.Data;
            }

            inputActionReference = GetInputActionReference(objSpawnerPlaceholder);

            InputAction inputAction = inputActionReference != null ? inputActionReference.action : null;
            if (inputAction != null)
            {
                RegisterActionCallback(inputAction);
            }

            return Task.CompletedTask;
        }

        protected virtual void RegisterActionCallback(InputAction action)
        {
            action.Enable();
            action.performed += ButtonActionCallback;
        }

        protected virtual void DeregisterActionCallback(InputAction action)
        {
            action.performed -= ButtonActionCallback;
            action.Disable();
        }

        protected async void ButtonActionCallback(CallbackContext context)
        {
            if (context.action.enabled)
            {
                await SpawnObj();
            }
        }

        public async Task<SpawnedObject> SpawnObj()
        {
            GameObject spawned = SM.GetSystem<ObjectSpawnerSystem>().CheckEntireFovAndSpawn(spawnableData);
            if (spawned == null)
            {
                return null;
            }
            var spawnedObject = spawned.AddComponent<SpawnedObject>();
            SceneComponentsMapper mapper = await Addressables.LoadAssetAsync<SceneComponentsMapper>("LearningSpaceComponentsMapper").Task;

            spawnedObject.isAlive = true;

            spawnedObject.objectSpawnerManager = this;
            foreach (SceneComponentPlaceholderBase placeholder in spawned.GetComponentsInChildren<SceneComponentPlaceholderBase>(true))
            {
                await placeholder.Init(mapper);
            }
            return spawnedObject;
        }

        protected abstract InputActionReference GetInputActionReference(ObjectSpawnerPlaceholder placeholder);

        private void OnDestroy()
        {
            if (inputActionReference != null)
            {
                DeregisterActionCallback(inputActionReference.action);
            }
        }

    }
}
