using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
public class ItemPopupBase : MonoBehaviour
{
    protected ItemData itemData;

    public virtual void Initialize() { }
    public void Show(ItemData item)
    {
        gameObject.SetActive(true);
        itemData = item;
        Initialize();
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
