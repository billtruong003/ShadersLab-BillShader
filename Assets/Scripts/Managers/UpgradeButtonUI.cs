// Path: Assets/Scripts/UI/UpgradeButtonUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class UpgradeButtonUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Button button;
    private UpgradeData assignedUpgrade;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    public void Setup(UpgradeData upgradeData)
    {
        assignedUpgrade = upgradeData;
        iconImage.sprite = upgradeData.Icon;
        titleText.text = upgradeData.Title;
        descriptionText.text = upgradeData.Description;
    }

    private void HandleClick()
    {
        if (assignedUpgrade != null)
        {
            UpgradeManager.Instance.SelectUpgrade(assignedUpgrade);
        }
    }
}