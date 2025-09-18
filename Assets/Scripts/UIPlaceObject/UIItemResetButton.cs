using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Button))]
public class UIItemResetButton : MonoBehaviour
{
    [Required("This component needs a reference to its parent DraggableUIItem.")]
    [SceneObjectsOnly]
    [SerializeField, HideLabel, Tooltip("Reference to the parent Draggable UI Item this button controls.")]
    private DraggableUIItem _ownerItem;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnResetButtonClicked);
    }

    [Button("Auto-Find Owner Item"), PropertyOrder(-1)]
    private void FindOwnerInParent()
    {
        _ownerItem = GetComponentInParent<DraggableUIItem>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_ownerItem == null)
        {
            FindOwnerInParent();
        }
    }
#endif

    private void OnResetButtonClicked()
    {
        if (_ownerItem != null)
        {
            _ownerItem.ResetToAvailable();
        }
    }

    private void OnDestroy()
    {
        if (TryGetComponent<Button>(out var button))
        {
            button.onClick.RemoveListener(OnResetButtonClicked);
        }
    }
}