// Assets/Scripts/TuTien/Player/PlayerCharacter.cs
using UnityEngine;
using VoTanTuTien.Core;
using VoTanTuTien.Interfaces;
using System; // Thêm namespace này

namespace VoTanTuTien.Player
{
    public abstract class PlayerCharacter : MonoBehaviour, IHaveStats, IRewardRecipient
    {
        public static event Action<PlayerCharacter> OnPlayerInitialized; // Event mới

        [SerializeField]
        private CharacterStats characterStatsTemplate;

        public CharacterStats Stats { get; private set; }

        protected virtual void Awake()
        {
            InitializeStats();
        }

        private void Start() // Dùng Start để đảm bảo các script khác đã Awake
        {
            OnPlayerInitialized?.Invoke(this); // Phát tín hiệu
        }

        public void InitializeStats()
        {
            Stats = Instantiate(characterStatsTemplate);
            Stats.InitializeRuntimeValues();
        }

        public void ReceiveRewards(long linhLuc, long linhNang)
        {
            Stats.AddLinhLuc(linhLuc);
            Stats.AddLinhNang(linhNang);
        }
    }
}