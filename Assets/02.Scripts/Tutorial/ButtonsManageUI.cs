using System.Collections.Generic;
using UnityEngine;

public class ButtonsManageUI : MonoBehaviour
{
    //코드 담당자: 최은주
    //튜토리얼 UI 버튼 매니징

    [SerializeField] TutorialManager tm;
    [SerializeField] private List<GameObject> pages;
    [SerializeField] private GameObject rootCanvas;
    private int currIndex = 0;

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].SetActive(i == index);         
        }
    }
    public void NextPage()
    {
        if(currIndex == pages.Count -1)
        {
            rootCanvas.SetActive(false);
            CursorManager.Instance.ClosePopUI();
            tm.SetPlayerPaused(false);            
            return;
        }

        currIndex++;
        ShowPage(currIndex);
         
    }

    public void PrevPage()
    {
        if(currIndex > 0)
        {
            currIndex--;
            ShowPage(currIndex);
        }
    }

}
