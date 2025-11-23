using UnityEngine;

namespace Nebulanook.Core
{
    public interface IBumpable
    {
        void OnBump(Vector3 impactDirection, float impactForce);
    }

    [System.Serializable]
    public struct BumpData
    {
        public float force;
        public Vector3 direction;
        public Vector3 contactPoint;
    }
}