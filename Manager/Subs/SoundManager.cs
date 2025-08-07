using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using _1.Scripts.Manager.Core;
using _1.Scripts.Sound;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace _1.Scripts.Manager.Subs
{
    public enum BgmType
    {
        Lobby,
        Stage1,
        Stage2,
        Ending
    }

    public enum SfxType
    {
        Test,
        
        // UI SFX
        ButtonClick,
        PopupOpen,
        PopupClose,
        TypeWriter,
        
        // Player SFX
        PlayerHit,
        PlayerFootStep,
        PlayerLand,
        
        // Gun SFX
        PistolShoot,
        RifleShoot,
        GrenadeLauncherShoot,
        HackGunShoot,
        SniperRifleShoot,
        PistolReload,
        RifleReload,
        GrenadeLauncherReload,
        HackGunReload,
        SniperRifleReload,
        PistolEmpty,
        RifleEmpty,
        GrenadeLauncherEmpty,
        HackGunEmpty,
        SniperRifleEmpty,
        HeadShotSound,
        
        // Grenade SFX
        GrenadeExplode,
        
        // Enemy SFX
        EnemyAttack,
        EnemyHit,
        EnemyFootStep,
        Drone,
        Shebot,
        Dog,
        
        // Item SFX
        ItemPickup,
        
        // Door SFX
        Door,
        GarageDoor,
        
        // Other
        HackingTry,
        HackingSuccess,
        HackingFail,
        WeaponTuning,
        
        // Dialogue
        None,
        System,
        Stage2_Player,
        Stage2_System,
    }
    
    [Serializable] public class SoundManager
    {
        [Header("Sound Groups")] 
        [SerializeField] private SerializedDictionary<string, SoundGroupSO> soundGroups = new();
             
        private AudioSource bgmSource;
        private ResourceManager resourceManager;
        private ObjectPoolManager poolManager;
        
        private float masterVolume = 1.0f;
        private float bgmVolume = 1.0f;
        private float sfxVolume = 1.0f;
        
        public void Start(AudioSource audioSource)
        {
            resourceManager = CoreManager.Instance.resourceManager;
            poolManager = CoreManager.Instance.objectPoolManager;
            bgmSource = audioSource;
            
            bgmSource.playOnAwake = true;
            bgmSource.loop = true;

            if (bgmSource.clip != null)
                bgmSource.Play();
            
            LoadVolumeSettings(); 
        }

        public void CacheSoundGroup()
        {
            soundGroups.Clear();
            var loadedGroup = resourceManager.GetAllAssetsOfType<SoundGroupSO>();
            foreach (var group in loadedGroup)
            {
                if (!soundGroups.ContainsKey(group.groupName))
                {
                    soundGroups.Add(group.groupName, group);
                }
            }
        }

        public void PlayBGM(BgmType bgmType, int index = -1)
        {
            string groupName = bgmType.ToString();

            if (!soundGroups.TryGetValue(groupName, out var group))
            {
                Debug.LogWarning($"[PlayBGM] 사운드 그룹 '{groupName}' 없음");
                return;
            }

            var clipRef = (index < 0) ? group.GetRandomClip() : group.GetClip(index);
            if (clipRef?.Asset is not AudioClip clip)
            {
                Debug.LogWarning($"[PlayBGM] '{groupName}' 그룹에 로드된 AudioClip이 없습니다. 선로딩 필요.");
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public SoundPlayer PlayUISFX(SfxType sfxType, float duration = -1, int index = -1)
        {
            string groupName = sfxType.ToString();

            if (!soundGroups.TryGetValue(groupName, out var group))
            {
                Debug.LogWarning($"[PlayUISFX] 사운드 그룹 '{groupName}' 없음");
                return null;
            }

            var clipRef = (index < 0) ? group.GetRandomClip() : group.GetClip(index);
            if (clipRef?.Asset is not AudioClip clip)
            {
                Debug.LogWarning($"[PlayUISFX] '{groupName}' 클립 미로딩. 선로딩 필요.");
                return null;
            }

            var obj = poolManager.Get("SoundPlayer");
            if (!obj) return null;
            if (!obj.TryGetComponent(out SoundPlayer soundPlayer)) return null;
            
            soundPlayer.Play2D(clip, duration, sfxVolume);
            return soundPlayer;
        }
        
        public SoundPlayer PlaySFX(SfxType sfxType, Vector3 position, float duration = -1, int index = -1)
        {
            string groupName = sfxType.ToString();

            if (!soundGroups.TryGetValue(groupName, out var group))
            {
                Debug.LogWarning($"[PlaySFX] 사운드 그룹 '{groupName}' 없음");
                return null;
            }

            var clipRef = (index < 0) ? group.GetRandomClip() : group.GetClip(index);
            if (clipRef?.Asset is not AudioClip clip)
            {
                Debug.LogWarning($"[PlaySFX] '{groupName}' 클립 미로딩. 선로딩 필요.");
                return null;
            }

            // Debug.Log(clip);
            
            var obj = poolManager.Get("SoundPlayer");
            if (!obj.TryGetComponent(out SoundPlayer soundPlayer)) return null;
            
            soundPlayer.Play3D(clip, duration, sfxVolume, position);
            return soundPlayer;
        }
        
        public void StopBGM()
        {
            bgmSource.Stop();
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = masterVolume;
            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        }
        
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            bgmSource.volume = masterVolume * bgmVolume;
            PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume) * masterVolume;
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1.0f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            
            AudioListener.volume = masterVolume;
            bgmSource.volume = masterVolume * bgmVolume;
            sfxVolume = masterVolume * sfxVolume;
        }

        public async Task LoadClips()
        {
            foreach (var group in soundGroups.Values)
            {
                foreach (var clipRef in group.audioClips)
                {
                    if (clipRef == null) continue;
                    // Service.Log(group.name);
                    AudioClip clip;
                    if (clipRef.Asset is AudioClip existingClip) clip = existingClip;
                    else
                    {
                        var handle = clipRef.LoadAssetAsync<AudioClip>();
                        await handle.Task;
                        if (handle.Status != AsyncOperationStatus.Succeeded)
                        {
                            Debug.LogWarning($"[LoadClips] 로드 실패: {clipRef}");
                            continue;
                        }
                        clip = handle.Result;
                    }

                    if (clip.loadState != AudioDataLoadState.Loaded)
                    {
                        clip.LoadAudioData();
                        while (clip.loadState != AudioDataLoadState.Loaded)
                            await Task.Yield();
                    }
                }
            }
        }
    }
}
