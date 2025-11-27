using UnityEngine;
using Sirenix.OdinInspector;

namespace Nebulanook.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Title("Movement Keys")]
        [SerializeField] private KeyCode moveForwardKey = KeyCode.W;
        [SerializeField] private KeyCode moveBackwardKey = KeyCode.S;
        [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
        [SerializeField] private KeyCode moveRightKey = KeyCode.D;

        [Title("Action Keys")]
        [SerializeField] private KeyCode dashKey = KeyCode.Space;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode interactKey = KeyCode.E; // Key mới

        public Vector2 MoveInput { get; private set; }
        public bool DashInputDown { get; private set; }
        public bool DashInputHeld { get; private set; }
        public bool DashInputUp { get; private set; }
        public bool SprintInputHeld { get; private set; }
        public bool InteractInputDown { get; private set; } // Property mới

        private bool isInputActive = true;

        public void SetInputActive(bool active)
        {
            isInputActive = active;
            if (!active) ResetInputs();
        }

        private void Update()
        {
            if (!isInputActive) return;

            float y = 0f;
            float x = 0f;

            if (Input.GetKey(moveForwardKey)) y += 1f;
            if (Input.GetKey(moveBackwardKey)) y -= 1f;
            if (Input.GetKey(moveRightKey)) x += 1f;
            if (Input.GetKey(moveLeftKey)) x -= 1f;

            MoveInput = new Vector2(x, y).normalized;

            DashInputDown = Input.GetKeyDown(dashKey);
            DashInputHeld = Input.GetKey(dashKey);
            DashInputUp = Input.GetKeyUp(dashKey);
            SprintInputHeld = Input.GetKey(sprintKey);
            InteractInputDown = Input.GetKeyDown(interactKey); // Logic mới
        }

        private void ResetInputs()
        {
            MoveInput = Vector2.zero;
            DashInputDown = false;
            DashInputHeld = false;
            DashInputUp = false;
            SprintInputHeld = false;
            InteractInputDown = false;
        }
    }
}