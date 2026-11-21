using TMPro;
using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Prefabs d'effets (systèmes de particules Unity), projectiles, butin, chiffres de dégâts.</summary>
    public static class PDVfx
    {
        private static Mesh CubeMesh => Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        private static Mesh SphereMesh => Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        private static Mesh CylinderMesh => Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        private static Mesh QuadMesh => Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        private class PS
        {
            public string name;
            public int burst = 10;
            public float rate;
            public Vector2 life = new Vector2(0.3f, 0.6f);
            public Vector2 speed = new Vector2(2f, 4f);
            public Vector2 size = new Vector2(0.1f, 0.2f);
            public Color colorA = Color.white, colorB = Color.white;
            public float gravity;
            public ParticleSystemShapeType shape = ParticleSystemShapeType.Sphere;
            public float shapeAngle = 25f, shapeRadius = 0.1f;
            public Vector3 shapeRotation;
            public Material material;
            public Mesh mesh;
            public bool stretch;
            public bool growSize;
            public bool loop;
            public bool collide;
            public float duration = 0.5f;
            public bool rotate;
            public bool local;
        }

        private static ParticleSystem Make(PS s, Transform parent = null)
        {
            var go = new GameObject(s.name);
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = s.duration;
            main.loop = s.loop;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(s.life.x, s.life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(s.speed.x, s.speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(s.size.x, s.size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(s.colorA, s.colorB);
            main.gravityModifier = s.gravity;
            main.simulationSpace = s.local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.useUnscaledTime = false;
            main.maxParticles = 200;
            if (s.rotate) main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            if (s.mesh != null && s.rotate)
            {
                main.startRotation3D = true;
                main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            }

            var em = ps.emission;
            em.rateOverTime = s.rate;
            if (s.burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)s.burst) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = s.shape;
            shape.angle = s.shapeAngle;
            shape.radius = s.shapeRadius;
            shape.rotation = s.shapeRotation;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, s.growSize ? AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f) : AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            if (s.rotate && s.mesh != null)
            {
                var rol = ps.rotationOverLifetime;
                rol.enabled = true;
                rol.separateAxes = true;
                rol.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
                rol.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
                rol.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }

            if (s.collide)
            {
                var c = ps.collision;
                c.enabled = true;
                c.type = ParticleSystemCollisionType.World;
                c.mode = ParticleSystemCollisionMode.Collision3D;
                c.dampen = 0.4f;
                c.bounce = 0.35f;
                c.quality = ParticleSystemCollisionQuality.Low;
                c.collidesWith = 1; // Default
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = s.material;
            if (s.mesh != null)
            {
                r.renderMode = ParticleSystemRenderMode.Mesh;
                r.mesh = s.mesh;
                r.alignment = s.local ? ParticleSystemRenderSpace.Local : ParticleSystemRenderSpace.World;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else if (s.stretch)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.06f;
                r.lengthScale = 1.5f;
            }
            else r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private static T SavePrefab<T>(GameObject go, string name) where T : Component
        {
            PDPaths.EnsureFolder(PDPaths.Vfx);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, PDPaths.Vfx + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return saved != null ? saved.GetComponent<T>() : null;
        }

        private static ParticleSystem SavePS(PS s) => SavePrefab<ParticleSystem>(Make(s).gameObject, s.name);

        public static VfxLibrary BuildVfx(PDAssets.MaterialSet m, Mesh arc)
        {
            var lib = PDPaths.LoadOrCreate<VfxLibrary>(PDPaths.Data + "/PD_VfxLibrary.asset");

            lib.hitSpark = SavePS(new PS { name = "VFX_HitSpark", burst = 14, life = new Vector2(0.15f, 0.3f), speed = new Vector2(4f, 9f), size = new Vector2(0.06f, 0.14f), colorA = new Color(1f, 0.9f, 0.6f), colorB = new Color(1f, 0.6f, 0.25f), shape = ParticleSystemShapeType.Cone, shapeAngle = 40f, material = m.particleAdd, stretch = true, gravity = 0.6f });
            lib.woodChips = SavePS(new PS { name = "VFX_WoodChips", burst = 22, life = new Vector2(0.8f, 1.4f), speed = new Vector2(3f, 7f), size = new Vector2(0.06f, 0.16f), colorA = new Color(0.85f, 0.65f, 0.42f), colorB = new Color(0.55f, 0.38f, 0.24f), gravity = 2.2f, shape = ParticleSystemShapeType.Sphere, shapeRadius = 0.2f, material = m.chips, mesh = CubeMesh, rotate = true, collide = true, duration = 1.5f });
            lib.dust = SavePS(new PS { name = "VFX_Dust", burst = 7, life = new Vector2(0.4f, 0.7f), speed = new Vector2(0.4f, 1.4f), size = new Vector2(0.25f, 0.5f), colorA = new Color(0.75f, 0.68f, 0.6f, 0.5f), colorB = new Color(0.6f, 0.55f, 0.5f, 0.35f), shape = ParticleSystemShapeType.Hemisphere, shapeRadius = 0.25f, material = m.particle, growSize = true, gravity = -0.05f });
            lib.doorSlam = SavePS(new PS { name = "VFX_DoorSlam", burst = 26, life = new Vector2(0.5f, 0.9f), speed = new Vector2(1.5f, 3.2f), size = new Vector2(0.35f, 0.7f), colorA = new Color(0.7f, 0.64f, 0.58f, 0.55f), colorB = new Color(0.5f, 0.45f, 0.42f, 0.4f), shape = ParticleSystemShapeType.Circle, shapeRadius = 0.8f, shapeRotation = new Vector3(90f, 0f, 0f), material = m.particle, growSize = true, gravity = -0.03f });
            lib.lootBurst = SavePS(new PS { name = "VFX_LootBurst", burst = 30, life = new Vector2(0.4f, 0.9f), speed = new Vector2(1.5f, 4.5f), size = new Vector2(0.06f, 0.15f), colorA = Color.white, colorB = new Color(1f, 0.9f, 0.6f), shape = ParticleSystemShapeType.Sphere, shapeRadius = 0.2f, material = m.particleAdd, gravity = -0.2f });
            lib.healPuff = SavePS(new PS { name = "VFX_Heal", burst = 22, life = new Vector2(0.6f, 1.1f), speed = new Vector2(0.6f, 2f), size = new Vector2(0.08f, 0.18f), colorA = new Color(0.55f, 1f, 0.5f), colorB = new Color(0.9f, 1f, 0.6f), shape = ParticleSystemShapeType.Circle, shapeRadius = 0.6f, shapeRotation = new Vector3(90f, 0f, 0f), material = m.particleAdd, gravity = -0.4f });
            lib.splinterNova = SavePS(new PS { name = "VFX_SplinterNova", burst = 36, life = new Vector2(0.35f, 0.6f), speed = new Vector2(7f, 11f), size = new Vector2(0.05f, 0.12f), colorA = new Color(0.9f, 0.7f, 0.45f), colorB = new Color(1f, 0.85f, 0.5f), shape = ParticleSystemShapeType.Circle, shapeRadius = 0.2f, shapeRotation = new Vector3(90f, 0f, 0f), material = m.chips, mesh = CubeMesh, rotate = true, gravity = 0.5f });

            // Traînée d'arme : un arc qui s'étire puis s'efface (une seule particule maillée, repère local)
            var swing = new PS { name = "VFX_Swing", burst = 1, life = new Vector2(0.16f, 0.16f), speed = Vector2.zero, size = new Vector2(1f, 1f), colorA = new Color(1f, 0.95f, 0.85f, 0.9f), colorB = new Color(1f, 0.95f, 0.85f, 0.9f), shape = ParticleSystemShapeType.Sphere, shapeRadius = 0.001f, material = m.trail, mesh = arc, local = true, growSize = true, duration = 0.2f };
            var swingPs = Make(swing);
            var sh = swingPs.shape; sh.enabled = false;
            var main = swingPs.main; main.startRotation3D = false;
            lib.swingTrail = SavePrefab<ParticleSystem>(swingPs.gameObject, swing.name);

            lib.telegraphCircle = BuildTelegraph(m);
            lib.safeZoneRing = BuildSafeRing(m);
            lib.alertMark = BuildAlertMark();
            lib.damageNumber = BuildDamageNumber();
            EditorUtility.SetDirty(lib);
            return lib;
        }

        private static GameObject BuildTelegraph(PDAssets.MaterialSet m)
        {
            var root = new GameObject("VFX_Telegraph");
            var outline = new GameObject("Contour");
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = new Vector3(1f, 0.005f, 1f);
            outline.AddComponent<MeshFilter>().sharedMesh = CylinderMesh;
            var or = outline.AddComponent<MeshRenderer>();
            or.sharedMaterial = m.telegraph;
            or.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fillRoot = new GameObject("Remplissage").transform;
            fillRoot.SetParent(root.transform, false);
            var fill = new GameObject("Disque");
            fill.transform.SetParent(fillRoot, false);
            fill.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            fill.transform.localScale = new Vector3(1f, 0.005f, 1f);
            fill.AddComponent<MeshFilter>().sharedMesh = CylinderMesh;
            var fr = fill.AddComponent<MeshRenderer>();
            fr.sharedMaterial = m.telegraphFill;
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var tc = root.AddComponent<TelegraphCircle>();
            tc.fill = fillRoot;
            tc.outline = or;
            PDPaths.EnsureFolder(PDPaths.Vfx);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PDPaths.Vfx + "/VFX_Telegraph.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildSafeRing(PDAssets.MaterialSet m)
        {
            var root = new GameObject("VFX_SafeZone");
            var quad = new GameObject("Anneau");
            quad.transform.SetParent(root.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.AddComponent<MeshFilter>().sharedMesh = QuadMesh;
            var r = quad.AddComponent<MeshRenderer>();
            r.sharedMaterial = m.safeRing;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.AddComponent<GroundRing>();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PDPaths.Vfx + "/VFX_SafeZone.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildAlertMark()
        {
            var root = new GameObject("VFX_Alert");
            var text = root.AddComponent<TextMeshPro>();
            text.text = "!";
            text.fontSize = 9f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.85f, 0.2f);
            if (UIBuilder.Font != null) text.font = UIBuilder.Font;
            if (UIBuilder.OutlineMaterial != null) text.fontSharedMaterial = UIBuilder.OutlineMaterial;
            text.rectTransform.sizeDelta = new Vector2(2f, 2f);
            root.AddComponent<AlwaysFaceCamera>();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PDPaths.Vfx + "/VFX_Alert.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }

        private static DamageNumber BuildDamageNumber()
        {
            var root = new GameObject("VFX_DamageNumber");
            var text = root.AddComponent<TextMeshPro>();
            text.text = "12";
            text.fontSize = 5f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            if (UIBuilder.Font != null) text.font = UIBuilder.Font;
            if (UIBuilder.OutlineMaterial != null) text.fontSharedMaterial = UIBuilder.OutlineMaterial;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(6f, 2f);
            var dn = root.AddComponent<DamageNumber>();
            dn.label = text;
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PDPaths.Vfx + "/VFX_DamageNumber.prefab");
            Object.DestroyImmediate(root);
            return saved.GetComponent<DamageNumber>();
        }

        // ------------------------------------------------------------------ Projectiles et butin

        public static Projectile BuildBolt(PDAssets.MaterialSet m)
        {
            var root = new GameObject("PD_PlayerBolt");
            var body = new GameObject("Carreau");
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.07f, 0.07f, 0.55f);
            body.AddComponent<MeshFilter>().sharedMesh = CubeMesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = m.bolt;
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.startWidth = 0.12f;
            trail.endWidth = 0f;
            trail.sharedMaterial = m.trail;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var p = root.AddComponent<Projectile>();
            p.radius = 0.2f;
            p.lifetime = 1.4f;
            p.hitColor = new Color(1f, 0.85f, 0.5f);
            PDPaths.EnsureFolder(PDPaths.Prefabs);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PDPaths.Prefabs + "/PD_PlayerBolt.prefab");
            Object.DestroyImmediate(root);
            return saved.GetComponent<Projectile>();
        }

        public static Projectile BuildOrb(PDAssets.MaterialSet m)
        {
            var root = new GameObject("PD_EnemyOrb");
            var body = new GameObject("Orbe");
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * 0.32f;
            body.AddComponent<MeshFilter>().sharedMesh = SphereMesh;
            var mr = body.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m.orb;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.26f;
            trail.endWidth = 0f;
            trail.sharedMaterial = m.trailEnemy;
            var lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(root.transform, false);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.8f, 0.4f, 1f);
            l.range = 2.2f;
            l.intensity = 1.5f;
            l.shadows = LightShadows.None;
            var p = root.AddComponent<Projectile>();
            p.radius = 0.2f;
            p.lifetime = 3.5f;
            p.hitColor = new Color(0.8f, 0.45f, 1f);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PDPaths.Prefabs + "/PD_EnemyOrb.prefab");
            Object.DestroyImmediate(root);
            return saved.GetComponent<Projectile>();
        }

        public static LootPickup BuildLoot(PDAssets.MaterialSet m)
        {
            var root = new GameObject("PD_LootPickup");
            var spinner = new GameObject("Toupie").transform;
            spinner.SetParent(root.transform, false);
            spinner.localPosition = new Vector3(0f, 0.75f, 0f);
            var gem = new GameObject("Gemme");
            gem.transform.SetParent(root.transform, false);
            gem.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            gem.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);
            gem.AddComponent<MeshFilter>().sharedMesh = CylinderMesh;
            var gr = gem.AddComponent<MeshRenderer>();
            gr.sharedMaterial = m.gem;
            gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var beam = Make(new PS { name = "Faisceau", burst = 0, rate = 10f, loop = true, duration = 1f, life = new Vector2(0.8f, 1.4f), speed = new Vector2(0.8f, 1.8f), size = new Vector2(0.05f, 0.12f), shape = ParticleSystemShapeType.Cone, shapeAngle = 3f, shapeRadius = 0.35f, shapeRotation = new Vector3(-90f, 0f, 0f), material = m.particleAdd, gravity = -0.1f }, root.transform);

            var lightGo = new GameObject("Lumière");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 3f;
            l.intensity = 2f;
            l.shadows = LightShadows.None;

            var lp = root.AddComponent<LootPickup>();
            lp.spinner = spinner;
            lp.beamLight = l;
            lp.beam = beam;
            lp.gem = gr;
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PDPaths.Prefabs + "/PD_LootPickup.prefab");
            Object.DestroyImmediate(root);
            return saved.GetComponent<LootPickup>();
        }
    }
}
