using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIRemoveButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnRemoveButtonClicked);
    }

    private void OnRemoveButtonClicked()
    {
        // Chỉ phát đi sự kiện, không cần biết ai sẽ xử lý.
        PlacementEvents.OnEnterRemovalMode?.Invoke();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnRemoveButtonClicked);
        }
    }
}