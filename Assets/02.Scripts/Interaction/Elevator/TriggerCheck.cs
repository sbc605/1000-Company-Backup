using UnityEngine;

public class TriggerCheck : MonoBehaviour
{
    //코드 담당자: 최은주 


    public bool isPlayerInside;
    [SerializeField] Canvas goNoticeUI;

    private void Start()
    {
        isPlayerInside = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            isPlayerInside = true;
            goNoticeUI.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            isPlayerInside = false;
            goNoticeUI.gameObject.SetActive(false);
        }
    }
}
