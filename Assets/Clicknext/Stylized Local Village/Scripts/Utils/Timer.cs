using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Utils
{
    public class Timer : MonoBehaviour
    {
        public static event Action OnChanged = delegate { };
        public static int Day = 1;
        public static string TimeStamp;

        [SerializeField] TMP_Text dayText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] Color dayColor;
        [SerializeField] Color nightColor;
        [SerializeField] GameObject dayIcon;
        [SerializeField] GameObject nightIcon;

        private int minute;
        private int hour;
        private int shift;

        private void Start () => StartCoroutine(WatchStop());

        private IEnumerator WatchStop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.01f);
                minute++;

                if (minute >= 60)
                {
                    minute = 0;
                    hour++;
                }

                if (hour >= 12)
                {
                    hour = 0;
                    shift++;
                }

                if (shift >= 2)
                {
                    shift = 0;
                    Day++;
                    OnChanged();
                }

                var sstr = shift == 0 ? "PM" : "AM";
                var hstr = hour == 0 ? "12" : hour.ToString();
                TimeStamp = $"{hstr}:{minute:00} {sstr}";
                dayText.text = Day.ToString();
                timeText.text = TimeStamp.ToString();

                bool isDay = (shift == 1 && (hour >= 0 && hour <= 6) ||
                    (shift == 0 && (hour >= 6 && hour <= 12)));
                timeText.color = isDay? dayColor: nightColor;
                dayIcon.SetActive(isDay);
                nightIcon.SetActive(!isDay);
            }
        }
    }
}
