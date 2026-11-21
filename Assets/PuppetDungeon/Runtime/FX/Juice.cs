using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Calques utilisés par le jeu (créés par l'outil de setup).</summary>
    public static class Layers
    {
        public static int Player => Get("Player");
        public static int Enemy => Get("Enemy");
        public static int Debris => Get("Debris");
        public static int Projectile => Get("Projectile");
        public static int Environment => 0; // Default

        private static readonly Dictionary<string, int> cache = new Dictionary<string, int>();
        internal static void ResetStatics() => cache.Clear();

        private static int Get(string n)
        {
            if (cache.TryGetValue(n, out int v)) return v;
            v = LayerMask.NameToLayer(n);
            if (v < 0) v = 0;
            cache[n] = v;
            return v;
        }

        public static int EnemyMask => 1 << Enemy;
        public static int PlayerMask => 1 << Player;
        public static int EnvironmentMask => 1 << Environment;

        public static void ConfigureCollisions()
        {
            if (Debris != 0)
            {
                Physics.IgnoreLayerCollision(Debris, Player, true);
                Physics.IgnoreLayerCollision(Debris, Enemy, true);
                Physics.IgnoreLayerCollision(Debris, Debris, false);
                if (Projectile != 0) Physics.IgnoreLayerCollision(Debris, Projectile, true);
            }
        }
    }

    /// <summary>Boîte à outils de « juice » : effets, secousses, débris, chiffres de dégâts.</summary>
    public static class Juice
    {
        private static VfxLibrary Vfx => GameManager.Instance != null && GameManager.Instance.Config != null ? GameManager.Instance.Config.vfx : null;

        public static ParticleSystem Spawn(System.Func<VfxLibrary, ParticleSystem> pick, Vector3 pos, Quaternion rot, Color? color = null, float scale = 1f)
        {
            var lib = Vfx;
            if (lib == null) return null;
            var prefab = pick(lib);
            if (prefab == null) return null;
            var ps = Object.Instantiate(prefab, pos, rot);
            ps.transform.localScale = Vector3.one * scale;
            if (color.HasValue)
            {
                var main = ps.main;
                main.startColor = color.Value;
                foreach (var child in ps.GetComponentsInChildren<ParticleSystem>())
                {
                    if (child == ps) continue;
                    var m = child.main;
                    m.startColor = color.Value;
                }
            }
            var mainModule = ps.main;
            mainModule.stopAction = ParticleSystemStopAction.Destroy;
            ps.Play(true);
            return ps;
        }

        public static GameObject SpawnObject(System.Func<VfxLibrary, GameObject> pick, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            var lib = Vfx;
            if (lib == null) return null;
            var prefab = pick(lib);
            if (prefab == null) return null;
            return Object.Instantiate(prefab, pos, rot, parent);
        }

        public static void Shake(float trauma)
        {
            if (RoomCamera.Instance != null) RoomCamera.Instance.AddTrauma(trauma * Settings.ScreenShake);
        }

        public static void Kick(Vector3 direction, float amount)
        {
            if (RoomCamera.Instance != null) RoomCamera.Instance.Kick(direction, amount * Settings.ScreenShake);
        }

        public static void DamageNumber(Vector3 pos, float amount, bool crit, Color color)
        {
            var lib = Vfx;
            if (lib == null || lib.damageNumber == null) return;
            var dn = Object.Instantiate(lib.damageNumber, pos + Vector3.up * 1.6f + Random.insideUnitSphere * 0.25f, Quaternion.identity);
            dn.Show(amount, crit, color);
        }

        public static void Text(Vector3 pos, string text, Color color, float size = 1f)
        {
            var lib = Vfx;
            if (lib == null || lib.damageNumber == null) return;
            var dn = Object.Instantiate(lib.damageNumber, pos + Vector3.up * 2f, Quaternion.identity);
            dn.ShowText(text, color, size);
        }

        /// <summary>Transforme un maillage en débris physique (le pantin se disloque).</summary>
        public static void MakeDebris(GameObject source, Vector3 origin, float force)
        {
            if (source == null) return;
            GameObject piece;
            var smr = source.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                if (!smr.enabled || smr.sharedMesh == null) return;
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                piece = new GameObject(source.name + " (débris)");
                piece.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                piece.AddComponent<MeshFilter>().sharedMesh = baked;
                var mr = piece.AddComponent<MeshRenderer>();
                mr.sharedMaterials = smr.sharedMaterials;
                smr.enabled = false;
            }
            else
            {
                var mf = source.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) return;
                piece = source;
                Vector3 s = piece.transform.lossyScale;
                piece.transform.SetParent(null, true);
                piece.transform.localScale = s;
            }

            piece.layer = Layers.Debris;
            var filter = piece.GetComponent<MeshFilter>();
            var box = piece.AddComponent<BoxCollider>();
            if (filter != null && filter.sharedMesh != null)
            {
                var b = filter.sharedMesh.bounds;
                box.center = b.center;
                box.size = Vector3.Max(b.size, Vector3.one * 0.02f);
            }
            var rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Vector3 dir = (piece.transform.position - origin);
            dir.y = 0f;
            dir = dir.sqrMagnitude < 0.001f ? Random.insideUnitSphere : dir.normalized;
            dir += Vector3.up * 1.2f + Random.insideUnitSphere * 0.4f;
            rb.AddForce(dir.normalized * force, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * force * 2f, ForceMode.VelocityChange);
            piece.AddComponent<DebrisLife>();
        }

        public static IEnumerator ScalePunch(Transform t, float amount, float duration)
        {
            if (t == null) yield break;
            Vector3 baseScale = t.localScale;
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                if (t == null) yield break;
                float k = time / duration;
                float s = 1f + Mathf.Sin(k * Mathf.PI) * amount * (1f - k);
                t.localScale = baseScale * s;
                yield return null;
            }
            if (t != null) t.localScale = baseScale;
        }
    }
}
