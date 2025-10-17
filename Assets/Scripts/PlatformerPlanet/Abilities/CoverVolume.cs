using UnityEngine;

namespace PlatformerPlanet
{
    [RequireComponent(typeof(Collider))]
    public class CoverVolume : MonoBehaviour
    {
        private void Awake()
        {
            // Đảm bảo collider luôn là trigger
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlatformerStateMachine>(out var stateMachine))
            {
                stateMachine.EnterCoverZone();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlatformerStateMachine>(out var stateMachine))
            {
                stateMachine.ExitCoverZone();
            }
        }
    }
}