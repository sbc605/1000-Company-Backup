using UnityEngine;
using System.Collections.Generic;

public class SoundManager : SingleTon<SoundManager>
{
    [SerializeField] AudioSource bgmAudio;
    [SerializeField] AudioSource eventAudio;

    private Dictionary<string, AudioClip> clipDatabase = new Dictionary<string, AudioClip>();

    private const string BGM_PATH = "Audio/BGM";
    private const string CLIP_PATH = "Audio/Clip";

    protected override void Awake()
    {
        base.Awake();

        clipDatabase = new Dictionary<string, AudioClip>();

        LoadClipsFromResources(BGM_PATH);
        LoadClipsFromResources(CLIP_PATH);
    }

    private void LoadClipsFromResources(string path)
    {
        AudioClip[] clipsInFolder = Resources.LoadAll<AudioClip>(path);

        Debug.Log($"[SoundManager] {path} 폴더에서 {clipsInFolder.Length}개의 오디오 클립을 로드합니다.");

        foreach (var clip in clipsInFolder)
        {
            if (!clipDatabase.TryAdd(clip.name, clip))
            {
                Debug.LogWarning($"[SoundManager] 이미 딕셔너리에 '{clip.name}' 이름의 클립이 존재합니다. ({path} 경로)");
            }
        }
    }

    public void BgmSoundPlay(string clipName)
    {
        if (clipDatabase.TryGetValue(clipName, out AudioClip clip))
        {
            bgmAudio.clip = clip;
            bgmAudio.Play();
        }
        else
        {
            Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없습니다: {clipName}");
        }
    }

    public void EventSoundPlay(string clipName)
    {
        if (clipDatabase.TryGetValue(clipName, out AudioClip clip))
        {
            eventAudio.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] 이벤트 클립을 찾을 수 없습니다: {clipName}");
        }
    }

    public void BgmSoundStop()
    {
        bgmAudio.Stop();
    }
}