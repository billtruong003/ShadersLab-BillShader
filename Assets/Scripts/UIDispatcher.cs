// File: UIDispatcher.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDispatcher : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private PlaceableItemData itemToPlace;

    public void OnPointerDown(PointerEventData eventData)
    {
        SystemEvents.RaisePlacementRequested(itemToPlace);
    }
}