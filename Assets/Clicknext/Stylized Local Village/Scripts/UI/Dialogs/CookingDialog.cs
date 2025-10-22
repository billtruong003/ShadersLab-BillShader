using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Dialogs
{ 
    public class CookingDialog : MonoBehaviour
    {
        [Serializable] 
        private struct Data
        {
            public Toggle toggle;
            public GameObject canvas;
            public float iconSize;
            public Product[] products;
        }

        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject dialog;
        [SerializeField] ProcessTemplate processTempate;
        [SerializeField] Data[] data;

        private Coroutine[] coroutines;

        void Awake()
        {
            coroutines = new Coroutine[Enum.GetValues(typeof(Item)).Length];

            openButton.onClick.AddListener(() => dialog.SetActive(true));
            closeButton.onClick.AddListener(() => dialog.SetActive(false));
            dialog.SetActive(false);
            processTempate.gameObject.SetActive(false);

            foreach (var each in data)
            {
                each.canvas.SetActive(false);
                each.toggle.onValueChanged.AddListener(each.canvas.SetActive);

                foreach (var i in each.products)
                {
                    var process = Instantiate(processTempate, each.canvas.transform);
                        process.Set(i, i.ingredients, each.iconSize);
                        process.OnCraft = () => OnCraft(process, i);
                }
            }

            data[0].toggle.isOn = true;
            data[0].canvas.SetActive(true);
        }

        private void OnCraft(ProcessTemplate process, Product product)
        {
            bool isValid = true;
            foreach(var each in product.ingredients)
            {
                if (each.amount > Storage.Get(each.product.Type))
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid)
            {
                foreach(var each in product.ingredients)
                    Storage.Remove(each.product.Type, each.amount);

                process.CraftAmount++;

                var routineIndex = (int)product.Type;
                if (coroutines[routineIndex] == null)
                    coroutines[routineIndex] = StartCoroutine(Process(process, product));
            }
        }

        private IEnumerator Process(ProcessTemplate process, Product product)
        {
            var routineIndex = (int)product.Type;
            while (process.CraftAmount > 0)
            {
                yield return null;
                process.DurationRemain -= Time.deltaTime;
                if (process.DurationRemain <= 0f)
                {
                    process.DurationRemain = product.ProduceTime;
                    process.CraftAmount--;

                    Storage.Add(product.Type, 1);
                }

                if (process.CraftAmount <= 0)
                    break;
            }

            coroutines[routineIndex] = null;
        }
    }
}
