using UnityEngine;

public interface IInteractable
{
    string GetInteractionPrompt(ActiveToolSystem toolSystem);
    bool Interact(GameObject interactor, ActiveToolSystem toolSystem);
}