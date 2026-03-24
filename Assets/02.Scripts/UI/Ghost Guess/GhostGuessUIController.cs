using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 코드 작성자 : 정하윤
public class GhostGuessDataUIController : MonoBehaviour
{
    public List<GhostGuessData> ghosts = new List<GhostGuessData>();

    public Dictionary<string, Button> ghostButtons = new Dictionary<string, Button>();

    public Button[] hintButtons;
    [SerializeField] private Button[] ghostButtonsArray;



    // 현재 선택된 힌트들의 비트 마스크
    private EHintType selectedHints = EHintType.None;

    // 각 힌트 버튼이 선택되었는지 저장
    private bool[] isSelectedHint;

    // 현재 비트마스크 상태 확인용
    [SerializeField] private int currentMask;

    // 일반 버튼 색상
    public Color defaultButtonColor = Color.white;

    // 선택된 버튼 색상
    public Color selectedButtonColor = Color.green;

    //이하 귀신 추리 상태에 들어갔는지 판단 bool
    public bool isGuessReady { get; private set; }
    public bool isSelectedGhost { get; private set; }
    private string currentSelectedGhost = null;

    TutorialManager tm;
    [SerializeField] SubmitResultUIController src;

    void Start()
    {
        isSelectedHint = new bool[hintButtons.Length];
        tm = FindFirstObjectByType<TutorialManager>();

        // 귀신 버튼을 담은 배열을 순회하면서 딕셔너리에 귀신 버튼과 이름 매칭
        foreach (var button in ghostButtonsArray)
        {
            if (button == null)
                continue;

            string ghostName = button.name;

            // 딕셔너리에 귀신 이름이 존재하지 않는 경우에만 
            // 귀신 이름을 키로 딕셔너리에 추가
            if (!ghostButtons.ContainsKey(ghostName))
                ghostButtons.Add(ghostName, button);
        }
    }

    // 힌트 버튼 클릭 시 호출되는 함수
    public void OnHintButtonClicked(int hintIndex)
    {
        // 선택과 해제 둘 다 가능하도록 반전
        isSelectedHint[hintIndex] = !isSelectedHint[hintIndex];

        // 클릭한 버튼(힌트)에 해당하는 비트 값 계산
        EHintType clickedHint = (EHintType)(1 << hintIndex);

        // 선택인 경우 비트 추가
        if (isSelectedHint[hintIndex])
            selectedHints |= clickedHint;
        // 해제인 경우 비트 제거
        else
            selectedHints &= ~clickedHint; 

        // 클릭한 버튼의 색상 변경
        hintButtons[hintIndex].image.color = isSelectedHint[hintIndex] ? selectedButtonColor : defaultButtonColor;

        ApplyHint();
        UpdateGuessState();
    }

    // 선택된 힌트 기준으로 귀신 버튼 필터링하는 함수
    void ApplyHint()
    {
        // 모든 귀신을 순회하며 힌트에 부합하는 귀신 버튼만 활성화
        foreach (var ghost in ghosts)
        {
            // 선택된 힌트를 귀신이 모두 가지고 있다면 true(선택된 힌트 외에 다른 힌트가 귀신에게 있더라도 true)
            bool isMatched = (ghost.hints & selectedHints) == selectedHints;

            // 딕셔너리에서 해당 귀신에 해당되는 버튼을 찾아 button에 할당
            if (ghostButtons.TryGetValue(ghost.name, out var button))
            {
                // 선택된 힌트를 모두 가진 귀신 버튼만 활성화
                button.interactable = isMatched;
            }
        }
    }


    //귀신 추정 중인지 확인
    void UpdateGuessState()
    {
        int selectedCount = 0;

        for (int i = 0; i < isSelectedHint.Length; i++)
        {
            if (isSelectedHint[i])
                selectedCount++;
        }

        bool prevGuessReady = isGuessReady;
        isGuessReady = selectedCount >= 3;

        // false → true 로 바뀌는 순간에만
        if (!prevGuessReady && isGuessReady)
        {
            tm.OnClickedGhostTrait();
        }
    }

    public void OnGhostButtonClicked(string ghostName)
    {
        if (!isGuessReady) return;

        // 같은 귀신 다시 누르면 취소
        if (currentSelectedGhost == ghostName)
        {
            currentSelectedGhost = null;
            isSelectedGhost = false;

            Debug.Log("귀신 선택 취소");
            return;
        }

        bool prevSelected = isSelectedGhost;

        currentSelectedGhost = ghostName;
        isSelectedGhost = true;

        // 처음 선택되는 순간만
        if (!prevSelected && isSelectedGhost)
        {
            tm.OnGhostFigureOut();
            src.GetGhostName(ghostName);
        }

        Debug.Log($"플레이어가 귀신 선택: {ghostName}");      
    }
}
