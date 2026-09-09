using Virtuademy.SDK.Core.Utilities;

using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;

namespace Virtuademy.SDK.Core.SystemFramework
{

    /// <summary>
    /// System manager instance.
    /// </summary>
    public class SM
    {
        #region Readiness

        public static UnityEvent OnAllSystemsSetupsDone = new UnityEvent();

        public static bool IsReady { get; private set; } = false;

        public static void DoOnceWhenReady(UnityAction callback)
        {
            if (callback == null)
            {
                Debug.LogWarning("[SM] Trying to execute null callback!");
                return;
            }

            // In case of valid callback, manage its execution.
            if (IsReady)
            {
                callback.Invoke();
            }
            else
            {
                OnAllSystemsSetupsDone.AddListenerOnce(callback);
            }
        }

        #endregion


        #region Systems

        public static List<ISystem> CurrentSystems { get; set; } = new();

        /// <summary>
        /// Instantiated (if required) and initializes a list of systems
        /// </summary>
        /// <param name="systems"></param>
        public static async void LoadAndSetup(List<BaseSystem> systems)
        {
            IsReady = false;

            CurrentSystems = new List<ISystem>();
            for (int i = 0; i < systems.Count; i++)
            {
                BaseSystem system = systems[i];
                if (system != null)
                {

                    BaseSystem systemInstance = system.RequiresNewInstance ? ScriptableObject.Instantiate(system) : system;
                    CurrentSystems.Add(systemInstance);
                    if (system.AutoInitAtStartup)
                    {
                        _ = await InitSystem(systemInstance, null);
                    }
                }
                else
                {
                    // Warning! The system in position [i] is not valid!
                    Debug.LogWarning($"[SystemManager] System not valid in SystemManagerController, index [{i}].");
                }
            }

            //while (CurrentSystems.Exists(x => x.AutoInitAtStartup && !x.IsInit))
            //{
            //    Debug.Log($"System {string.Join("", CurrentSystems.Where(x => x.AutoInitAtStartup && !x.IsInit).Select(x => "|" + x.ToString() + "|").ToList())} not initialized yet");
            //    await Task.Yield();
            //}

            IsReady = true;

            OnAllSystemsSetupsDone?.Invoke();
        }

        /// <summary>
        /// Initializes a system
        /// </summary>
        /// <param name="systemToInitialize"></param>
        /// <param name="parentSystem"></param>
        /// <returns></returns>
        private static async Task<ISystem> InitSystem(ISystem systemToInitialize, ISystem parentSystem)
        {
            await systemToInitialize.InitInternal(parentSystem);
            foreach (ISystem subSystem in systemToInitialize.SubSystems)
            {
                //if (subSystem.AutoInitAtStartup)
                //{
                if (subSystem == null)
                {
                    Debug.LogError("Found null subsystem inside system: " + systemToInitialize, systemToInitialize as Object);
                    continue;
                }
                BaseSystem systemInstance = subSystem.RequiresNewInstance ? ScriptableObject.Instantiate(subSystem as BaseSystem) : subSystem as BaseSystem;
                CurrentSystems.Add(systemInstance);
                _ = await InitSystem(systemInstance, systemToInitialize);
                //}
            }
            return systemToInitialize;
        }

        /// <summary>
        /// Get an instantiated system of type T/>
        /// </summary>
        /// <remarks>
        /// <b>The constraint is <c>class</c> and not <c>ISystem</c> on purpose.</b> The lookup has
        /// always been "the registered instance assignable to T", and an instance is assignable to a
        /// contract whether or not that contract happens to derive from <see cref="ISystem"/>. The
        /// authoring package's contracts deliberately do not: a world is authored against interfaces
        /// that say what a thing does, not against the framework that hosts them. Requiring
        /// <c>ISystem</c> here would have forced the framework back into every one of them.
        /// <para>
        /// The looser constraint means <c>GetSystem&lt;Something&gt;()</c> for a type nothing
        /// registers now compiles and returns null, where it used to be a compile error. That is the
        /// price, and it is the same failure a wrong-but-legal type argument always had.
        /// </para>
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetSystem<T>() where T : class
        {
            ISystem returnSystem = CurrentSystems.Find(s => s.GetType() == typeof(T) || typeof(T).IsAssignableFrom(s.GetType()));
            return returnSystem as T;
        }
        /// <summary>
        /// Get an instantiated system of type T/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ISystem GetSystem(ISystem system)
        {
            ISystem returnSystem = CurrentSystems.Find(s => s.GetType() == system.GetType() || system.GetType().IsAssignableFrom(s.GetType()));
            return returnSystem;
        }

        #endregion
    }

}
