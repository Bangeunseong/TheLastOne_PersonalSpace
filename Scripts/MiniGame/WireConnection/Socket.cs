using System;
using UnityEngine;
using UnityEngine.UI;

namespace _1.Scripts.MiniGame.WireConnection
{
    public enum SocketColor
    {
        Red,
        Blue,
        Green,
        Purple,
        Yellow,
    }

    public enum SocketType
    {
        Start, 
        End,
    }
    
    [ExecuteInEditMode] public class Socket : MonoBehaviour
    {
        [field: Header("Components")]
        [field: SerializeField] public RectTransform RectTransform { get; private set; }
        [field: SerializeField] public Image Img { get; private set; }
        
        [field: Header("Socket Settings")]
        [field: SerializeField] public SocketType Type { get; private set; }
        [field: SerializeField] public SocketColor Color { get; private set; }
        [field: SerializeField] public bool IsConnected { get; set; }

        private void Awake()
        {
            if (!RectTransform) RectTransform = this.TryGetComponent<RectTransform>();
            if (!Img) Img = this.TryGetComponent<Image>();
        }

        private void Reset()
        {
            if (!RectTransform) RectTransform = this.TryGetComponent<RectTransform>();
            if (!Img) Img = this.TryGetComponent<Image>();
        }

        public void Initialize(SocketColor color, SocketType type)
        {
            Color = color; 
            Type = type;
            Img.color = GetColor();
        }

        public Color GetColor()
        {
            return Color switch
            {
                SocketColor.Red => new Color(1f, 120f/255f, 120f/255f),
                SocketColor.Blue => new Color(120f/255f, 120f/255f, 1f),
                SocketColor.Green => new Color(120f/255f, 1f, 120f/255f),
                SocketColor.Purple => new Color(240f / 255f, 120f/255f, 1f),
                SocketColor.Yellow => new Color(1f,1f, 120f/255f),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}