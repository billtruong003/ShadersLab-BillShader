using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.UI
{
    public class ThinkingBalloons : MonoBehaviour
    {
        [SerializeField] BalloonTemplate balloonTemplate;

        private void Awake() =>  balloonTemplate.gameObject.SetActive(false);

        public void Create(Item type, Transform attachedPoint)
        {
            var balloonTag = Instantiate(balloonTemplate, transform);
                balloonTag.Spawn(type, attachedPoint);
        }
    }
}
