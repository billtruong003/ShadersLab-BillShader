using UnityEngine;
using Sirenix.OdinInspector;

namespace PlatformerPlanet
{
    [CreateAssetMenu(fileName = "InputBindings", menuName = "PlatformerPlanet/Input Bindings")]
    public class InputBindings : ScriptableObject
    {
        [Title("Movement")]
        public KeyCode MoveRight = KeyCode.D;
        public KeyCode MoveLeft = KeyCode.A;
        public KeyCode Jump = KeyCode.Space;

        [Title("Abilities")]
        public KeyCode Interact = KeyCode.E;
        public KeyCode Teleport = KeyCode.Q;
        public KeyCode ReverseGravity = KeyCode.R;
        public KeyCode Fly = KeyCode.F;
    }
}