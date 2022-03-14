using System;
using UnityEngine;

namespace SPACS.Extra.Runtime
{
    /// <summary>
    /// Component that allows to apply a Billboard behaviour to its owner.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [Flags]
        public enum EAxis
        {
            None = 0,
            X = 2,
            Y = 4,
            Z = 8,
        }

        private struct LerpData
        {
            public Quaternion rotation;
            public float time;
        }


        [SerializeField]
        private new Camera camera = default;

        [SerializeField]
        private float minDistance = 0.0f;

        [SerializeField]
        private float speed = 1.0f;

        [SerializeField]
        private EAxis axesToRotate = EAxis.X | EAxis.Y | EAxis.Z;

        [NonSerialized]
        private float sqrMinDistance = 0.0f;

        [NonSerialized]
        private Quaternion originalOrientation = default;

        [NonSerialized]
        private LerpData? startLerpData = null;


        ///////////////////////////////////////////////////////////////////////////
        private void Awake()
        {
            sqrMinDistance = minDistance * minDistance;
            originalOrientation = transform.localRotation;

            if (camera == null)
                camera = Camera.main;
        }

        ///////////////////////////////////////////////////////////////////////////
        private void LateUpdate()
        {
            Vector3 camPosition = camera.transform.position;

            float sqrCameraDistance = Vector3.SqrMagnitude(transform.position - camPosition);
            if (sqrCameraDistance > sqrMinDistance)
            {
                Vector3 newRotEuler = Quaternion.LookRotation(transform.position - camPosition).eulerAngles;

                if (!axesToRotate.HasFlag(EAxis.X)) { newRotEuler.x = transform.rotation.eulerAngles.x; }
                if (!axesToRotate.HasFlag(EAxis.Y)) { newRotEuler.y = transform.rotation.eulerAngles.y; }
                if (!axesToRotate.HasFlag(EAxis.Z)) { newRotEuler.z = transform.rotation.eulerAngles.z; }

                transform.rotation = Quaternion.Euler(newRotEuler);
                startLerpData = null;
            }
            else
            {
                if (!startLerpData.HasValue)
                {
                    startLerpData = new LerpData()
                    {
                        rotation = transform.localRotation,
                        time = Time.time
                    };
                }

                LerpData lerpData = startLerpData.Value;
                float lerpT = (Time.time - lerpData.time) * speed;
                transform.localRotation = Quaternion.Lerp(lerpData.rotation, originalOrientation, lerpT);
            }
        }
    }
}
