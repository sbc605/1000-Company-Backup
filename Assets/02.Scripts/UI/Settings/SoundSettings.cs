using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MixerMaster = "MasterVolume";
    private const string MixerBGM = "BGMVolume";
    private const string MixerSFX = "SFXVolume";

    private const string PrefMaster = "SoundSetting_MasterVolume";
    private const string PrefBGM = "SoundSetting_BGMVolume";
    private const string PrefSFX = "SoundSetting_SFXVolume";

    void Start()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        LoadVolumeSettings();
    }

    void LoadVolumeSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(PrefMaster, 1.0f);
        float bgmVolume = PlayerPrefs.GetFloat(PrefBGM, 1.0f);
        float sfxVolume = PlayerPrefs.GetFloat(PrefSFX, 1.0f);

        if (masterSlider != null) masterSlider.value = masterVolume;
        if (bgmSlider != null) bgmSlider.value = bgmVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;

        ApplyVolumeToMixer(MixerMaster, masterVolume);
        ApplyVolumeToMixer(MixerBGM, bgmVolume);
        ApplyVolumeToMixer(MixerSFX, sfxVolume);
    }

    public void SetMasterVolume(float linearVolume)
    {
        ApplyAndSaveVolume(MixerMaster, PrefMaster, linearVolume);
    }

    public void SetBGMVolume(float linearVolume)
    {
        ApplyAndSaveVolume(MixerBGM, PrefBGM, linearVolume);
    }

    public void SetSFXVolume(float linearVolume)
    {
        ApplyAndSaveVolume(MixerSFX, PrefSFX, linearVolume);
    }

    private void ApplyAndSaveVolume(string mixerParam, string prefKey, float linearVolume)
    {
        ApplyVolumeToMixer(mixerParam, linearVolume);
        
        PlayerPrefs.SetFloat(prefKey, linearVolume);
        PlayerPrefs.Save();
    }

    private void ApplyVolumeToMixer(string mixerParam, float linearVolume)
    {
        float dbVolume = (linearVolume > 0.001f) ? Mathf.Log10(linearVolume) * 20 : -80f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat(mixerParam, dbVolume);
        }
    }
}