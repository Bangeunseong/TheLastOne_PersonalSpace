using System;
using System.Collections;
using System.Threading;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using _1.Scripts.Sound;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _1.Scripts.Map.Doors
{
    public class GarageDoor : MonoBehaviour
    {
        [field: Header("Components")]
        [field: SerializeField] public Transform Door { get; private set; }
        [field: SerializeField] public Transform UpperPosition { get; private set; }
        [field: SerializeField] public Transform LowerPosition { get; private set; }
        
        [field: Header("Door State")]
        [field: SerializeField] public bool IsOpened { get; private set; }
        [field: SerializeField] public float OpenDuration { get; private set; } = 3f;
        [field: SerializeField] public AnimationCurve DoorAnimationCurve { get; private set; }

        private CancellationTokenSource doorCTS;
        private SoundPlayer soundPlayer;
        
        private void Awake()
        {
            if (!Door) Door = this.TryGetChildComponent<Transform>("Door");
            if (!UpperPosition) UpperPosition = this.TryGetChildComponent<Transform>("UpperPosition");
            if (!LowerPosition) LowerPosition = this.TryGetChildComponent<Transform>("LowerPosition");
        }

        private void Reset()
        {
            if (!Door) Door = this.TryGetChildComponent<Transform>("Door");
            if (!UpperPosition) UpperPosition = this.TryGetChildComponent<Transform>("UpperPosition");
            if (!LowerPosition) LowerPosition = this.TryGetChildComponent<Transform>("LowerPosition");
        }

        private void Start()
        {
            doorCTS = new CancellationTokenSource();
        }

        public void ActiveDoor()
        {
            doorCTS?.Cancel(); doorCTS?.Dispose(); doorCTS = null;
            doorCTS = new CancellationTokenSource();
            _ = ActiveDoor_Async();
        }

        private async UniTaskVoid ActiveDoor_Async()
        {
            var originalDoorPosition = Door.localPosition;
            var targetPosition = IsOpened ? LowerPosition.localPosition : UpperPosition.localPosition;
            IsOpened = !IsOpened;
            float time = 0f;

            if (soundPlayer != null) soundPlayer.Stop();
            soundPlayer = CoreManager.Instance.soundManager.PlaySFX(SfxType.GarageDoor, transform.position, OpenDuration);
            
            while (time < OpenDuration)
            {
                time += Time.deltaTime;
                float t = time / OpenDuration;

                // 곡선을 적용한 비율
                float curveT = DoorAnimationCurve.Evaluate(t);
                Door.localPosition = Vector3.Lerp(originalDoorPosition, targetPosition, curveT);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: doorCTS.Token, cancelImmediately: true);
            }

            soundPlayer = null;
        }
    }
}