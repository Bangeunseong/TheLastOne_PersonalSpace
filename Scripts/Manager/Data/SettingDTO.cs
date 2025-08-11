using System;
using UnityEngine;

namespace _1.Scripts.Manager.Data
{
    [Serializable] public class SettingDTO
    {
        [Header("Setting Data")]
        public Resolution resolution;
        public bool isFullScreen;
        
        public float masterVolume;
        public float bgmVolume;
        public float sfxVolume;
    }
}