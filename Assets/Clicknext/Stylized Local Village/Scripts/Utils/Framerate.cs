using TMPro;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Utils
{
    public class Framerate : MonoBehaviour
    {
        [SerializeField] TMP_Text framerateText;

        private int _currentFramerate;
        private float _time;

        void Awake() => Application.targetFrameRate = 60;

        private void Update()
        {
            if (_time >= 1f)
            {
                framerateText.text = _currentFramerate.ToString();
                _time = 0f;
                _currentFramerate = 0;
            }
            else
            {
                _time += Time.deltaTime;
                _currentFramerate++;
            }
        }
    }
}
