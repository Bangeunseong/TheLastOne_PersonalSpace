using System;
using System.Collections;
using System.Threading;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _1.Scripts.Map.Doors
{
    public class AutomaticDoor : MonoBehaviour
    {
        [field: Header("Doors")]
        [field: SerializeField] public Transform LowerDoor { get; private set; }
        [field: SerializeField] public Transform UpperDoor { get; private set; }
        [field: SerializeField] public Vector3 LowerVector { get; private set; }
        [field: SerializeField] public Vector3 UpperVector { get; private set; }
        
        [field: Header("Door Settings")]
        [field: SerializeField] public AnimationCurve DoorAnimationCurve { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }
        
        [field: Header("Detective Targets")]
        [field: SerializeField]
        public LayerMask TargetMask { get; private set; }
        
        private bool isOpen;
        private Vector3 originalUpperPosition;
        private Vector3 originalLowerPosition;
        private CancellationTokenSource doorCTS;

        private void Awake()
        {
            if (!LowerDoor) LowerDoor = this.TryGetChildComponent<Transform>("LowerDoor");
            if (!UpperDoor) UpperDoor = this.TryGetChildComponent<Transform>("UpperDoor");
        }

        private void Reset()
        {
            if (!LowerDoor) LowerDoor = this.TryGetChildComponent<Transform>("LowerDoor");
            if (!UpperDoor) UpperDoor = this.TryGetChildComponent<Transform>("UpperDoor");
        }

        private void Start()
        {
            originalUpperPosition = UpperDoor.localPosition;
            originalLowerPosition = LowerDoor.localPosition;
        }

        private void OnTriggerStay(Collider other)
        {
            if (isOpen || ((1 << other.gameObject.layer) & TargetMask.value) == 0) return;
            if (doorCTS != null) return;
            isOpen = !isOpen;
            doorCTS = new CancellationTokenSource();

            _ = Door_Async();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isOpen || ((1 << other.gameObject.layer) & TargetMask.value) == 0) return;
            if (doorCTS != null) return;
            isOpen = !isOpen;
            doorCTS = new CancellationTokenSource();

            _ = Door_Async();
        }

        private void OnDestroy()
        {
            doorCTS?.Cancel(); doorCTS?.Dispose();
            doorCTS = null;
        }

        private async UniTaskVoid Door_Async()
        {
            var time = 0f;
            Vector3 upperDoorPosition = UpperDoor.localPosition;
            Vector3 lowerDoorPosition = LowerDoor.localPosition;
            
            // 열리는 소리
            CoreManager.Instance.soundManager.PlaySFX(SfxType.Door, transform.position, index: isOpen ? 0 : 1); // 닫히는 소리
            
            while (time < Duration)
            {
                time += Time.deltaTime;
                float t = time / Duration;

                // 곡선을 적용한 비율
                float curveT = DoorAnimationCurve.Evaluate(t);
                if (isOpen)
                {
                    UpperDoor.localPosition = Vector3.Lerp(upperDoorPosition, UpperVector, curveT);
                    LowerDoor.localPosition = Vector3.Lerp(lowerDoorPosition, LowerVector, curveT);
                }
                else
                {
                    UpperDoor.localPosition = Vector3.Lerp(upperDoorPosition, originalUpperPosition, curveT);
                    LowerDoor.localPosition = Vector3.Lerp(lowerDoorPosition, originalLowerPosition, curveT);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: doorCTS.Token, cancelImmediately: true);
            }
            
            doorCTS.Dispose(); doorCTS = null;
        }
    }
}