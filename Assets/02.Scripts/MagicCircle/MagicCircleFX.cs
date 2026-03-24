using UnityEngine;

// 코드 담당자: 김수아

public class MagicCircleFX : MonoBehaviour
{
    [SerializeField] private GameObject defaultCircle;
    [SerializeField] private GameObject correctCircle;
    [SerializeField] private GameObject faultCircle;

    public void ShowDefault()
    {
        Set(defaultCircle, true);
        Set(correctCircle, false);
        Set(faultCircle, false);
    }

    public void ShowResult(bool success)
    {
        Set(defaultCircle, false);
        Set(correctCircle, success);
        Set(faultCircle, !success);
    }

    private void Set(GameObject obj, bool on)
    {
        if (obj) obj.SetActive(on);
    }
}
