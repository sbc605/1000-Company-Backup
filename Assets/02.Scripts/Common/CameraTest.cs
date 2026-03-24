using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraTest : MonoBehaviour
{
    Camera playerCamera;
    [SerializeField] private float distance = 10f;

    private void Start()
    {
        playerCamera = GetComponent<Camera>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 클릭 시
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, distance))
            {
                Debug.Log($"[Raycast Hit] {hit.collider.gameObject.name} / Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            else
            {
                Debug.Log("[Raycast] 아무 것도 맞지 않음");
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                foreach (var r in results)
                {
                    Debug.Log($"[UI Raycast Hit] {r.gameObject.name} (Sorting Layer: {r.sortingLayer}, Sorting Order: {r.sortingOrder})");
                }
            }
            else
            {
                Debug.Log("[UI Raycast] 아무 UI도 맞지 않음");
            }
        }
    }
}
