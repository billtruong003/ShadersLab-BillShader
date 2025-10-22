using Clicknext.StylizedLocalVillage.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class LabelTemplate : MonoBehaviour
    {
        [SerializeField] Image icon;

        private Transform _target;

        public void Set(Item item, Transform target)
        {
            icon.sprite = Resources.Load<Sprite>(item.ToString());

            _target = target;
            transform.position = Camera.main.WorldToScreenPoint(_target.position);
        }

        public void Show(bool isVisible)
        {
            transform.position = Camera.main.WorldToScreenPoint(_target.position);
            gameObject.SetActive(isVisible);
        }

        private void Update()
        {
            if (!_target)
                return;

            transform.position = Camera.main.WorldToScreenPoint(_target.position);
        }
    }
}
