using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Projectile (carreau du joueur ou orbe ennemie). Déplacement + SphereCast anti-traversée.</summary>
    public class Projectile : MonoBehaviour
    {
        public float radius = 0.18f;
        public float lifetime = 3f;
        public Color hitColor = new Color(1f, 0.8f, 0.4f);

        private Vector3 velocity;
        private bool fromPlayer;
        private float damage;
        private float knockback;
        private PlayerCombat owner;
        private GameObject source;
        private float dieAt;
        private bool dead;
        private int pierceLeft;

        public void LaunchFromPlayer(PlayerCombat combat, Vector3 dir, float speed)
        {
            fromPlayer = true;
            owner = combat;
            source = combat.gameObject;
            velocity = dir.normalized * speed;
            pierceLeft = 0;
            dieAt = Time.time + lifetime;
            gameObject.layer = Layers.Projectile;
        }

        public void LaunchFromEnemy(GameObject shooter, Vector3 dir, float speed, float dmg)
        {
            fromPlayer = false;
            source = shooter;
            velocity = dir.normalized * speed;
            damage = dmg;
            knockback = 3f;
            dieAt = Time.time + lifetime;
            gameObject.layer = Layers.Projectile;
        }

        private void Update()
        {
            if (dead) return;
            if (Time.time > dieAt) { Die(false); return; }
            float dist = velocity.magnitude * Time.deltaTime;
            Vector3 dir = velocity.normalized;
            var hits = Physics.SphereCastAll(transform.position, radius, dir, dist, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (source != null && h.collider.transform.IsChildOf(source.transform)) continue;
                if (h.collider.gameObject.layer == Layers.Debris || h.collider.gameObject.layer == Layers.Projectile) continue;

                if (fromPlayer)
                {
                    var enemy = h.collider.GetComponentInParent<EnemyController>();
                    if (enemy != null)
                    {
                        if (enemy.Health.IsDead) continue;
                        owner.DealDamage(enemy, owner.crossbow.damageMul, 1f, dir, 0.1f);
                        Juice.Spawn(v => v.hitSpark, h.point == Vector3.zero ? transform.position : h.point, Quaternion.LookRotation(-dir), hitColor);
                        if (pierceLeft-- > 0) continue;
                        Die(true);
                        return;
                    }
                    if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
                }
                else
                {
                    var player = h.collider.GetComponentInParent<PlayerController>();
                    if (player != null)
                    {
                        if (player.Health.Invulnerable) continue;
                        player.Health.TakeDamage(new DamageInfo { amount = damage, point = transform.position, direction = dir, knockback = knockback, source = source });
                        Die(true);
                        return;
                    }
                    if (h.collider.GetComponentInParent<EnemyController>() != null) continue;
                }
                // Mur / décor
                transform.position = h.point == Vector3.zero ? transform.position : h.point - dir * radius;
                Die(true);
                return;
            }
            transform.position += dir * dist;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private void Die(bool impact)
        {
            dead = true;
            if (impact) Juice.Spawn(v => v.hitSpark, transform.position, Quaternion.identity, hitColor, 0.7f);
            foreach (var tr in GetComponentsInChildren<TrailRenderer>()) { tr.transform.SetParent(null, true); tr.autodestruct = true; tr.emitting = false; }
            Destroy(gameObject);
        }
    }
}
