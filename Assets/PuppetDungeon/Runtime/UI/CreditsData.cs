using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Crédits du jeu, modifiables dans l'inspecteur (Assets/PuppetDungeon/Content/PD_Credits.asset).
    /// Ajoutez autant de sections et de lignes que nécessaire ; le texte riche TextMeshPro est accepté.
    /// </summary>
    [CreateAssetMenu(fileName = "PD_Credits", menuName = "Puppet Dungeon/Crédits", order = 40)]
    public class CreditsData : ScriptableObject
    {
        [Serializable]
        public class Section
        {
            public string title = "Équipe";
            [TextArea(1, 3)] public List<string> lines = new List<string>();
        }

        public string gameTitle = GameInfo.Title;
        [TextArea(1, 3)] public string subtitle = "Volcano Game Jam 3";
        public List<Section> sections = new List<Section>();
        [TextArea(1, 4)] public string closing = "Merci d'avoir joué !";
        [Tooltip("Vitesse de défilement (pixels de référence par seconde).")]
        [Min(5f)] public float scrollSpeed = 70f;
        [Tooltip("Musique des crédits (vide = musique du menu).")]
        public AudioClip music;

        public string BuildText(Color titleColor, Color headingColor)
        {
            var sb = new System.Text.StringBuilder();
            string th = "#" + ColorUtility.ToHtmlStringRGB(titleColor);
            string hh = "#" + ColorUtility.ToHtmlStringRGB(headingColor);
            sb.Append($"<size=150%><b><color={th}>{GameInfo.Resolve(gameTitle)}</color></b></size>\n");
            if (!string.IsNullOrEmpty(subtitle)) sb.Append(subtitle).Append('\n');
            sb.Append("\n\n");
            foreach (var s in sections)
            {
                if (s == null) continue;
                sb.Append($"<size=120%><b><color={hh}>{s.title}</color></b></size>\n");
                if (s.lines != null) foreach (var l in s.lines) sb.Append(l).Append('\n');
                sb.Append("\n\n");
            }
            if (!string.IsNullOrEmpty(closing)) sb.Append($"\n<size=125%><b>{closing}</b></size>\n");
            return sb.ToString();
        }
    }
}
