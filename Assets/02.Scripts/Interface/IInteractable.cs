//코드 담당자: 유호정
using UnityEngine;

public interface IInteractable
{
    void Interact(GameObject interactor);
    
    void EnableOutline();
    void DisableOutline();
    // string GetInteractText();
}