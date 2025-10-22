using System;

namespace Clicknext.StylizedLocalVillage.Entities
{
    public class Vault
    {
        public static event Action OnExpChanged = delegate{};
        public static int Coin = 100;
        public static int EXP { set { exp = value; OnExpChanged(); } get { return exp; } }
        private static int exp;

        public static int Level = 1;
    }
}
