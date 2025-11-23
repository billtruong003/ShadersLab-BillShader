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

        public Vector2 MoveInput { get; private set; }
        public bool DashInputDown { get; private set; }
        public bool DashInputHeld { get; private set; }
        public bool DashInputUp { get; private set; }
        public bool SprintInputHeld { get; private set; }

        private void Update()
        {
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
        }
    }
}