// Path: Assets/Scripts/UI/UpgradeChoiceUI.cs
using UnityEngine;
using System.Collections.Generic;

public class UpgradeChoiceUI : MonoBehaviour
{
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private UpgradeButtonUI upgradeButtonPrefab;

    private readonly List<GameObject> activeButtons = new List<GameObject>();

    public void DisplayChoices(List<UpgradeData> choices)
    {
        ClearExistingButtons();

        foreach (var choice in choices)
        {
            UpgradeButtonUI buttonInstance = Instantiate(upgradeButtonPrefab, buttonsContainer);
            buttonInstance.Setup(choice);
            activeButtons.Add(buttonInstance.gameObject);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ClearExistingButtons()
    {
        foreach (var button in activeButtons)
        {
            Destroy(button);
        }
        activeButtons.Clear();
    }
}