using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Characters
{
    public class Truck : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject body;

        private bool isWholesale;

        private void Awake()
        {
            body.SetActive(false);
        }

        public void Transport()
        {
            if (isWholesale)
                return;

            isWholesale = true;
            animator.SetTrigger("sell");
        }

        public void SetWholesale() => isWholesale = false;
    }
}
