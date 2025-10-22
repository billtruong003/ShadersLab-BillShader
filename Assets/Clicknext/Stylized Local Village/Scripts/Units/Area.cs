using Clicknext.StylizedLocalVillage.Entities;
using System;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Units
{
    public class Area : MonoBehaviour
    {
        public event Action<Collider, Product[], Product> OnEnter = delegate { };
        public event Action<Collider> OnExit = delegate { };

        [SerializeField] private int max;
        [SerializeField] private Product[] products;
        [SerializeField] private GameObject[] displays;

        public Transform AttachedPoint;
        public int Amount => mAmount;
        public float ProducTime => mProduceTime;
        public Item ProductType => mCurrentProduct.Type;

        private Product mCurrentProduct;
        private float mProduceTime;
        private int mAmount;

        private void Awake()
        {
            if (products.Length > 0)
                ChangeAndGatherProduct(products[0]);
        }

        private void OnTriggerEnter(Collider other) =>
                OnEnter.Invoke(other, products, mCurrentProduct);

        private void OnTriggerExit(Collider other) => OnExit.Invoke(other);

        private void Update()
        {
            if (!mCurrentProduct)
                return;

            if(mAmount >= max)
            {
                mProduceTime = mCurrentProduct.ProduceTime;
                return;
            }

            if (mProduceTime > 0)
            {
                mProduceTime -= Time.deltaTime;
            }
            else
            {
                mProduceTime = mCurrentProduct.ProduceTime;
                mAmount++;
            }
        }

        public int ChangeAndGatherProduct(Product product) 
        {
            mCurrentProduct = product;
            mProduceTime = product.ProduceTime;
            ChangeDisplay();
            return GatherProduct();
        }

        public int GatherProduct()
        {
            var total = mAmount;
            mAmount = 0;
            return total;
        }

        private void ChangeDisplay()
        {
            int selected = 0;
            for(int i = 0; i < products.Length; ++i)
            {
                if ( products[i]!= null && 
                     products[i] == mCurrentProduct)
                {
                    selected = i;
                    break;
                }
            }

            foreach (var display in displays)
                if(display)
                    display.SetActive(false);

            if (displays.Length > selected)
                if (displays[selected])
                    displays[selected].SetActive(true);
        }
    }
}
