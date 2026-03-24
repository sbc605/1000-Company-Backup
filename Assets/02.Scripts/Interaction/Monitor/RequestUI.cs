using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RequestUI : MonoBehaviour
{
    //코드 담당자 : 최은주 
    //scriptable object의 의뢰 정보 받는 상위 프리팹.
  
    public GameObject requestDetailUI;
    
    public TextMeshProUGUI buttonTitle;
    public TextMeshProUGUI buttonAddress;
    public TextMeshProUGUI reqState;
  
    DetailInfo request;
    public int index; //순서

    private void Start()
    {
        reqState.text = "미해결";
    }

    public void SetInfo(DetailInfo infoIndex)
    {
        request = infoIndex;
        index = infoIndex.requestOrder;

        buttonTitle.text = infoIndex.requestName;
        buttonAddress.text = infoIndex.address;
    }

    public void TurnOnRequest() //자신의 의뢰 인덱스값과 맞는 의뢰창 열기
    { 
        foreach(var correctUi in RequestManager.Instance.detailDatas)
        {
            if(correctUi.uiIndex == index)
            {
                correctUi.gameObject.SetActive(true);
            }
        }            
    }

    public void TurnOffRequest() //자신의 의뢰 인덱스값과 맞는 의뢰창 닫기
    {
        foreach (var correctUi in RequestManager.Instance.detailDatas)
        {
            if (correctUi.uiIndex == index)
            {
                correctUi.gameObject.SetActive(false);
            }
        }
    }

    public void SetStatus(string text)
    {
        reqState.text = text;
    }   
}
