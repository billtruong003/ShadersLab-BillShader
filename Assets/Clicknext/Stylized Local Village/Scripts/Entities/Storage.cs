using System;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Entities
{
    public class Storage
    {
        public static event Action OnUpdated = delegate { };

        private static readonly int[] mStorage = new int[Enum.GetValues(typeof(Item)).Length];

        public static void Add(Item type, int value)
        {
            mStorage[(int)type] += value;
            OnUpdated();
        }

        public static int Remove(Item type, int value)
        {
            var amount = mStorage[(int)type] >= value? value: mStorage[(int)type];
            mStorage[(int)type] -= value;
            mStorage[(int)type] = Mathf.Clamp(mStorage[(int)type], 0, int.MaxValue);
            OnUpdated();

            return amount;
        }

        public static int Get(Item type) => mStorage[(int)type];

        public static bool IsEmpty()
        {
            foreach(var item in mStorage)
                if (item > 0)
                    return false;

            return true;    
        }
    }
}
