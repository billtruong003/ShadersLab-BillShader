using UnityEngine;
using Sirenix.OdinInspector;

namespace PlatformerPlanet
{
    public class PlayerPlatformerInput : MonoBehaviour, IPlatformerInput
    {
        [SerializeField, Required, InlineEditor]
        private InputBindings bindings;

        public float HorizontalInput { get; private set; }
        public float VerticalInput { get; private set; }
        public bool IsJumpPressed { get; private set; }
        public bool IsJumpHeld { get; private set; }
        public bool IsInteractPressed { get; private set; }
        public bool IsTeleportPressed { get; private set; }
        public bool IsReverseGravityPressed { get; private set; }
        public bool IsFlyPressed { get; private set; }

        private bool _isJumpConsumed;
        private bool _isInteractConsumed;
        private bool _isTeleportConsumed;
        private bool _isReverseGravityConsumed;
        private bool _isFlyConsumed;

        private void Update()
        {
            ResetConsumedInputs();
            ProcessInputs();
        }

        private void ResetConsumedInputs()
        {
            if (_isJumpConsumed) IsJumpPressed = false;
            if (_isInteractConsumed) IsInteractPressed = false;
            if (_isTeleportConsumed) IsTeleportPressed = false;
            if (_isReverseGravityConsumed) IsReverseGravityPressed = false;
            if (_isFlyConsumed) IsFlyPressed = false;
        }

        private void ProcessInputs()
        {
            HorizontalInput = (Input.GetKey(bindings.MoveRight) ? 1f : 0f) + (Input.GetKey(bindings.MoveLeft) ? -1f : 0f);
            VerticalInput = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f); // Dùng W/S cho Bơi/Bay

            IsJumpHeld = Input.GetKey(bindings.Jump);

            if (Input.GetKeyDown(bindings.Jump)) { IsJumpPressed = true; _isJumpConsumed = false; }
            if (Input.GetKeyDown(bindings.Interact)) { IsInteractPressed = true; _isInteractConsumed = false; }
            if (Input.GetKeyDown(bindings.Teleport)) { IsTeleportPressed = true; _isTeleportConsumed = false; }
            if (Input.GetKeyDown(bindings.ReverseGravity)) { IsReverseGravityPressed = true; _isReverseGravityConsumed = false; }
            if (Input.GetKeyDown(bindings.Fly)) { IsFlyPressed = true; _isFlyConsumed = false; }
        }

        public void ConsumeJumpInput() { IsJumpPressed = false; _isJumpConsumed = true; }
        public void ConsumeInteractInput() { IsInteractPressed = false; _isInteractConsumed = true; }
        public void ConsumeTeleportInput() { IsTeleportPressed = false; _isTeleportConsumed = true; }
        public void ConsumeReverseGravityInput() { IsReverseGravityPressed = false; _isReverseGravityConsumed = true; }
        public void ConsumeFlyInput() { IsFlyPressed = false; _isFlyConsumed = true; }
    }
}