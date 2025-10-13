namespace VoTanTuTien.Core
{
    public interface IHaveStats
    {
        CharacterStats Stats { get; }
        void InitializeStats();
    }
}