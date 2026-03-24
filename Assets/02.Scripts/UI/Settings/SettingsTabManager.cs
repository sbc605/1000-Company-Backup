//코드 담당자: 유호정
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;


[System.Serializable]
public class SettingsTab
{
    public Button button;
    public CanvasGroup panel;
}

public class SettingsTabManager : MonoBehaviour
{
    [SerializeField]
    private List<SettingsTab> tabs;


    [SerializeField]
    private CanvasGroup initialPanel;

 
    [SerializeField]
    private Color selectedColor = Color.white;

    [SerializeField]
    private Color deselectedColor = new Color(0.8f, 0.8f, 0.8f);

    void Start()
    {

        foreach (SettingsTab tab in tabs)
        {
            tab.button.onClick.AddListener(() => OpenPanel(tab.panel));
        }

        if (initialPanel != null)
        {
            OpenPanel(initialPanel);
        }
        else if (tabs.Count > 0)
        {
            OpenPanel(tabs[0].panel);
        }
    }

    private void HideAll()
    {
        foreach (SettingsTab tab in tabs)
        {
            if (tab.panel != null)
            {
                tab.panel.alpha = 0;
                tab.panel.interactable = false;
                tab.panel.blocksRaycasts = false;
            }
            if (tab.button != null && tab.button.targetGraphic != null)
            {
                tab.button.targetGraphic.color = deselectedColor;
            }
        }
    }


    public void OpenPanel(CanvasGroup panelToOpen)
    {
        HideAll();
        SettingsTab tabToOpen = tabs.FirstOrDefault(t => t.panel == panelToOpen);

        if (tabToOpen != null)
        {
            tabToOpen.panel.alpha = 1;
            tabToOpen.panel.interactable = true;
            tabToOpen.panel.blocksRaycasts = true;
            
            if (tabToOpen.button != null && tabToOpen.button.targetGraphic != null)
            {
                tabToOpen.button.targetGraphic.color = selectedColor;
            }
        }
    }
}