using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Décor du menu principal : une petite salle et des pantins qui se balancent.</summary>
    public class MenuDiorama : MonoBehaviour
    {
        public DungeonTheme theme;
        public Light sun;
        public float orbitSpeed = 4f;

        private RoomController room;
        private Transform cam;
        private float angle;

        private void Start()
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (theme == null && cfg != null) theme = cfg.ThemeFor(0);
            var plan = new RoomPlan { index = 0, kind = RoomKind.Start, origin = Vector3.zero, sizeX = 15, sizeZ = 11 };
            room = RoomBuilder.Build(plan, theme, transform);
            if (theme != null)
            {
                if (sun != null) { sun.color = theme.lightColor; sun.intensity = theme.lightIntensity; }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = theme.ambient;
            }
            if (cfg != null && cfg.villagerPrefab != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var pos = room.Center + new Vector3((i - 1) * 2.2f, 0f, i == 1 ? 0.8f : 0f);
                    var go = Instantiate(cfg.villagerPrefab, pos, Quaternion.Euler(0f, 180f + (i - 1) * 20f, 0f), transform);
                    var rig = go.GetComponentInChildren<PuppetRig>();
                    if (rig != null) { rig.ApplyLoadout(null, cfg); rig.SetWoodTint(GameManager.RandomWoodTint()); }
                }
            }
            if (Camera.main != null) cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (cam == null || room == null) return;
            angle += orbitSpeed * Time.unscaledDeltaTime;
            Vector3 c = room.Center + Vector3.up * 0.8f;
            Vector3 offset = Quaternion.Euler(38f, 180f + Mathf.Sin(angle * Mathf.Deg2Rad * 3f) * 18f, 0f) * new Vector3(0f, 0f, -14f);
            cam.position = c + offset;
            cam.LookAt(c);
        }
    }
}
