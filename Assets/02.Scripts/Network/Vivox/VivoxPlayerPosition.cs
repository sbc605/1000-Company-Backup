using System.Collections;
using Unity.Services.Vivox;
using UnityEngine;
using Fusion;
using System;

// 코드 담당자: 김수아
/// <summary>
/// 플레이어 프리팹에 넣어서 사용
/// 플레이어의 위치를 3D 채널에 업데이트 → 거리에 따른 감쇄
/// </summary>
public class VivoxPlayerPosition : NetworkBehaviour
{
    [SerializeField] Transform cam; //플레이어 카메라
    [SerializeField] private WaitForSeconds interval = new WaitForSeconds(0.1f);

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            StartCoroutine(WaitStartVoicePos());
        }
    }

    private IEnumerator WaitStartVoicePos()
    {
        // Vivox 초기화/로그인/채널 Join이 끝날 때까지 대기
        while (!VivoxManager.Instance.MainChannelConnected)
            yield return null;

        yield return new WaitForSeconds(3f);

        // 채널 연결 확실히 끝 -> 음성 위치 업데이트 시작
        StartCoroutine(UpdateVoicePos());
    }

    /// <summary>
    /// 3D 채널상의 위치를 최신화하는 함수
    /// </summary>
    private IEnumerator UpdateVoicePos()
    {
        while (true)
        {
            if (!VivoxManager.Instance.IsInPositionalChannel)
            {
                yield return interval;
                continue;
            }

            string channel = VivoxManager.Instance.mainChannel;

            try
            {
                // MainChannel에 있을 때만 3D 위치 업데이트
                VivoxService.Instance.Set3DPosition(transform.position, cam.position, cam.forward, cam.up, channel);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Vivox] Set3DPosition 실패, 채널 끊김: {e.Message}");
                VivoxManager.Instance.IsInPositionalChannel = false;
                VivoxManager.Instance.MainChannelConnected = false;

                // 재접속 시작
                VivoxManager.Instance.ReconnectLoop();
            }

            yield return interval;
        }
    }
}
