using Fusion;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUi : MonoBehaviour
{
    [SerializeField] TMP_Text nicknameText;
    [SerializeField] public Button readyBtn;
    [SerializeField] public Button cancelBtn;

    PlayerRef playerRef;
    NetworkRunner runner;

    private PlayerReadyState _cachedReadyState;
    private bool _lastKnownReadyState = false;
    private bool _isInitialized = false;


    public void Init(PlayerRef player, NetworkRunner assignedRunner)
    {
        playerRef = player;
        runner = assignedRunner;
        nicknameText.text = $"Player {player.PlayerId}";

        bool isMine = (runner.LocalPlayer == playerRef);

        nicknameText.color = Color.white;
        readyBtn.gameObject.SetActive(isMine);
        cancelBtn.gameObject.SetActive(false);
        readyBtn.interactable = isMine;

        readyBtn.onClick.AddListener(OnReadyClicked);
        cancelBtn.onClick.AddListener(OnCancelClicked);
    }
    
    private void LateUpdate()
    {
        if (runner == null) return;

        if (_cachedReadyState == null)
        {
            if (runner.TryGetPlayerObject(playerRef, out var playerObj))
            {
                _cachedReadyState = playerObj.GetComponent<PlayerReadyState>();
            }
            else
            {
                return;
            }
        }

        bool currentReadyState = _cachedReadyState.IsReady;

        if (!_isInitialized || currentReadyState != _lastKnownReadyState)
        {
            UpdateReadyVisual(currentReadyState, playerRef);
            _lastKnownReadyState = currentReadyState;
            _isInitialized = true;
        }
    }


    void OnReadyClicked()
    {

        if (runner.LocalPlayer != playerRef) return;
        if (_cachedReadyState != null)
        {
            _cachedReadyState.RPC_SetReady(true);
            Debug.Log($"[{playerRef}] Ready TRUE RPC º¸³¿");
        }
    }

    void OnCancelClicked()
    {
        if (runner.LocalPlayer != playerRef) return;
        if (_cachedReadyState != null)
        {
            _cachedReadyState.RPC_SetReady(false);
            Debug.Log($"[{playerRef}] Ready FALSE RPC º¸³¿");
        }
    }

    public void UpdateReadyVisual(bool ready, PlayerRef owner)
    {
        nicknameText.color = ready ? Color.green : Color.white;
        bool isMine = (runner.LocalPlayer == owner);

        if (isMine)
        {
            readyBtn.gameObject.SetActive(!ready);
            cancelBtn.gameObject.SetActive(ready);
            readyBtn.interactable = true;
            cancelBtn.interactable = true;
        }
        else
        {
            readyBtn.gameObject.SetActive(true);
            cancelBtn.gameObject.SetActive(false);
            readyBtn.interactable = false;
            cancelBtn.interactable = false;
        }
    }
}