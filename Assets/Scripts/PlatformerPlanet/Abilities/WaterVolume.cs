using UnityEngine;

namespace PlatformerPlanet
{
    [RequireComponent(typeof(Collider))]
    public class WaterVolume : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlatformerStateMachine>(out var stateMachine))
            {
                stateMachine.EnterWater();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlatformerStateMachine>(out var stateMachine))
            {
                stateMachine.ExitWater();
            }
        }
    }
}