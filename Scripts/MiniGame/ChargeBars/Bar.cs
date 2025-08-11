using System;
using UnityEngine;
using UnityEngine.UI;

namespace _1.Scripts.MiniGame.ChargeBars
{
    public class Bar : MonoBehaviour
    {
        [field: Header("Components")]
        [field: SerializeField] public Image Img { get; private set; }
        [field: SerializeField] public float CurrentValue { get; private set; }
        [field: SerializeField] public bool IsFilled { get; private set; }
        [field: SerializeField] public ChargeGameController Controller { get; private set; }

        public void Reset()
        {
            if (!Img) Img = this.TryGetChildComponent<Image>("Body");
        }

        public void Initialize(ChargeGameController controller)
        {
            Controller = controller;
            CurrentValue = 0f;
            IsFilled = false;
            Img.fillAmount = CurrentValue;
        }
        
        public void IncreaseValue(float amount)
        {
            if (IsFilled) return;
            
            CurrentValue = Mathf.Min(CurrentValue + amount, 1f);
            Img.fillAmount = CurrentValue;
            
            if (!(CurrentValue >= 1f)) return;
            IsFilled = true; Controller.OnBarFilled();
        }

        public void DecreaseValue(float amount)
        {
            if (IsFilled) return;
            
            CurrentValue = Mathf.Max(CurrentValue - amount, 0f);
            Img.fillAmount = CurrentValue;
        }
    }
}