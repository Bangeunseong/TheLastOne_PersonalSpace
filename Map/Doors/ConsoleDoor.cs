using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _1.Scripts.Map.Doors
{
    public class ConsoleDoor : MonoBehaviour
    {
        [field: Header("Doors")]
        [field: SerializeField] public Transform LowerDoor { get; private set; }
        [field: SerializeField] public Transform UpperDoor { get; private set; }
        [field: SerializeField] public Vector3 LowerVector { get; private set; }
        [field: SerializeField] public Vector3 UpperVector { get; private set; }
        
        [field: Header("Lights")]
        [field: SerializeField] public List<Light> Indicators { get; private set; }
        
        [field: Header("Door Settings")]
        [field: SerializeField] public AnimationCurve DoorAnimationCurve { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }
        [field: SerializeField] public bool IsOpened { get; private set; }
        
        private CancellationTokenSource doorCTS;
        
        private void Awake()
        {
            if (!LowerDoor) LowerDoor = this.TryGetChildComponent<Transform>("LowerDoor");
            if (!UpperDoor) UpperDoor = this.TryGetChildComponent<Transform>("UpperDoor");
            if (Indicators.Count <= 0) Indicators = new List<Light>(GetComponentsInChildren<Light>());
        }

        private void Reset()
        {
            if (!LowerDoor) LowerDoor = this.TryGetChildComponent<Transform>("LowerDoor");
            if (!UpperDoor) UpperDoor = this.TryGetChildComponent<Transform>("UpperDoor");
            if (Indicators.Count <= 0) Indicators = new List<Light>(GetComponentsInChildren<Light>());
        }

        private void OnDestroy()
        {
            doorCTS?.Cancel();
            doorCTS?.Dispose();
            doorCTS = null;
        }

        public void Initialize(bool isOpened)
        {
            IsOpened = isOpened;
            if (!IsOpened) return;
            LowerDoor.localPosition = LowerVector;
            UpperDoor.localPosition = UpperVector;
            foreach (var indicator in Indicators) indicator.color = Color.green; 
        }

        public void OpenDoor()
        {
            foreach (var indicator in Indicators) indicator.color = Color.green; 
            
            CoreManager.Instance.soundManager.PlaySFX(SfxType.Door, transform.position, index:0);
            doorCTS = new CancellationTokenSource();
            _ = OpenDoor_Async();
        }

        private async UniTaskVoid OpenDoor_Async()
        {
            var time = 0f;
            Vector3 upperDoorPosition = UpperDoor.localPosition;
            Vector3 lowerDoorPosition = LowerDoor.localPosition;
            
            // 열리는 소리
            CoreManager.Instance.soundManager.PlaySFX(SfxType.Door, transform.position, index: 0); // 닫히는 소리
            
            IsOpened = true;
            while (time < Duration)
            {
                time += Time.deltaTime;
                float t = time / Duration;

                // 곡선을 적용한 비율
                float curveT = DoorAnimationCurve.Evaluate(t);
                if (IsOpened)
                {
                    UpperDoor.localPosition = Vector3.Lerp(upperDoorPosition, UpperVector, curveT);
                    LowerDoor.localPosition = Vector3.Lerp(lowerDoorPosition, LowerVector, curveT);
                }
                else
                {
                    // UpperDoor.localPosition = Vector3.Lerp(upperDoorPosition, originalUpperPosition, curveT);
                    // LowerDoor.localPosition = Vector3.Lerp(lowerDoorPosition, originalLowerPosition, curveT);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: doorCTS.Token, cancelImmediately: true);
            }
            
            doorCTS.Dispose(); doorCTS = null;
        }
    }
}