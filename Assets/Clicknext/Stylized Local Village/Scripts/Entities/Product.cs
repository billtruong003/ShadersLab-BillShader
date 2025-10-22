using System;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Entities
{
    [Serializable]
    [CreateAssetMenu(fileName = "Product", menuName = "ScriptableObjects/Product", order = 1)]
    public class Product : ScriptableObject
    {
        public Item Type;
        public float ProduceTime;
        public bool isLocked;
        public Ingredient[] upgradeItems;
        public Ingredient[] ingredients;

        public int IngredientCount
        { 
            get 
            {
                int count = 0;
                foreach(var ingredient in ingredients)
                    count += ingredient.amount;

                return count;
            }
        }
    }

    [Serializable]
    public struct Ingredient
    {
        public Product product;
        public int amount;
    }
}

