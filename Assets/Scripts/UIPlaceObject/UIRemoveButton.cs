using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Button))]
public class UIRemoveButton : MonoBehaviour
{
    [Title("Dependencies")]
    [Required("A PlacementManager must be assigned.")]
    [SerializeField] private PlacementManager placementManager;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnRemoveButtonClicked);
    }

    private void OnRemoveButtonClicked()
    {
        if (placementManager == null) return;

        placementManager.EnterRemovalMode();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnRemoveButtonClicked);
        }
    }
}