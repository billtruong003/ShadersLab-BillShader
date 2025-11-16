using UnityEngine;

namespace Nebulanook.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        public bool GetRawInput;
        public Vector2 MoveInput { get; private set; }
        public bool DashInputDown { get; private set; }
        public bool DashInputHeld { get; private set; }
        public bool DashInputUp { get; private set; }

        public bool SprintInputHeld { get; private set; }

        private float horizontal;
        private float vertical;

        private void Update()
        {
            ReadMovementInput();
            ReadDashInput();
            ReadSprintInput();
        }

        private void ReadMovementInput()
        {
            if (GetRawInput)
            {
                GetRawInputAxis();
                return;
            }
            GetSmoothInputAxis();
        }

        private void GetRawInputAxis()
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            MoveInput = new Vector2(horizontal, vertical);
        }

        private void GetSmoothInputAxis()
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
            MoveInput = new Vector2(horizontal, vertical);
        }

        private void ReadDashInput()
        {
            // Sử dụng KeyCode.Space làm ví dụ, có thể thay đổi
            DashInputDown = Input.GetKeyDown(KeyCode.Space);
            DashInputHeld = Input.GetKey(KeyCode.Space);
            DashInputUp = Input.GetKeyUp(KeyCode.Space);
        }

        private void ReadSprintInput()
        {
            SprintInputHeld = Input.GetKey(KeyCode.LeftShift);
        }
    }
}