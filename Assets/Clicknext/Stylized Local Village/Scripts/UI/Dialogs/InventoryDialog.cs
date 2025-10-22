using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Dialogs
{
    public class InventoryDialog : MonoBehaviour
    {
        [SerializeField] GameObject dialog;
        [SerializeField] SlotTemplate slotTemplate;
        [SerializeField] Transform inventoryRoot;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        private SlotTemplate[] mSlots;

        private void Awake()
        {
            openButton.onClick.AddListener(() => dialog.SetActive(true));
            closeButton.onClick.AddListener(() => dialog.SetActive(false));

            dialog.SetActive(false);
            var items = (Item[])Enum.GetValues(typeof(Item));
            mSlots = new SlotTemplate[items.Length];

            for (int i = 0; i < items.Length; ++i)
            {
                var item = items[i];
                var slot = Instantiate(slotTemplate, inventoryRoot);
                    slot.SetName(item.ToString());
                    slot.SetAmount(0);

                mSlots[i] = slot;
            }

            Storage.OnUpdated += OnStorageUpdated;
        }

        private void OnStorageUpdated()
        {
            var items = (Item[])Enum.GetValues(typeof(Item));
            foreach (var item in items)
                mSlots[(int)item].SetAmount(Storage.Get(item));

            slotTemplate.gameObject.SetActive(Storage.IsEmpty());
        }
    }
}
