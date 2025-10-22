using System;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Entities
{
    public static class Market
    {
        static readonly int minPrice = 10;
        static readonly int maxPrice = 100;
        static readonly int minAmount = 1;
        static readonly int maxAmount = 10;

        private static int[] prices;
        private static int[] amounts;

        static Market() => ReGenerate();

        public static void ReGenerate()
        {
            RandomPrices();
            RandomAmounts();
        }

        public static int GetPrice(Item type, int ingredients)
        {
            ingredients = Mathf.Clamp(ingredients, 1, ingredients);
            return prices[(int)type] * ingredients;
        }

        public static int GetAmount(Item type) => amounts[(int)type];

        public static int Remove(Item type, int amount)
        {
            var remain = amounts[(int)type];
            amounts[(int)type] -= amount;
            amounts[(int)type] = Mathf.Clamp(amounts[(int)type], 0, int.MaxValue);
            return amounts[(int)type] == 0 ? remain : amount;
        }

        private static void RandomPrices()
        {
            var length = Enum.GetValues(typeof(Item)).Length;
            prices ??= new int[length];
            for (int i = 0; i < prices.Length; i++)
                prices[i] = UnityEngine.Random.Range(minPrice, maxPrice);
        }

        private static void RandomAmounts()
        {
            var length = Enum.GetValues(typeof(Item)).Length;
            amounts ??= new int[length];
            for (int i = 0; i < amounts.Length; i++) 
                amounts[i] = UnityEngine.Random.Range(0f, 100f) > 50f? 
                    UnityEngine.Random.Range(minAmount,maxAmount): 0;
        }
    }
}
