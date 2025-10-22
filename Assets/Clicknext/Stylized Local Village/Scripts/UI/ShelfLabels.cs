using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using Clicknext.StylizedLocalVillage.Units;
using System.Collections.Generic;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.UI
{
    public class ShelfLabels : MonoBehaviour
    {
        [SerializeField] private LabelTemplate labelTemplate;

        private readonly Dictionary<Item, List<LabelTemplate>> _labels = new();

        private void Awake()
        {
            labelTemplate.gameObject.SetActive(false);

            var shelves = FindObjectsByType<Shelf>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach(var shelf in shelves)
            {
                var label = Instantiate(labelTemplate, labelTemplate.transform.parent);
                    label.name = shelf.name;
                    label.Set(shelf.Product.Type, shelf.transform);
                    label.gameObject.SetActive(false);
                var item = shelf.Product.Type;
                if (!_labels.ContainsKey(item))
                    _labels.Add(item, new List<LabelTemplate>() { label });
                else
                    _labels[item].Add(label);
            }
        }

        public void ShowLabels(Item item, bool isVisible)
        {
            if (_labels.ContainsKey(item))
                foreach (var label in _labels[item])
                    label.Show(isVisible);
        }
    }
}
