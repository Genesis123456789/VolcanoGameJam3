using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Inspecteur de PD_GameConfig : affiche, sous les réglages, la table des probabilités de génération
    /// (sortie / boss) calculée à partir des courbes, pour régler les proportions en connaissance de cause.
    /// </summary>
    [CustomEditor(typeof(GameConfig))]
    public class GameConfigEditor : Editor
    {
        private bool showOdds = true;
        private int dungeon;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var cfg = (GameConfig)target;

            EditorGUILayout.Space(12);
            showOdds = EditorGUILayout.Foldout(showOdds, "Simulation : sortie et boss selon les salles parcourues", true, EditorStyles.foldoutHeader);
            if (!showOdds) return;
            dungeon = EditorGUILayout.IntSlider("Donjon n°", dungeon + 1, 1, 25) - 1;
            EditorGUILayout.HelpBox(
                "« Chance » = probabilité que la prochaine nouvelle salle soit la sortie / un boss.\n" +
                "« Sortie ici » = probabilité de tomber sur la sortie à cette salle précise (en tenant compte des salles précédentes).",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Salles parcourues", EditorStyles.miniBoldLabel, GUILayout.Width(110));
                GUILayout.Label("Sortie : chance", EditorStyles.miniBoldLabel, GUILayout.Width(95));
                GUILayout.Label("Sortie ici", EditorStyles.miniBoldLabel, GUILayout.Width(70));
                GUILayout.Label("Cumulé", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                GUILayout.Label("Boss : chance", EditorStyles.miniBoldLabel, GUILayout.Width(90));
            }
            float notYet = 1f, cumulative = 0f, expected = 0f;
            int max = cfg.maxRoomsBeforeExit + Mathf.FloorToInt(dungeon * cfg.extraRoomsPerDungeon) + 1;
            for (int n = 0; n <= max; n++)
            {
                float pExit = cfg.ExitChance(n, dungeon);
                float here = notYet * pExit;
                cumulative += here;
                expected += here * (n + 1);
                notYet *= 1f - pExit;
                float pBoss = n >= 1 ? (1f - pExit) * cfg.BossChance(n, dungeon) : 0f;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(n.ToString(), GUILayout.Width(110));
                    GUILayout.Label($"{pExit * 100f:0}%", GUILayout.Width(95));
                    GUILayout.Label($"{here * 100f:0.0}%", GUILayout.Width(70));
                    GUILayout.Label($"{cumulative * 100f:0}%", GUILayout.Width(60));
                    GUILayout.Label($"{pBoss * 100f:0}%", GUILayout.Width(90));
                }
                if (cumulative >= 0.999f) break;
            }
            EditorGUILayout.LabelField($"Nouvelles salles avant la sortie (moyenne) : {expected:0.0}", EditorStyles.boldLabel);
        }
    }
}
