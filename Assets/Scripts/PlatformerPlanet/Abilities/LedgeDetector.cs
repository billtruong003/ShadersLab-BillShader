using UnityEngine;

namespace PlatformerPlanet
{
    public class LedgeDetector : MonoBehaviour
    {
        [SerializeField] private PlatformerMotor _motor;
        [SerializeField] private Vector3 _ledgeCheckOffset = new Vector3(0, 1.8f, 0);
        [SerializeField] private float _ledgeCheckDistance = 0.6f;
        [SerializeField] private float _surfaceCheckDistance = 0.6f;

        public bool LedgeDetected { get; private set; }
        public Vector3 LedgePosition { get; private set; }
        public Vector3 SurfacePosition { get; private set; }

        private void Update()
        {
            CheckForLedge();
        }

        private void CheckForLedge()
        {
            Vector3 origin = transform.position + _ledgeCheckOffset;
            Vector3 direction = _motor.IsFacingRight ? Vector3.right : Vector3.left;

            RaycastHit wallHit;
            bool hasHitWall = Physics.Raycast(origin, direction, out wallHit, _ledgeCheckDistance, _motor.Settings.WallLayer);

            LedgeDetected = false;
            if (hasHitWall)
            {
                Vector3 surfaceCheckOrigin = wallHit.point + direction * 0.1f + Vector3.up * _surfaceCheckDistance;
                RaycastHit surfaceHit;
                if (!Physics.Raycast(origin, Vector3.down, _ledgeCheckDistance, _motor.Settings.GroundLayer) &&
                    Physics.Raycast(surfaceCheckOrigin, Vector3.down, out surfaceHit, _surfaceCheckDistance * 2f, _motor.Settings.GroundLayer))
                {
                    LedgeDetected = true;
                    LedgePosition = new Vector3(wallHit.point.x, surfaceHit.point.y, 0);
                    SurfacePosition = surfaceHit.point;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_motor == null) return;

            Vector3 origin = transform.position + _ledgeCheckOffset;
            Vector3 direction = _motor.IsFacingRight ? Vector3.right : Vector3.left;

            Gizmos.color = LedgeDetected ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + direction * _ledgeCheckDistance);

            if (LedgeDetected)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(LedgePosition, 0.1f);
                Gizmos.DrawWireSphere(SurfacePosition, 0.1f);
            }
        }
#endif
    }
}