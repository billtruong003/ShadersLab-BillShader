using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class ProductTemplate : MonoBehaviour
    {
        public Button SelectButton;
        public Image Icon;
        public bool IsLocked;

        [SerializeField] GameObject lockedImage;

        public void SetLock(bool value)
        {
            IsLocked = value;
            lockedImage.SetActive(value);
        }

    }
}