using SPACS.Core;
using SPACS.SDK.CharacterController;

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;

namespace SPACS.SDK.Avatars
{
    public interface IAvatarSystem
    {
        #region Properties

        AvatarControllerBase AvatarInstance { get; }

        #endregion

        #region Unity events

        UnityEvent<IAvatarConfig> AvatarConfigChanged { get; }
        UnityEvent<string> PlayerNickNameChanged { get; }

        #endregion

        #region Avatar lifecycle

        /// <summary>
        /// Updates the avatar instance, i.e. the avatar associated with the character controller.
        /// </summary>
        /// <param name="avatar">The current controller</param>
        /// <returns>Task</returns>
        Task CreateAvatarInstance(AvatarControllerBase avatar);

        /// <summary>
        /// Destroys the current avatar instance
        /// </summary>
        /// <returns>Task</returns>
        Task DestroyAvatarInstance();

        #endregion

        #region Avatar instance customization

        /// <summary>
        /// Updates the configuration of the avatar instances
        /// </summary>
        /// <param name="config">The new configuration of the avatar instance</param>
        /// <param name="onBeforeAction">Called before che configuration update takes place</param>
        /// <param name="onAfterAction">Called after che configuration operation has completed</param>
        void UpdateAvatarInstanceCustomization(IAvatarConfig config);

        /// <summary>
        /// Shows/hides the meshes of the avatar instance
        /// </summary>
        /// <param name="enable"></param>
        void UpdateAvatarInstanceNickName(string newName);

        #endregion

        #region Utility methods

        /// <summary>
        /// Shows/hides avatar's hands (only for half-body avatars)
        /// </summary>
        /// <param name="enable"></param>
        void EnableAvatarInstanceMeshes(bool enable);

        /// <summary>
        /// Shows/hides a specific hand mesh (only for half-body avatars)
        /// </summary>
        /// <param name="id">The hand to update (0 for left, 1 for right)</param>
        /// <param name="enable"></param>
        public void EnableAvatarInstanceHandMeshes(bool enable);

        /// <summary>
        /// Updates the nickname of the avatar instance (usually shown on top of avatar's head)
        /// </summary>
        /// <param name="newName">The new name</param>
        public void EnableAvatarInstanceHandMesh(int id, bool enable);

        #endregion
    }
}
