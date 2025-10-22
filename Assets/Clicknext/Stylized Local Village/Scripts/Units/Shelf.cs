using Clicknext.StylizedLocalVillage.Entities;
using System;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Units
{
    public class Shelf : MonoBehaviour
    {
        public event Action<Collider, Product> OnEnter = delegate { };
        public Product Product => product;

        [SerializeField] Product product;
        [SerializeField] GameObject displayObject;

        private void Awake()
        {
            if(displayObject)
                displayObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other) => OnEnter.Invoke(other, product);
        public void Add(int value) => Storage.Add(product.Type, value);
        public int Remove(int value) 
        {
            var amount = Storage.Remove(product.Type, value);
            return amount;
        }

        private void FixedUpdate()
        {
            if(displayObject)
                displayObject.SetActive(Storage.Get(product.Type) > 0);
        }
    }
}
