using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Assistant « Créer une pièce depuis un modèle » : choisissez un FBX/prefab modélisé autour du
    /// pantin de base, un emplacement, un nom et une histoire. L'outil accroche automatiquement chaque
    /// maillage à l'os le plus proche, crée le visuel, la définition, et l'ajoute à la base de pièces.
    /// </summary>
    public class PartWizard : EditorWindow
    {
        private GameObject model;
        private PuppetSlot slot = PuppetSlot.RightArm;
        private string displayName = "Nouvelle pièce";
        private AttackStyle style = AttackStyle.Fist;
        private Rarity minRarity = Rarity.Common;
        private int minDungeon;
        private string lore = "";
        private bool allowMirror = true;
        private List<StatRange> stats = new List<StatRange> { new StatRange(StatType.MaxHealth, ModifierMode.Flat, 6, 10) };
        private List<PassiveKind> passives = new List<PassiveKind>();
        private HashSet<PuppetSlot> detected;
        private Vector2 scroll;

        [MenuItem("Tools/Puppet Dungeon/Créer une pièce depuis un modèle…", priority = 10)]
        public static void Open()
        {
            var w = GetWindow<PartWizard>(true, "Nouvelle pièce de pantin");
            w.minSize = new Vector2(460f, 560f);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("Le modèle doit être modélisé dans le même repère que le pantin de base (rig_ff002). " +
                                    "Chaque maillage est accroché automatiquement à l'os le plus proche.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            model = (GameObject)EditorGUILayout.ObjectField("Modèle (FBX / prefab)", model, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && model != null)
            {
                detected = PDRigTools.DetectSlots(model);
                if (detected.Count > 0) slot = detected.First();
                if (displayName == "Nouvelle pièce") displayName = ObjectNames.NicifyVariableName(model.name);
            }
            if (detected != null && detected.Count > 0)
                EditorGUILayout.LabelField("Emplacements détectés", string.Join(", ", detected.Select(EnumUtil.SlotLabel)));

            displayName = EditorGUILayout.TextField("Nom", displayName);
            slot = (PuppetSlot)EditorGUILayout.EnumPopup("Emplacement", slot);
            allowMirror = EditorGUILayout.Toggle(new GUIContent("Symétrie auto", "Si le modèle est de l'autre côté du corps, il est mis en miroir."), allowMirror);
            using (new EditorGUI.DisabledScope(slot != PuppetSlot.RightArm))
                style = (AttackStyle)EditorGUILayout.EnumPopup("Style d'attaque", style);
            minRarity = (Rarity)EditorGUILayout.EnumPopup("Rareté minimale", minRarity);
            minDungeon = EditorGUILayout.IntField("Donjon minimum", minDungeon);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stats de base (niveau 1, Commun)", EditorStyles.boldLabel);
            for (int i = 0; i < stats.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var s = stats[i];
                s.stat = (StatType)EditorGUILayout.EnumPopup(s.stat, GUILayout.Width(150f));
                s.mode = (ModifierMode)EditorGUILayout.EnumPopup(s.mode, GUILayout.Width(70f));
                s.min = EditorGUILayout.FloatField(s.min);
                s.max = EditorGUILayout.FloatField(s.max);
                stats[i] = s;
                if (GUILayout.Button("−", GUILayout.Width(24f))) { stats.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Stat")) stats.Add(new StatRange(StatType.Damage, ModifierMode.Flat, 1, 2));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Passifs possibles", EditorStyles.boldLabel);
            for (int i = 0; i < passives.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                passives[i] = (PassiveKind)EditorGUILayout.EnumPopup(passives[i]);
                if (GUILayout.Button("−", GUILayout.Width(24f))) { passives.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Passif")) passives.Add(PassiveKind.Sap);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Histoire", EditorStyles.boldLabel);
            lore = EditorGUILayout.TextArea(lore, GUILayout.MinHeight(80f));
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(model == null || string.IsNullOrWhiteSpace(displayName)))
            {
                if (GUILayout.Button("Créer la pièce", GUILayout.Height(36f))) Create();
            }
        }

        private void Create()
        {
            string id = new string(displayName.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(id)) id = "Piece";
            id += "_" + slot;
            var wood = AssetDatabase.LoadAssetAtPath<Material>(PDPaths.Materials + "/PD_PartWood.mat");
            var visual = PDRigTools.BuildPartVisual(model, slot, false, "PV_" + id, wood);
            if (visual == null && allowMirror) visual = PDRigTools.BuildPartVisual(model, slot, true, "PV_" + id, wood);
            if (visual == null)
            {
                EditorUtility.DisplayDialog("Nouvelle pièce", $"Aucun maillage du modèle ne se trouve sur l'emplacement « {EnumUtil.SlotLabel(slot)} ».", "OK");
                return;
            }
            PDPaths.EnsureFolder(PDPaths.Parts);
            string path = AssetDatabase.GenerateUniqueAssetPath(PDPaths.Parts + "/PD_Part_" + id + ".asset");
            var def = CreateInstance<PartDefinition>();
            def.displayName = displayName;
            def.slot = slot;
            def.attackStyle = slot == PuppetSlot.RightArm ? style : AttackStyle.Fist;
            def.minRarity = minRarity;
            def.minDungeon = minDungeon;
            def.baseStats = new List<StatRange>(stats);
            def.possiblePassives = new List<PassiveKind>(passives);
            def.lore = lore;
            def.visualPrefab = visual;
            AssetDatabase.CreateAsset(def, path);
            PDContent.RefreshDatabase();
            AssetDatabase.SaveAssets();
            Selection.activeObject = def;
            EditorGUIUtility.PingObject(def);
            Debug.Log($"[PuppetDungeon] Pièce créée : {path}");
        }
    }

    /// <summary>Ajoute automatiquement toute nouvelle PartDefinition à la base de pièces.</summary>
    public class PartDatabaseAutoRegister : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touched = false;
            foreach (var p in imported)
            {
                if (!p.EndsWith(".asset")) continue;
                if (AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(PartDefinition)) { touched = true; break; }
            }
            if (!touched && deleted.Length == 0) return;
            var dbPath = PDPaths.Content + "/PD_PartDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<PartDatabase>(dbPath);
            if (db == null) return;
            EditorApplication.delayCall += () =>
            {
                if (db == null) return;
                int before = db.parts.Count;
                db.parts = AssetDatabase.FindAssets("t:PartDefinition")
                    .Select(g => AssetDatabase.LoadAssetAtPath<PartDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(x => x != null).OrderBy(x => x.slot).ThenBy(x => x.displayName).ToList();
                if (db.parts.Count != before) { EditorUtility.SetDirty(db); AssetDatabase.SaveAssetIfDirty(db); }
            };
        }
    }
}
