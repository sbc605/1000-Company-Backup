using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
public class TabletUIController : MonoBehaviour
{
    [System.Serializable]
    public class CategoryData
    {
        public string name;
        public Button button;
        public GameObject content;
    }

    [SerializeField] private TextMeshProUGUI mileageText;
    private int mileage = 1000;
    [SerializeField] private List<CategoryData> datas;

    private CategoryData _currentData;

    private void Start()
    {
        // 버튼 연결
        foreach (var data in datas)
        {
            data.button.onClick.AddListener(() => OnDataSelected(data));
        }

        // 처음에는 첫 번째 탭 활성화
        if (datas.Count > 0)
            OnDataSelected(datas[0]);

        UpdateMileageUI();
    }

    // 해당 카테고리에 매칭되는 버튼을 눌렀을 때 호출
    private void OnDataSelected(CategoryData selectedData)
    {
        // 클릭한 카테고리 외 다른 콘텐츠는 비활성화
        foreach (var data in datas)
        {
            bool isActive = data == selectedData;

            if (data.content != null)
                data.content.SetActive(isActive);
        }

        _currentData = selectedData;
    }

    public void SetMileage(int amount)
    {
        mileage = amount;
        UpdateMileageUI();
    }
    
    // 추후 플레이어 마일리지에 연동 필요
    private void UpdateMileageUI()
    {
        if (mileageText != null)
            mileageText.text = $"{mileage} 개";
    }
}
