using Reflectis.SDK.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Reflectis.SDK.ApplicationManagement
{
    public interface IReflectisApplicationManager
    {
        public static IReflectisApplicationManager Instance;

        #region Events
        public CMEvent CurrentEvent { get; }
        #endregion

        #region Shards
        public CMShard CurrentShard { get; }
        #endregion

        #region Worlds

        List<CMWorld> Worlds { get; }
        CMWorld CurrentWorld { get; }

        #endregion

        #region Permissions
        List<CMPermission> CurrentEventPermissions { get; }
        List<CMPermission> WorldPermissions { get; }
        #endregion

        #region Facets
        public List<CMFacet> WorldFacets { get; }
        #endregion
        public bool ShowFullNickname { get; }

        #region Users
        CMUser UserData { get; }
        #endregion

        public Task CalculateShard();
    }

}
