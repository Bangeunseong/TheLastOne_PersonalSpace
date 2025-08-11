using System.Collections;
using _1.Scripts.Manager.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _1.Scripts.Weapon.Scripts.Guns
{
    public class BulletHoleParticle : MonoBehaviour
    {
        private static readonly Vector2[] quadUVs = { new(0, 0), new(0, 1), new(1, 0), new(1, 1) };
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");

        [Header("Components")] 
        [SerializeField] private Renderer render;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private GameObject parent;
        
        [Header("BulletHole Properties")] 
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private float fadeOutPercent = 80;
        [SerializeField] private Vector2 frames;
        [SerializeField] private bool randomRotation;

        // Fields
        private float life;
        private float fadeout;
        private Color color;
        private Color orgColor;
        private float orgAlpha;
        private Coroutine holeUpdateCoroutine;

        private void Awake()
        {
            if (!render) render = this.TryGetComponent<Renderer>();
            if (!meshFilter) meshFilter = this.TryGetComponent<MeshFilter>();
            if (!parent) parent = transform.parent.gameObject;
            
            orgColor = color = render.material.GetColor(TintColor);
            orgAlpha = color.a;
        }

        private void Reset()
        {
            if (!render) render = this.TryGetComponent<Renderer>();
            if (!meshFilter) meshFilter = this.TryGetComponent<MeshFilter>();
            if (!parent) parent = transform.parent.gameObject;
        }

        private void OnEnable()
        {
            // Random UVs
            var random = Random.Range(0, (int)(frames.x * frames.y));
            int fx = (int)(random % frames.x);
            int fy = (int)(random / frames.y);
            
            // Set new UVs
            var meshUvs = new Vector2[4];
            for (var i = 0; i < 4; i++)
            {
                meshUvs[i].x = (quadUVs[i].x + fx) * (1.0f / frames.x);
                meshUvs[i].y = (quadUVs[i].y + fy) * (1.0f / frames.y);
            }
            meshFilter.mesh.uv = meshUvs;

            // Random rotate
            if (randomRotation)
                transform.Rotate(0f, 0f, Random.Range(0f, 360f), Space.Self);

            // Start lifetime coroutine
            life = lifetime;
            fadeout = life * (fadeOutPercent / 100f);
            color = orgColor;
            color.a = orgAlpha;
            render.material.SetColor(TintColor, color);
            
            if (holeUpdateCoroutine != null) StopCoroutine(holeUpdateCoroutine);
            holeUpdateCoroutine = StartCoroutine(HoleUpdate());
        }

        private IEnumerator HoleUpdate()
        {
            while (life > 0f)
            {
                life -= Time.deltaTime;
                if (life <= fadeout)
                {
                    color.a = Mathf.Lerp(0f, orgAlpha, life / fadeout);
                    render.material.SetColor(TintColor, color);
                }
                yield return null;
            }
            CoreManager.Instance.objectPoolManager.Release(parent);
            holeUpdateCoroutine = null;
        }
    }
}