using Clicknext.StylizedLocalVillage.UI.Templates;
using Clicknext.StylizedLocalVillage.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Dialogs
{
    public class TransactionDialog : MonoBehaviour
    {
        [SerializeField] GameObject dialog;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] int maxRows;
        [SerializeField] TMP_Text rowText;
        [SerializeField] LogTemplate logTemplate;

        private int _rows;

        private void Awake()
        {
            dialog.SetActive(false);
            openButton.onClick.AddListener(() => dialog.SetActive(true));
            closeButton.onClick.AddListener(() => dialog.SetActive(false));

            logTemplate.gameObject.SetActive(false);
            rowText.text = maxRows.ToString();
        }

        private void Log(string iconPath, string detail)
        {
            var root = logTemplate.transform.parent;

            if(_rows >= maxRows)
            {
                var firstRow = root.GetChild(1);
                    Destroy(firstRow.gameObject);
                _rows--;
            }

            var log = Instantiate(logTemplate, root);
                log.Set($"Day{Timer.Day} {Timer.TimeStamp}", iconPath, detail);
                log.gameObject.SetActive(true);
            _rows++;
        }

        public void LogOutofStock(string name) => 
            Log(name, $"{name} is currently out of stock.");

        public void LogSold(string name, int amount, int exp, int coin) =>
            Log(name, $"{name} sold out x{amount}, coin: {coin} EXP Gained: {exp} ");

        public void LogGather(string name, int amount, int exp) =>
            Log(name, $"{name} on the shelf for sales x{amount},  EXP Gained: {exp} ");
    }
}
