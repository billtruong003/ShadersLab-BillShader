using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Button))]
public class UIItemResetButton : MonoBehaviour
{
    [Required]
    [SceneObjectsOnly]
    [SerializeField, HideLabel]
    private DraggableUIItem _ownerItem;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnResetButtonClicked);

        if (_ownerItem == null)
        {
            FindOwnerInParent();
        }
    }

    [Button("Auto-Find Owner Item"), PropertyOrder(-1)]
    private void FindOwnerInParent()
    {
        _ownerItem = GetComponentInParent<DraggableUIItem>();
    }

    private void OnResetButtonClicked()
    {
        _ownerItem?.RequestPlacedObjectRemoval();
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnResetButtonClicked);
        }
    }
}