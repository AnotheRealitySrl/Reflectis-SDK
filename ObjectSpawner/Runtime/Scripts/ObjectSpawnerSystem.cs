using Reflectis.SDK.Core;
using Reflectis.SDK.CharacterController;

using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using UnityEngine.InputSystem;

namespace Reflectis.SDK.ObjectSpawner
{
    [CreateAssetMenu(menuName = "Reflectis/SDK-ObjectSpawner/ObjectSpawnerSystemConfig", fileName = "ObjectSpawnerSystemConfig")]

    public class ObjectSpawnerSystem : BaseSystem
    {
        private Transform origin;
        private GameObject npc = null;
        private bool checkOngoing = false;

        public override void Init()
        {
            //Get origin
            origin = SM.GetSystem<CharacterControllerSystem>().CharacterControllerInstance.PivotReference;
        }

        public void CheckEntireFov(SpawnableData data) //(addressable oggetto)
        {
            if (checkOngoing)
                return;

            checkOngoing = true;

            //Calculate how many fov cones I can check with the given values
            int cycles = (360 / data.FovAngle);
            Debug.Log($"Cycles {cycles}");

            float angle = data.StartingAngle - origin.eulerAngles.y;
            for (int i = 0; i < cycles; i++)
            {
                if (IsFovFree(data, angle))
                {
                    Debug.Log("Fov libera, spawna pawn");

                    //Calculate the free point in the space
                    Vector3 freePos = origin.position + data.OriginOffset + GetVectorFromAngle(angle - (data.FovAngle / 2)) * data.ViewDistance;

                    //Instantiate npc
                    if (npc && data.OnlyOneNpc)
                        Destroy(npc);
                    npc = Instantiate(data.ObjPrefab, freePos, Quaternion.identity);
                    npc.transform.LookAt(origin.position + data.OriginOffset);

                    checkOngoing = false;
                    return;
                }
                //Decrease the starting angle by the value of the fov cone
                angle -= data.FovAngle;
            }

            Debug.Log("Nessun punto libero!");
            checkOngoing = false;
        }

        private bool IsFovFree(SpawnableData data, float angle)
        {
            Vector3 v3origin = origin.position + data.OriginOffset;
            float angleIncrease = data.FovAngle / data.RayCount;


            for (int i = 0; i <= data.RayCount; i++)
            {
                RaycastHit raycastHit;
                if (Physics.Raycast(v3origin, GetVectorFromAngle(angle), out raycastHit, data.ViewDistance, data.LayerMask))
                {
                    Debug.Log("hit");
                    Debug.DrawRay(v3origin, raycastHit.point, Color.green, 2.5f);
                    return false;
                }
                Debug.DrawRay(v3origin, GetVectorFromAngle(angle) * data.ViewDistance, Color.green, 2.5f);

                angle -= angleIncrease;
            }
            return true;
        }

        private Vector3 GetVectorFromAngle(float angle)
        {
            float angleRad = angle * (Mathf.PI / 180f);
            return new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
        }
    }

    [Serializable]
    public class SpawnableData
    {
        [SerializeField]
        private GameObject objPrefab;
        [SerializeField]
        private int fovAngle = 90;
        [SerializeField]
        private int rayCount = 2;
        [SerializeField]
        private float viewDistance = 50f;
        [SerializeField]
        private float startingAngle = 0;
        [SerializeField]
        private Vector3 originOffset = Vector3.zero;
        [SerializeField]
        private LayerMask layerMask;
        [SerializeField]
        private bool onlyOneNpc = false;
        [SerializeField]
        private InputActionReference vrInput;
        [SerializeField]
        private InputActionReference desktopInput;


        public GameObject ObjPrefab { get => objPrefab; }
        public int FovAngle { get => fovAngle; }
        public int RayCount { get => rayCount; }
        public float ViewDistance { get => viewDistance; }
        public float StartingAngle { get => startingAngle; }
        public Vector3 OriginOffset { get => originOffset; }
        public LayerMask LayerMask { get => layerMask; }
        public bool OnlyOneNpc { get => onlyOneNpc; }
        public InputActionReference VrInput { get => vrInput; }
        public InputActionReference DesktopInput { get => desktopInput; }
    }
}
