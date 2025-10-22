using Clicknext.StylizedLocalVillage.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class BalloonTemplate : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] CanvasGroup canvasGroup;

        private Transform attachedPoint;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
        }

        public void Spawn(Item type, Transform attachedPoint)
        {
            icon.sprite = Resources.Load<Sprite>(type.ToString());
            this.attachedPoint = attachedPoint; 
            gameObject.SetActive(true);
        }

        public void Update()
        {
            if (attachedPoint)
                transform.position = Camera.main.WorldToScreenPoint(attachedPoint.position);
        }

        public void OnAnimationEnd() => Destroy(gameObject);
    }
}
