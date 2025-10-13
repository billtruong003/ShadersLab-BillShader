// Assets/Scripts/TuTien/Interfaces/IAttackable.cs
using UnityEngine;
using VoTanTuTien.Core;

namespace VoTanTuTien.Interfaces
{
    public interface IAttackable
    {
        Transform GetTransform();
        CharacterStats GetStats();
        bool IsDead();
        void ReceiveDamage(float damageAmount, IRewardRecipient source);
    }
}