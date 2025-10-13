// Assets/Scripts/TuTien/Interfaces/IRewardRecipient.cs
using VoTanTuTien.Core;

namespace VoTanTuTien.Interfaces
{
    public interface IRewardRecipient
    {
        CharacterStats Stats { get; }
        void ReceiveRewards(long linhLuc, long linhNang);
    }
}