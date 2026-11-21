using System.Collections;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Pièce au sol : jaillit, tourne, brille selon sa rareté. Ramassée au contact (aimant) ;
    /// si le sac est plein, elle reste au sol.
    /// </summary>
    public class LootPickup : MonoBehaviour
    {
        public Transform spinner;
        public Light beamLight;
        public ParticleSystem beam;
        public Renderer gem;
        public float pickupRadius = 1.1f;
        public float magnetRadius = 2.6f;

        public PartInstance Part { get; private set; }

        private bool ready;
        private bool taken;
        private float warnCooldown;
        private Vector3 restPos;

        public void Init(PartInstance part, Vector3 launchDir)
        {
            Part = part;
            var cfg = GameManager.Instance.Config;
            Color c = cfg.RarityColor(part.rarity);
            if (beamLight != null)
            {
                beamLight.color = c;
                beamLight.intensity = 1.5f + (int)part.rarity * 0.8f;
            }
            if (beam != null)
            {
                var main = beam.main;
                main.startColor = c;
                var em = beam.emission;
                em.rateOverTimeMultiplier = 6f + (int)part.rarity * 8f;
            }
            if (gem != null)
            {
                var mpb = new MaterialPropertyBlock();
                gem.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                mpb.SetColor("_EmissionColor", c * 1.5f);
                gem.SetPropertyBlock(mpb);
            }
            // Petit modèle de la pièce dans la toupie
            if (spinner != null && part.definition != null && part.definition.visualPrefab != null)
            {
                var preview = PartPreview.Build(part.definition.visualPrefab, spinner, 0.55f);
                if (preview != null) preview.transform.localPosition = Vector3.zero;
            }
            StartCoroutine(Pop(launchDir));
            if (part.rarity >= Rarity.Epic)
            {
                AudioManager.Sfx(l => l.legendaryDrop);
                Juice.Text(transform.position, EnumUtil.RarityLabel(part.rarity) + " !", c, 1.1f);
            }
            else AudioManager.Sfx(l => l.lootDrop, 0.8f);
        }

        private IEnumerator Pop(Vector3 dir)
        {
            Vector3 start = transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitSphere;
            dir.y = 0f;
            Vector3 end = start + dir.normalized * Random.Range(0.8f, 1.6f);
            // Ne pas sortir de la salle : on vérifie avec un rayon
            if (Physics.Raycast(start + Vector3.up * 0.5f, (end - start).normalized, out var hit, (end - start).magnitude + 0.4f, ~0, QueryTriggerInteraction.Ignore))
                end = start + (end - start).normalized * Mathf.Max(0f, hit.distance - 0.5f);
            end.y = 0f;
            start.y = 0f;
            for (float t = 0; t < 0.55f; t += Time.deltaTime)
            {
                float k = t / 0.55f;
                transform.position = Vector3.Lerp(start, end, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.6f + 0.1f);
                yield return null;
            }
            restPos = end;
            transform.position = restPos;
            Juice.Spawn(v => v.lootBurst, restPos + Vector3.up * 0.3f, Quaternion.identity, GameManager.Instance.Config.RarityColor(Part.rarity));
            ready = true;
        }

        private void Update()
        {
            if (spinner != null)
            {
                spinner.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
                if (ready) spinner.localPosition = new Vector3(0f, 0.75f + Mathf.Sin(Time.time * 2.4f) * 0.12f, 0f);
            }
            if (!ready || taken) return;
            var player = PlayerController.Instance;
            if (player == null || player.IsDead) return;
            Vector3 d = player.transform.position - transform.position; d.y = 0f;
            float dist = d.magnitude;
            var inv = GameManager.Instance.Run.inventory;
            if (dist < magnetRadius && inv.FreeCells > 0)
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(player.transform.position.x, 0f, player.transform.position.z), (magnetRadius - dist + 1f) * 4f * Time.deltaTime);
            if (dist < pickupRadius)
            {
                if (inv.TryAdd(Part)) Collect();
                else if (Time.time > warnCooldown)
                {
                    warnCooldown = Time.time + 2f;
                    if (UIRoot.Instance != null) UIRoot.Instance.Toast("Sac plein ! Faites de la place (I / Tab).", new Color(1f, 0.5f, 0.4f));
                }
            }
        }

        private void Collect()
        {
            taken = true;
            var cfg = GameManager.Instance.Config;
            AudioManager.Sfx(l => l.lootPickup);
            Juice.Spawn(v => v.lootBurst, transform.position + Vector3.up * 0.6f, Quaternion.identity, cfg.RarityColor(Part.rarity), 0.7f);
            if (UIRoot.Instance != null)
                UIRoot.Instance.Toast($"<color={cfg.RarityHex(Part.rarity)}>{Part.DisplayName}</color> ramassé", Color.white);
            Destroy(gameObject);
        }

        /// <summary>Fait apparaître une pièce au sol.</summary>
        public static LootPickup Drop(PartInstance part, Vector3 position, Vector3 dir)
        {
            var cfg = GameManager.Instance.Config;
            if (part == null || cfg.lootPickupPrefab == null) return null;
            var p = Instantiate(cfg.lootPickupPrefab, new Vector3(position.x, 0f, position.z), Quaternion.identity);
            var room = DungeonManager.Instance != null ? DungeonManager.Instance.CurrentRoom : null;
            if (room != null) p.transform.SetParent(room.transform, true);
            p.Init(part, dir);
            return p;
        }
    }
}
