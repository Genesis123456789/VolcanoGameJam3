using System.Collections;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Caméra « diorama » façon Link's Awakening (Switch) : vue plongeante, focale serrée,
    /// cadre toujours la salle entière (le reste est noir profond), glisse d'une salle à l'autre.
    /// Si une salle est trop grande, la caméra suit le joueur à l'intérieur des limites de la salle.
    /// Secousses par « trauma » (bruit de Perlin) et petits coups directionnels.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RoomCamera : MonoBehaviour
    {
        public static RoomCamera Instance { get; private set; }

        [Header("Cadrage")]
        [Range(30f, 89f)] public float pitch = 58f;
        [Tooltip("Champ vertical serré = effet maquette.")]
        [Range(15f, 60f)] public float fieldOfView = 30f;
        public float margin = 0.6f;
        public float minDistance = 12f;
        public float maxDistance = 34f;
        [Tooltip("Décalage vertical du point visé (compense le mur sud bas).")]
        public float lookHeight = 0.6f;
        public float followSmooth = 6f;

        [Header("Secousses")]
        public float maxShakeOffset = 0.45f;
        public float maxShakeAngle = 1.6f;
        public float traumaDecay = 1.6f;
        public float noiseFrequency = 22f;

        public Camera Cam { get; private set; }
        public float CurrentDistance { get; private set; } = 20f;
        public Vector3 FocusPoint { get; private set; }

        private RoomController room;
        private Vector3 basePos;
        private Quaternion baseRot;
        private float trauma;
        private Vector3 kick;
        private Vector3 kickVel;
        private bool transitioning;
        private float seed;

        private void Awake()
        {
            Instance = this;
            room = null;
            trauma = 0f;
            transitioning = false;
            kick = kickVel = Vector3.zero;
            Cam = GetComponent<Camera>();
            Cam.fieldOfView = fieldOfView;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Color.black;
            baseRot = Quaternion.Euler(pitch, 0f, 0f);
            seed = Random.Range(0f, 100f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SnapTo(RoomController r)
        {
            room = r;
            ComputeTarget(r, out basePos, out float d);
            CurrentDistance = d;
            ApplyTransform();
        }

        public void TransitionTo(RoomController r, float duration)
        {
            StartCoroutine(TransitionRoutine(r, duration));
        }

        private IEnumerator TransitionRoutine(RoomController r, float duration)
        {
            transitioning = true;
            Vector3 start = basePos;
            float startD = CurrentDistance;
            room = r;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                ComputeTarget(r, out var target, out float d);
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                basePos = Vector3.Lerp(start, target, k);
                CurrentDistance = Mathf.Lerp(startD, d, k);
                yield return null;
            }
            ComputeTarget(r, out basePos, out float dd);
            CurrentDistance = dd;
            transitioning = false;
        }

        /// <summary>Position caméra qui cadre la salle (et suit le joueur si la salle déborde).</summary>
        private void ComputeTarget(RoomController r, out Vector3 pos, out float distance)
        {
            Vector3 forward = baseRot * Vector3.forward;
            Vector3 right = baseRot * Vector3.right;
            Vector3 up = baseRot * Vector3.up;
            Vector3 center = r.Center + Vector3.up * lookHeight;
            float tanV = Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * Mathf.Max(0.5f, Cam.aspect);

            Vector3 o = r.transform.position;
            float sx = r.size.x, sz = r.size.y;
            distance = minDistance;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = new Vector3(
                    o.x + ((i & 1) == 0 ? -margin : sx + margin),
                    (i & 2) == 0 ? 0f : RoomBuilder.WallHeight + 0.2f,
                    o.z + ((i & 4) == 0 ? -margin : sz + margin));
                Vector3 rel = c - center;
                float depth = Vector3.Dot(rel, forward);
                float x = Mathf.Abs(Vector3.Dot(rel, right));
                float y = Mathf.Abs(Vector3.Dot(rel, up));
                distance = Mathf.Max(distance, x / tanH - depth, y / tanV - depth);
            }

            Vector3 focus = center;
            if (distance > maxDistance)
            {
                // Salle trop grande : on suit le joueur, borné à la salle.
                float over = distance / maxDistance;
                distance = maxDistance;
                var player = PlayerController.Instance;
                if (player != null)
                {
                    Vector3 p = player.transform.position;
                    float halfW = sx * 0.5f / over, halfD = sz * 0.5f / over;
                    focus.x = Mathf.Clamp(p.x, o.x + halfW, o.x + sx - halfW);
                    focus.z = Mathf.Clamp(p.z, o.z + halfD, o.z + sz - halfD);
                }
            }
            FocusPoint = focus;
            pos = focus - forward * distance;
        }

        public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        public void Kick(Vector3 worldDir, float amount)
        {
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.0001f) return;
            kickVel += worldDir.normalized * amount * 6f;
        }

        private void LateUpdate()
        {
            if (room != null && !transitioning)
            {
                ComputeTarget(room, out var target, out float d);
                float k = 1f - Mathf.Exp(-followSmooth * Time.unscaledDeltaTime);
                basePos = Vector3.Lerp(basePos, target, k);
                CurrentDistance = Mathf.Lerp(CurrentDistance, d, k);
            }
            // Ressort pour les coups directionnels
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            kickVel += (-kick * 120f - kickVel * 14f) * dt;
            kick += kickVel * dt;
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            float shake = trauma * trauma;
            float t = Time.unscaledTime * noiseFrequency;
            Vector3 offset = new Vector3(
                (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f,
                (Mathf.PerlinNoise(seed + 1f, t) - 0.5f) * 2f,
                0f) * maxShakeOffset * shake;
            float roll = (Mathf.PerlinNoise(seed + 2f, t) - 0.5f) * 2f * maxShakeAngle * shake;
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, roll);
            transform.position = basePos + baseRot * offset + kick;
        }

        /// <summary>Visée souris : rayon écran → sol.</summary>
        public bool ScreenToGround(Vector2 screen, float height, out Vector3 point)
        {
            var ray = Cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            if (plane.Raycast(ray, out float d)) { point = ray.GetPoint(d); return true; }
            point = Vector3.zero;
            return false;
        }
    }
}
