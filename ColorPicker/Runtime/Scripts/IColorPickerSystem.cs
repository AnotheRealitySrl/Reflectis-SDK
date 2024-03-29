using Reflectis.SDK.Core;

using System.Threading.Tasks;

using UnityEngine;

public interface IColorPickerSystem : ISystem
{
    Task AssignColorPicker(GameObject obj, bool networkedContext = true);

    void AssignSavedColorToPicker(Color assignColor, GameObject obj);
}
