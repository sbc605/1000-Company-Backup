//코드 담당자: 최은주
//태블릿에 결과 보고 제출란

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubmitResultUIController : MonoBehaviour
{
    [SerializeField] GameObject submitPanel;
    [SerializeField] GameObject gobackPanel;
    [SerializeField] TextMeshProUGUI ghostType;

    [SerializeField] Button yesBtn;
    [SerializeField] Button noBtn;

    [Header("참조 스크립트")]
    [SerializeField] GhostGuessDataUIController ggdc;
    TutorialManager tm;

    public void Start()
    {
        tm = FindFirstObjectByType<TutorialManager>();
        yesBtn.onClick.AddListener(SubmitResult);       
    }

    public void GetGhostName(string ghostName)
    {
        ghostType.text = ($"현재 선택한 혼령은 {ghostName} 입니다.");
    }

    public void SubmitResult()
    {
        if(tm != null)
        {
            tm.OnSubmitResult();
        }
        //목표 완료. 본부로 돌아가십시오 창 켜지기
        submitPanel.SetActive(false);
        gobackPanel.SetActive(true);
    }

    //의뢰 완료 후 창 리셋시키기
    public void ResetPanel()
    {
        submitPanel.SetActive(true);
        gobackPanel.SetActive(false);
        ghostType.text = "현재 의뢰 중이 아닙니다.";
    }
}
