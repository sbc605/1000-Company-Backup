using UnityEngine;
using UnityEngine.UI;

// 코드 담당자: 김수아
// 일반 채팅 기능을 구현하고 싶었는데 필요없다면 지워주세요.
public class UI_TextChat : MonoBehaviour
{
    public static UI_TextChat Instance;

    [SerializeField] private Text chatBox;

    private void Awake() => Instance = this;

    public void AddMessage(string sender, string text)
    {
        chatBox.text += $"\n<b>{sender}</b>: {text}";
    }
}