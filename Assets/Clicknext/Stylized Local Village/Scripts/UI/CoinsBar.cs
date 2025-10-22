using Clicknext.StylizedLocalVillage.Entities;
using TMPro;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.UI
{
    public class CoinsBar : MonoBehaviour
    {
        [SerializeField] TMP_Text amount;

        private void Awake()
        {
            amount.text = Vault.Coin.ToString();
        }

        private void FixedUpdate()
        {
            amount.text = Vault.Coin.ToString();
        }
    }
}
