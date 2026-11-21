using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Textures générées, matériaux, maillage d'arc, Animator Controller et clip d'idle.</summary>
    public static class PDAssets
    {
        // ------------------------------------------------------------------ Textures

        public static Texture2D SoftCircle => AssetDatabase.LoadAssetAtPath<Texture2D>(PDPaths.Textures + "/PD_SoftCircle.png");
        public static Texture2D Ring => AssetDatabase.LoadAssetAtPath<Texture2D>(PDPaths.Textures + "/PD_Ring.png");
        public static Sprite White => AssetDatabase.LoadAssetAtPath<Sprite>(PDPaths.Textures + "/PD_White.png");
        public static Sprite Circle => AssetDatabase.LoadAssetAtPath<Sprite>(PDPaths.Textures + "/PD_CircleSprite.png");
        public static Sprite Gradient => AssetDatabase.LoadAssetAtPath<Sprite>(PDPaths.Textures + "/PD_Gradient.png");

        public static void CreateTextures()
        {
            WriteTexture("PD_SoftCircle.png", 128, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 64f;
                float a = Mathf.Clamp01(1f - d);
                return new Color(1, 1, 1, a * a);
            }, false);
            WriteTexture("PD_Ring.png", 256, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(127.5f, 127.5f)) / 128f;
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.08f);
                float fill = d < 0.9f ? 0.12f * d : 0f;
                return new Color(1, 1, 1, Mathf.Clamp01(ring + fill));
            }, false);
            WriteTexture("PD_White.png", 8, (x, y) => Color.white, true);
            WriteTexture("PD_CircleSprite.png", 128, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                return new Color(1, 1, 1, Mathf.Clamp01(63f - d));
            }, true);
            WriteTexture("PD_Gradient.png", 64, (x, y) => new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(y - 31.5f) / 32f)), true);
        }

        private static void WriteTexture(string file, int size, System.Func<int, int, Color> f, bool sprite)
        {
            PDPaths.EnsureFolder(PDPaths.Textures);
            string path = PDPaths.Textures + "/" + file;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, f(x, y));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = !sprite;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            if (sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(s);
            }
            ti.SaveAndReimport();
        }

        // ------------------------------------------------------------------ Matériaux

        public static bool IsURP => UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null;

        public static Shader Lit => Shader.Find(IsURP ? "Universal Render Pipeline/Lit" : "Standard") ?? Shader.Find("Standard");
        public static Shader Unlit => Shader.Find(IsURP ? "Universal Render Pipeline/Unlit" : "Unlit/Transparent") ?? Shader.Find("Unlit/Color");
        public static Shader ParticleShader => Shader.Find(IsURP ? "Universal Render Pipeline/Particles/Unlit" : "Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");

        public static Material LitMat(string name, Color color, float smoothness = 0.25f, float emission = 0f)
        {
            string path = PDPaths.Materials + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Lit);
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = Lit;
            SetColor(m, color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * emission);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material TransparentMat(string name, Color color, Texture tex, bool additive, bool particle)
        {
            string path = PDPaths.Materials + "/" + name + ".mat";
            var shader = particle ? ParticleShader : Unlit;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = shader;
            SetColor(m, color);
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (IsURP)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHATEST_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public class MaterialSet
        {
            public Material wood, woodDark, partWood, gem, bolt, orb, particle, particleAdd, telegraph, telegraphFill, safeRing, trail, trailEnemy, chips;
            public Material floor, wall, wallTop, prop;
        }

        public static MaterialSet CreateMaterials()
        {
            PDPaths.EnsureFolder(PDPaths.Materials);
            var s = new MaterialSet
            {
                wood = LitMat("PD_Wood", new Color(1f, 1f, 1f), 0.3f),
                woodDark = LitMat("PD_WoodCorrupted", new Color(0.16f, 0.13f, 0.15f), 0.45f),
                partWood = LitMat("PD_PartWood", new Color(0.78f, 0.6f, 0.42f), 0.35f),
                gem = LitMat("PD_Gem", Color.white, 0.8f, 1.5f),
                bolt = LitMat("PD_Bolt", new Color(1f, 0.85f, 0.55f), 0.5f, 2f),
                orb = LitMat("PD_Orb", new Color(0.75f, 0.3f, 1f), 0.5f, 3f),
                chips = LitMat("PD_Chips", new Color(0.7f, 0.52f, 0.34f), 0.2f),
                floor = LitMat("PD_Floor", Color.white, 0.15f),
                wall = LitMat("PD_Wall", Color.white, 0.1f),
                wallTop = LitMat("PD_WallTop", Color.white, 0.25f),
                prop = LitMat("PD_Prop", Color.white, 0.3f),
                particle = TransparentMat("PD_Particle", Color.white, SoftCircle, false, true),
                particleAdd = TransparentMat("PD_ParticleAdditive", Color.white, SoftCircle, true, true),
                telegraph = TransparentMat("PD_Telegraph", new Color(1f, 0.2f, 0.15f, 0.35f), null, false, false),
                telegraphFill = TransparentMat("PD_TelegraphFill", new Color(1f, 0.25f, 0.15f, 0.45f), null, true, false),
                safeRing = TransparentMat("PD_SafeRing", new Color(0.55f, 0.85f, 1f, 0.8f), Ring, true, false),
                trail = TransparentMat("PD_Trail", new Color(1f, 0.85f, 0.5f, 0.8f), SoftCircle, true, true),
                trailEnemy = TransparentMat("PD_TrailEnemy", new Color(0.8f, 0.35f, 1f, 0.8f), SoftCircle, true, true),
            };
            // Bois du pantin : on garde la texture/couleur importée si elle existe, sinon bois clair
            SetColor(s.wood, new Color(0.88f, 0.72f, 0.52f));
            AssetDatabase.SaveAssets();
            return s;
        }

        /// <summary>Convertit les matériaux Standard du prototype vers URP/Lit (sinon ils deviennent roses).</summary>
        public static void UpgradeLegacyMaterials()
        {
            if (!IsURP) return;
            // Les FBX réimportés génèrent leurs matériaux avec le shader du pipeline actif (URP/Lit).
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Projet" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Projet" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null || m.shader.name != "Standard") continue;
                Color c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                Texture t = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                m.shader = Lit;
                SetColor(m, c);
                if (t != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
                EditorUtility.SetDirty(m);
            }
        }

        // ------------------------------------------------------------------ Maillage d'arc (traînée d'attaque)

        public static Mesh CreateArcMesh()
        {
            PDPaths.EnsureFolder(PDPaths.Meshes);
            string path = PDPaths.Meshes + "/PD_SwingArc.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create) mesh = new Mesh { name = "PD_SwingArc" };
            mesh.Clear();
            const int seg = 24;
            var verts = new Vector3[(seg + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var cols = new Color[verts.Length];
            var tris = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float k = i / (float)seg;
                float a = Mathf.Lerp(-75f, 75f, k) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                verts[i * 2] = dir * 0.55f;
                verts[i * 2 + 1] = dir * 1f;
                uvs[i * 2] = new Vector2(k, 0f);
                uvs[i * 2 + 1] = new Vector2(k, 1f);
                float alpha = Mathf.Sin(k * Mathf.PI) * Mathf.Lerp(0.3f, 1f, k);
                cols[i * 2] = new Color(1, 1, 1, 0f);
                cols[i * 2 + 1] = new Color(1, 1, 1, alpha);
            }
            for (int i = 0; i < seg; i++)
            {
                int v = i * 2, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.colors = cols;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (create) AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // ------------------------------------------------------------------ Animation

        public static AnimationClip CreateIdleClip(AnimationClip walk)
        {
            PDPaths.EnsureFolder(PDPaths.Animation);
            string path = PDPaths.Animation + "/PD_Idle.anim";
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool create = idle == null;
            if (create) idle = new AnimationClip { name = "PD_Idle" };
            idle.ClearCurves();
            if (walk == null) { if (create) AssetDatabase.CreateAsset(idle, path); return idle; }

            var bindings = AnimationUtility.GetCurveBindings(walk);
            // Instant où les jambes sont le plus rapprochées : la pose la plus « debout ».
            var left = bindings.FirstOrDefault(b => b.propertyName.Contains("Left Upper Leg Front-Back"));
            var right = bindings.FirstOrDefault(b => b.propertyName.Contains("Right Upper Leg Front-Back"));
            float bestT = 0f;
            if (!string.IsNullOrEmpty(left.propertyName) && !string.IsNullOrEmpty(right.propertyName))
            {
                var lc = AnimationUtility.GetEditorCurve(walk, left);
                var rc = AnimationUtility.GetEditorCurve(walk, right);
                float best = float.MaxValue;
                for (float t = 0f; t <= walk.length; t += walk.length / 60f)
                {
                    float d = Mathf.Abs(lc.Evaluate(t) - rc.Evaluate(t));
                    if (d < best) { best = d; bestT = t; }
                }
            }

            const float len = 2.4f;
            foreach (var b in bindings)
            {
                var src = AnimationUtility.GetEditorCurve(walk, b);
                if (src == null) continue;
                float v = src.Evaluate(bestT);
                AnimationCurve curve;
                string n = b.propertyName;
                if (n.Contains("Spine Front-Back") || n.Contains("Chest Front-Back"))
                    curve = new AnimationCurve(new Keyframe(0f, v), new Keyframe(len * 0.5f, v + 0.06f), new Keyframe(len, v));
                else if (n.Contains("Head Nod Down-Up"))
                    curve = new AnimationCurve(new Keyframe(0f, v), new Keyframe(len * 0.5f, v - 0.05f), new Keyframe(len, v));
                else if (n.Contains("Arm Down-Up"))
                    curve = new AnimationCurve(new Keyframe(0f, v - 0.05f), new Keyframe(len * 0.5f, v - 0.02f), new Keyframe(len, v - 0.05f));
                else
                    curve = new AnimationCurve(new Keyframe(0f, v), new Keyframe(len, v));
                AnimationUtility.SetEditorCurve(idle, b, curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(idle);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idle, settings);
            if (create) AssetDatabase.CreateAsset(idle, path);
            EditorUtility.SetDirty(idle);
            return idle;
        }

        public static AvatarMask CreateUpperBodyMask()
        {
            string path = PDPaths.Animation + "/PD_UpperBody.mask";
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, path);
            }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                var part = (AvatarMaskBodyPart)i;
                bool lower = part == AvatarMaskBodyPart.Root || part == AvatarMaskBodyPart.LeftLeg || part == AvatarMaskBodyPart.RightLeg
                             || part == AvatarMaskBodyPart.LeftFootIK || part == AvatarMaskBodyPart.RightFootIK;
                mask.SetHumanoidBodyPartActive(part, !lower);
            }
            EditorUtility.SetDirty(mask);
            return mask;
        }

        /// <summary>
        /// Animator du pantin (joueur, villageois, ennemis) :
        /// calque de base = blend tree Idle/Marche/Course sur « Speed »,
        /// calque « Haut du corps » = attaque jouable en courant (masque d'avatar).
        /// </summary>
        public static AnimatorController CreateAnimator(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip attack)
        {
            string path = PDPaths.Animation + "/PD_Puppet.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter(new AnimatorControllerParameter { name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

            var sm = ctrl.layers[0].stateMachine;
            var tree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, 0.35f);
            if (run != null) tree.AddChild(run, 1f);
            var loco = sm.AddState("Locomotion");
            loco.motion = tree;
            sm.defaultState = loco;

            if (attack != null)
            {
                var mask = CreateUpperBodyMask();
                ctrl.AddLayer("Attaque");
                var layers = ctrl.layers;
                var layer = layers[1];
                layer.defaultWeight = 1f;
                layer.avatarMask = mask;
                layer.blendingMode = AnimatorLayerBlendingMode.Override;
                var asm = layer.stateMachine;
                var empty = asm.AddState("Vide");
                // Write Defaults désactivé : l'état vide laisse passer la marche/course du calque de base
                // (sinon il forcerait le haut du corps dans la pose par défaut de l'avatar).
                empty.writeDefaultValues = false;
                asm.defaultState = empty;
                var atk = asm.AddState("Attaque");
                atk.writeDefaultValues = false;
                atk.motion = attack;
                atk.speedParameterActive = true;
                atk.speedParameter = "AttackSpeed";
                var toAtk = asm.AddAnyStateTransition(atk);
                toAtk.hasExitTime = false;
                toAtk.duration = 0.05f;
                toAtk.canTransitionToSelf = true;
                toAtk.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
                var back = atk.AddTransition(empty);
                back.hasExitTime = true;
                back.exitTime = 0.9f;
                back.duration = 0.12f;
                ctrl.layers = layers;
            }
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }
    }
}
