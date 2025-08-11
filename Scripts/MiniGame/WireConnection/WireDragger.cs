using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _1.Scripts.MiniGame.WireConnection
{
    public class WireDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Components")]
        [SerializeField] private WireGameController wireGameController;
        [SerializeField] private Canvas canvas;
        [SerializeField] public Socket AttachedSocket;
        [SerializeField] private RectTransform socketRectTransform;
        
        [Header("Current Wire")]
        [SerializeField] private GameObject wire;
        [SerializeField] private Image wireImage; 
        [SerializeField] private RectTransform wireTransform;

        private void Awake()
        {
            if (!AttachedSocket) AttachedSocket = this.TryGetComponent<Socket>();
            if (!socketRectTransform) socketRectTransform = this.TryGetComponent<RectTransform>();
        }

        public void Initialize(Canvas parent, WireGameController controller)
        {
            canvas = parent;
            wireGameController = controller;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Service.Log($"{eventData.position}");
            if (!AttachedSocket || AttachedSocket.IsConnected)
                return;

            wire = wireGameController.CreateLine(wireGameController.WireContainer);
            wireImage = wire.GetComponent<Image>();
            wireTransform = wire.GetComponent<RectTransform>();
            wireImage.color = AttachedSocket.GetColor();

            var worldScreenPosition =
                RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, socketRectTransform.position);
            Service.Log($"{worldScreenPosition}");
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, 
                worldScreenPosition,
                eventData.pressEventCamera, out var pos);

            wireTransform.anchoredPosition = pos;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!wire) return;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position, eventData.pressEventCamera,
                out var pos);
            Service.Log($"{pos}");
            
            // Deprecated
            // Canvas 기준 Local Position을 World 기준으로 바꾸고
            // World Position을 LineRenderer 기준 Local Position으로 바꿔 적용
            // Vector3 worldPos = canvas.transform.TransformPoint(pos);
            // Vector3 localToLineRenderer = lineRenderer.transform.InverseTransformPoint(worldPos);
            
            UpdateWire(pos);
        }

        private void UpdateWire(Vector2 pos)
        {
            Vector2 dir = pos - wireTransform.anchoredPosition;
            float dist = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            wireTransform.sizeDelta = new Vector2(dist, wireTransform.sizeDelta.y);
            wireTransform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!wire) return;

            GameObject target = eventData.pointerCurrentRaycast.gameObject;

            if (!target || !target.TryGetComponent(out Socket endSocket)) {
                Destroy(wire.gameObject);
                Debug.Log("Connection Failed!");
                return;
            }

            if (endSocket != AttachedSocket &&
                endSocket.Type != AttachedSocket.Type &&
                endSocket.Color == AttachedSocket.Color &&
                !endSocket.IsConnected && !AttachedSocket.IsConnected)
            {
                Debug.Log("Connection Success!: " + AttachedSocket.Color);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, endSocket.transform.position),
                    eventData.pressEventCamera,
                    out var pos);
                
                // Deprecated
                // Canvas 기준 Local Position을 World 기준으로 바꾸고
                // World Position을 LineRenderer 기준 Local Position으로 바꿔 적용
                // Vector3 worldPos = canvas.transform.TransformPoint(endWorldPos);
                // Vector3 localToLineRenderer = lineRenderer.transform.InverseTransformPoint(worldPos);
                
                UpdateWire(pos);
                AttachedSocket.IsConnected = true;
                endSocket.IsConnected = true;
                wireGameController.RegisterConnection(AttachedSocket, endSocket, wire);
            }
            else
            {
                Destroy(wire.gameObject);
                Debug.Log("Connection Failed!");
            }
        }
    }
}