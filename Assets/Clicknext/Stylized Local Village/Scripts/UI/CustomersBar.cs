using Clicknext.StylizedLocalVillage.Characters;
using TMPro;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.UI
{
    public class CustomersBar : MonoBehaviour
    {
        [SerializeField] TMP_Text amount;

        private int counter;
        private Customer[] customers;

        private void Awake() => customers = FindObjectsOfType<Customer>(includeInactive: true);

        private void OnEnable()
        {
            foreach (Customer customer in customers)
            {
                customer.OnSpawn += Increase;
                customer.OnUnspawn += Decrease;
            }
        }

        private void OnDisable()
        {
            foreach (Customer customer in customers)
            {
                customer.OnSpawn -= Increase;
                customer.OnUnspawn -= Decrease;
            }
        }

        private void Decrease()
        {
            counter--;
            counter = Mathf.Clamp(counter, 0, int.MaxValue);
        }

        private void Increase()
        {
            counter++;
            counter = Mathf.Clamp(counter, 0, int.MaxValue);
        }

        private void FixedUpdate() => amount.text = counter.ToString();
    }
}
