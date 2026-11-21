using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Diagnostic des avatars et clips (s'affiche une fois par session dans la Console, préfixe [PD-DIAG]).</summary>
    [InitializeOnLoad]
    public static class PDDiagnostics
    {
        private const string Key = "PD_Diag_v2";

        static PDDiagnostics()
        {
            if (SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, true);
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/Puppet Dungeon/Diagnostic des animations", priority = 30)]
        public static void Run()
        {
            Report(PDPaths.Rig);
            Report(PDPaths.AnimWalk);
            Report(PDPaths.AnimRun);
            Report(PDPaths.AnimAttack);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PDPaths.Prefabs + "/PD_Player.prefab");
            if (player != null)
            {
                var a = player.GetComponentInChildren<Animator>();
                Debug.Log($"[PD-DIAG] PD_Player Animator: avatar={(a != null && a.avatar != null ? a.avatar.name + " valid=" + a.avatar.isValid + " human=" + a.avatar.isHuman : "AUCUN")} controller={(a != null && a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "AUCUN")}");
            }
        }

        private static void Report(string path)
        {
            string p = PDPaths.FindPath(path);
            if (p == null) { Debug.Log("[PD-DIAG] introuvable: " + path); return; }
            var mi = AssetImporter.GetAtPath(p) as ModelImporter;
            var assets = AssetDatabase.LoadAllAssetsAtPath(p);
            var avatar = assets.OfType<Avatar>().FirstOrDefault();
            string msg = $"[PD-DIAG] {p} type={(mi != null ? mi.animationType.ToString() : "?")} setup={(mi != null ? mi.avatarSetup.ToString() : "?")} " +
                         $"humanBonesMeta={(mi != null ? mi.humanDescription.human.Length : -1)} " +
                         $"avatar={(avatar != null ? avatar.name + " valid=" + avatar.isValid + " human=" + avatar.isHuman + " bones=" + avatar.humanDescription.human.Length : "AUCUN")}";
            foreach (var clip in assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            {
                var b = AnimationUtility.GetCurveBindings(clip);
                var leg = b.FirstOrDefault(x => x.propertyName.Contains("Left Upper Leg Front-Back"));
                string range = "-";
                if (!string.IsNullOrEmpty(leg.propertyName))
                {
                    var c = AnimationUtility.GetEditorCurve(clip, leg);
                    float mn = float.MaxValue, mx = float.MinValue;
                    for (float t = 0; t <= clip.length; t += clip.length / 20f) { float v = c.Evaluate(t); mn = Mathf.Min(mn, v); mx = Mathf.Max(mx, v); }
                    range = mn.ToString("0.00") + ".." + mx.ToString("0.00");
                }
                msg += $"\n    clip {clip.name} len={clip.length:0.00} human={clip.humanMotion} curves={b.Length} muscles={b.Count(x => x.type == typeof(Animator))} jambeG={range}";
            }
            Debug.Log(msg);
        }
    }
}
