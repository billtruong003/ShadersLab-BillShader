using UnityEngine;

namespace PlatformerPlanet
{
    public interface IPlatformerInput
    {
        float HorizontalInput { get; }
        float VerticalInput { get; } // Thêm cho Bơi và Bay
        bool IsJumpPressed { get; }
        bool IsJumpHeld { get; } // Thêm để xử lý các hành động giữ phím
        bool IsInteractPressed { get; }
        bool IsTeleportPressed { get; }
        bool IsReverseGravityPressed { get; }
        bool IsFlyPressed { get; }

        void ConsumeJumpInput();
        void ConsumeInteractInput();
        void ConsumeTeleportInput();
        void ConsumeReverseGravityInput();
        void ConsumeFlyInput();
    }
}