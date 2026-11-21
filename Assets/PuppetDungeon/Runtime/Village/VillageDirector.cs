using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Village des marionnettes (tous les N donjons) : le pantin arrive brisé, une marionnette
    /// vient à sa rencontre, le joueur lui offre une (ou plusieurs, rarement) de ses pièces et
    /// la marionnette devient le nouveau personnage joueur. Le vieux pantin reste au village.
    /// </summary>
    public class VillageDirector : MonoBehaviour
    {
        public Light sun;
        public DungeonTheme villageTheme;
        public Vector2Int plazaSize = new Vector2Int(23, 15);

        [Header("Dialogues")]
        [TextArea(2, 4)]
        public string[] greetings =
        {
            "Tes fils sont usés jusqu'à la corde, voyageur. Viens, assieds-toi près de l'âtre.",
            "Encore un qui remonte des profondeurs… Ton bois craque comme une vieille porte.",
            "Je t'ai entendu claquer des genoux depuis la grand-rue. Tu as marché longtemps.",
            "Les donjons t'ont pris ta couleur. Laisse-moi voir ce qu'il te reste."
        };
        [TextArea(2, 4)]
        public string[] requests =
        {
            "Tu ne pourras pas aller plus loin. Mais un morceau de toi peut continuer… à travers moi.",
            "C'est la coutume, ici : celui qui tombe confie sa meilleure part à celui qui se lève.",
            "Donne-moi ce qui compte. Je porterai ta mémoire là où tes jambes ne vont plus."
        };
        [TextArea(2, 4)]
        public string[] farewells =
        {
            "Je sens encore ta chaleur dans ce bois. On y retourne.",
            "Dors, ancien. Je finirai le chemin que tu as tracé.",
            "Les fils se tendent à nouveau. Ils ne me lâcheront pas, cette fois."
        };

        private RoomController plaza;
        private VillagerNPC villager;
        private Interactable gate;

        private IEnumerator Start()
        {
            var gm = GameManager.Instance;
            gm.EnsureRun();
            var cfg = gm.Config;

            if (sun != null && villageTheme != null)
            {
                sun.color = villageTheme.lightColor;
                sun.intensity = villageTheme.lightIntensity;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = villageTheme.ambient;
            }

            // Place du village
            var plan = new RoomPlan { index = 0, kind = RoomKind.Start, origin = Vector3.zero, sizeX = plazaSize.x, sizeZ = plazaSize.y };
            plaza = RoomBuilder.Build(plan, villageTheme, transform);
            VillageDecor.Build(plaza, villageTheme);
            gate = VillageDecor.BuildGate(plaza, villageTheme);
            gate.interactable = false;
            gate.Interacted += () => { gate.interactable = false; GameManager.Instance.LeaveVillage(); };
            if (RoomCamera.Instance != null) RoomCamera.Instance.SnapTo(plaza);

            // Joueur brisé
            var player = PlayerController.Instance != null ? PlayerController.Instance : Instantiate(cfg.playerPrefab).GetComponent<PlayerController>();
            yield return null;
            Vector3 arrival = plaza.Center + new Vector3(0f, 0f, -plazaSize.y * 0.5f + 2.5f);
            player.Teleport(arrival);
            player.FaceTowards(Vector3.forward);
            player.Lock(true);
            var playerFlash = player.GetComponent<HitFlash>();
            if (playerFlash != null) playerFlash.SetGlobalTint(new Color(0.6f, 0.58f, 0.6f));
            if (player.squash != null) player.squash.Punch(new Vector3(0.1f, -0.25f, 0.1f));

            // Marionnette du village
            string vName = gm.RandomPuppetName();
            for (int i = 0; i < 5 && (vName == gm.Run.puppetName || gm.Run.fallenPuppets.Contains(vName)); i++) vName = gm.RandomPuppetName();
            Color vTint = GameManager.RandomWoodTint();
            Vector3 home = plaza.Center + new Vector3(Random.Range(-5f, 5f), 0f, plazaSize.y * 0.5f - 3f);
            villager = SpawnVillager(cfg, home, vName, vTint);

            if (AudioManager.Instance != null && cfg.audio != null)
                AudioManager.Instance.PlayMusic(cfg.audio.musicVillage != null ? cfg.audio.musicVillage : cfg.audio.musicDungeon);
            var ui = UIRoot.Instance;
            if (ui != null) ui.TitleCard("Village", "Le Village des Fils", $"{gm.Run.puppetName}, pantin n°{gm.Run.generation}, est à bout de forces…");

            yield return new WaitForSeconds(2.2f);
            if (villager != null)
            {
                yield return villager.WalkTo(arrival + Vector3.forward * 1.6f, 2.2f);
                villager.Face(player.transform.position);
            }

            // Dialogue
            if (ui != null && ui.dialog != null)
            {
                yield return ui.dialog.Say(vName, Pick(greetings));
                yield return ui.dialog.Say(vName, Pick(requests));
                ui.dialog.Close();
            }

            // Offrande
            var inv = gm.Run.inventory;
            var equipped = inv.AllEquipped().ToList();
            var bag = inv.bag.Where(p => p != null).ToList();
            int max = Random.value < cfg.multiOfferingChance ? Random.Range(2, cfg.multiOfferingMax + 1) : 1;
            List<PartInstance> chosen = null;
            if (ui != null && ui.offering != null && (equipped.Count + bag.Count) > 0)
            {
                bool done = false;
                ui.offering.Show(equipped, bag, max, vName, list => { chosen = list; done = true; });
                while (!done) yield return null;
            }
            chosen = chosen ?? new List<PartInstance>();

            yield return TransferRoutine(player, chosen, vName, vTint);

            if (ui != null && ui.dialog != null)
            {
                yield return ui.dialog.Say(vName, Pick(farewells));
                ui.dialog.Close();
            }

            player.Lock(false);
            gate.interactable = true;
            if (ui != null)
            {
                ui.Banner($"{vName} reprend le flambeau", new Color(1f, 0.85f, 0.5f));
                ui.Toast($"Nouveau pantin : stats de base x{gm.Run.baseScale:0.00}", new Color(0.7f, 1f, 0.6f));
                ui.Toast("La porte du nord mène aux donjons suivants.", new Color(0.9f, 0.85f, 0.75f));
            }
        }

        private VillagerNPC SpawnVillager(GameConfig cfg, Vector3 pos, string vName, Color tint)
        {
            if (cfg.villagerPrefab == null) return null;
            var go = Instantiate(cfg.villagerPrefab, pos, Quaternion.LookRotation(Vector3.back), transform);
            var npc = go.GetComponent<VillagerNPC>();
            if (npc == null) npc = go.AddComponent<VillagerNPC>();
            npc.villagerName = vName;
            if (npc.rig != null)
            {
                npc.rig.ApplyLoadout(null, cfg);
                npc.rig.SetWoodTint(tint);
            }
            return npc;
        }

        private IEnumerator TransferRoutine(PlayerController player, List<PartInstance> chosen, string vName, Color vTint)
        {
            var gm = GameManager.Instance;
            var cfg = gm.Config;
            Vector3 from = player.transform.position + Vector3.up * 1.1f;
            Vector3 to = villager != null ? villager.transform.position + Vector3.up * 1.1f : from + Vector3.forward * 1.6f;

            // Les pièces volent de l'ancien pantin vers le nouveau
            foreach (var part in chosen)
            {
                var holder = new GameObject("Offrande").transform;
                holder.position = from;
                PartPreview.Build(part.definition != null ? part.definition.visualPrefab : null, holder, 0.6f);
                AudioManager.Sfx(l => l.offering);
                for (float t = 0f; t < 0.9f; t += Time.deltaTime)
                {
                    float k = t / 0.9f;
                    holder.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.8f;
                    holder.Rotate(0f, 400f * Time.deltaTime, 0f);
                    yield return null;
                }
                Juice.Spawn(v => v.lootBurst, to, Quaternion.identity, cfg.RarityColor(part.rarity), 1.2f);
                Destroy(holder.gameObject);
                yield return new WaitForSeconds(0.15f);
            }

            // Le vieux pantin s'effondre et reste au village
            var leftovers = gm.Run.inventory.equipped.Select(p => p != null && chosen.Contains(p) ? null : p).ToArray();
            Color oldTint = gm.Run.woodTint;
            Vector3 corpsePos = player.transform.position;
            Quaternion corpseRot = player.transform.rotation;
            if (cfg.villagerPrefab != null)
            {
                var corpse = Instantiate(cfg.villagerPrefab, corpsePos, corpseRot, transform);
                var npc = corpse.GetComponent<VillagerNPC>();
                if (npc != null) npc.sway = false;
                var rig = corpse.GetComponentInChildren<PuppetRig>();
                if (rig != null)
                {
                    rig.ApplyLoadout(leftovers, cfg);
                    rig.SetWoodTint(oldTint * 0.75f);
                    if (rig.Animator != null) rig.Animator.enabled = false;
                    rig.transform.localRotation = Quaternion.Euler(-78f, 0f, 0f);
                    rig.transform.localPosition += Vector3.up * 0.25f;
                }
                var label = corpse.AddComponent<Interactable>();
                label.prompt = $"{gm.Run.puppetName} se repose ici";
                label.radius = 1.5f;
            }

            // Le joueur « devient » la marionnette du village
            Juice.Spawn(v => v.healPuff, to, Quaternion.identity, new Color(1f, 0.9f, 0.6f), 2f);
            Juice.Shake(0.3f);
            PostFX.Pulse(PostFX.PulseKind.Heal);
            Vector3 newPos = villager != null ? villager.transform.position : player.transform.position;
            Quaternion newRot = villager != null ? villager.transform.rotation : player.transform.rotation;
            if (villager != null) Destroy(villager.gameObject);
            player.Teleport(newPos);
            player.transform.rotation = newRot;
            var flash = player.GetComponent<HitFlash>();
            if (flash != null) { flash.SetGlobalTint(Color.white); flash.Flash(new Color(1f, 0.95f, 0.8f), 0.5f); }
            gm.Rebirth(chosen, vName, vTint);
            player.RefreshFromRun();
            player.Health.Init(player.Stats[StatType.MaxHealth]);
            if (player.squash != null) player.squash.Punch(new Vector3(-0.2f, 0.35f, -0.2f));
            yield return new WaitForSeconds(0.6f);
        }

        private static string Pick(string[] arr) => arr == null || arr.Length == 0 ? "…" : arr[Random.Range(0, arr.Length)];
    }
}
