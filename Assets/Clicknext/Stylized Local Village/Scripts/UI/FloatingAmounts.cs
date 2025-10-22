using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.UI
{
    public class FloatingAmounts : MonoBehaviour
    {
        [SerializeField] FloatingTemplate floatingTemplate;

        private void Awake() => floatingTemplate.gameObject.SetActive(false);

        public void Create(Item type, int value, Vector3 position)
        {
            var amountTag = Instantiate(floatingTemplate, transform);
                amountTag.Spawn(type, value, position);
        }

        public void Create(Currency currency, int value, Vector3 position)
        {
            var amountTag = Instantiate(floatingTemplate, transform);
                amountTag.Spawn(currency, value, position);
        }
    }
}
