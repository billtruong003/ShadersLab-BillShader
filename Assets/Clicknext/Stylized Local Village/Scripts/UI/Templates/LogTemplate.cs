using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates 
{
    public class LogTemplate : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text timeText;
        [SerializeField] TMP_Text detailText;
        
        public void Set(string timeStamp, string iconPath, string detail) 
        {
            timeText.text = timeStamp;
            detailText.text = detail;

            var icon = Resources.Load<Sprite>(iconPath);
            iconImage.sprite = icon;
        }
    }
}
