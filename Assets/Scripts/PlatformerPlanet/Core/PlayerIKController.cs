using UnityEngine;
using Sirenix.OdinInspector;

namespace PlatformerPlanet
{
    [RequireComponent(typeof(Animator))]
    public class PlayerIKController : MonoBehaviour
    {
        private Animator _animator;

        private bool _useIK = false;
        private float _ikWeight = 0f;

        private Vector3 _rightHandTargetPosition;
        private Vector3 _leftHandTargetPosition;
        private Quaternion _rightHandTargetRotation;
        private Quaternion _leftHandTargetRotation;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_useIK)
            {
                _ikWeight = Mathf.Lerp(_ikWeight, 0f, Time.deltaTime * 10f);
            }
            else
            {
                _ikWeight = Mathf.Lerp(_ikWeight, 1f, Time.deltaTime * 10f);
            }

            SetHandIK(AvatarIKGoal.RightHand, _rightHandTargetPosition, _rightHandTargetRotation);
            SetHandIK(AvatarIKGoal.LeftHand, _leftHandTargetPosition, _leftHandTargetRotation);
        }

        private void SetHandIK(AvatarIKGoal goal, Vector3 position, Quaternion rotation)
        {
            _animator.SetIKPositionWeight(goal, _ikWeight);
            _animator.SetIKRotationWeight(goal, _ikWeight);
            _animator.SetIKPosition(goal, position);
            _animator.SetIKRotation(goal, rotation);
        }

        public void SetHandIKTargets(Vector3 rightHandPos, Quaternion rightHandRot, Vector3 leftHandPos, Quaternion leftHandRot)
        {
            _rightHandTargetPosition = rightHandPos;
            _rightHandTargetRotation = rightHandRot;
            _leftHandTargetPosition = leftHandPos;
            _leftHandTargetRotation = leftHandRot;
            _useIK = true;
        }

        public void ClearIKTargets()
        {
            _useIK = false;
        }
    }
}