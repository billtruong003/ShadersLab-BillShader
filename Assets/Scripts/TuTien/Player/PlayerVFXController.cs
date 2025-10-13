// Assets/Scripts/TuTien/VFX/PlayerVFXController.cs
using UnityEngine;
using Sirenix.OdinInspector;

namespace VoTanTuTien.VFX
{
    [RequireComponent(typeof(Player.PlayerMovement))]
    public class PlayerVFXController : MonoBehaviour
    {
        [Title("VFX References")]
        [SerializeField] private ParticleSystem jumpVFX;
        [SerializeField] private ParticleSystem runVFX;

        [Title("Dependencies")]
        [Required, SerializeField] private Player.PlayerMovement playerMovement;

        private void Update()
        {
            HandleRunVFX();
        }

        public void PlayJumpVFX()
        {
            if (jumpVFX != null)
            {
                jumpVFX.Play();
            }
        }

        private void HandleRunVFX()
        {
            if (runVFX == null) return;

            if (playerMovement.IsMoving() && playerMovement.IsGrounded)
            {
                if (!runVFX.isPlaying)
                {
                    runVFX.Play();
                }
            }
            else
            {
                if (runVFX.isPlaying)
                {
                    runVFX.Stop();
                }
            }
        }
    }
}