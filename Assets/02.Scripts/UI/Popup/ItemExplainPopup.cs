using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
// Explain 프리팹에 할당되는 스크립트
public class ItemExplainPopup : ItemPopupBase
{
    [SerializeField] protected TextMeshProUGUI itemNameText;
    [SerializeField] protected Image icon;
    [SerializeField] protected TextMeshProUGUI descriptionText;

    [Header("Text Option")]
    [SerializeField] private string[] breakTokens;
    [SerializeField] private float lineSpacing = 4f;
    [SerializeField] private float paragraphSpacing = 8f;

    public override void Initialize()
    {
        icon.sprite = itemData.icon;
        itemNameText.text = itemData.itemName;

        string format = ItemTextFormatter.Format(itemData.description, breakTokens, lineSpacing, paragraphSpacing, descriptionText);
        descriptionText.text = format;
    }
}
