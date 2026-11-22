using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Diagnostic des animations (préfixe [PD-DIAG] dans la Console) : clips cuits et avatar du joueur.</summary>
    [InitializeOnLoad]
    public static class PDDiagnostics
    {
        private const string Key = "PD_Diag_v3";

        static PDDiagnostics()
        {
            if (SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, true);
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/Puppet Dungeon/Animations/Diagnostic des animations", priority = 42)]
        public static void Run()
        {
            var lib = AssetDatabase.LoadAssetAtPath<AnimationLibrary>(PDPaths.ContentAnimation + "/PD_AnimationLibrary.asset");
            if (lib != null)
            {
                foreach (var e in lib.entries.Where(x => x != null && x.bake))
                {
                    var c = e.result;
                    if (c == null) { Debug.Log($"[PD-DIAG] {e.clipName} : pas encore cuit"); continue; }
                    var b = AnimationUtility.GetCurveBindings(c);
                    var leg = b.FirstOrDefault(x => x.propertyName == "Left Upper Leg Front-Back");
                    var arm = b.FirstOrDefault(x => x.propertyName == "Right Arm Down-Up");
                    Debug.Log($"[PD-DIAG] {c.name} len={c.length:0.00} human={c.humanMotion} courbes={b.Length} jambeG={Range(c, leg)} brasD={Range(c, arm)}");
                }
            }
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PDPaths.Prefabs + "/PD_Player.prefab");
            if (player != null)
            {
                var a = player.GetComponentInChildren<Animator>();
                Debug.Log($"[PD-DIAG] PD_Player Animator: avatar={(a != null && a.avatar != null ? a.avatar.name + " valid=" + a.avatar.isValid + " human=" + a.avatar.isHuman : "AUCUN")} controller={(a != null && a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "AUCUN")}");
            }
        }

        private static string Range(AnimationClip clip, EditorCurveBinding b)
        {
            if (string.IsNullOrEmpty(b.propertyName)) return "-";
            var curve = AnimationUtility.GetEditorCurve(clip, b);
            if (curve == null || curve.length == 0) return "-";
            float mn = curve.keys.Min(k => k.value), mx = curve.keys.Max(k => k.value);
            return $"{mn:0.00}..{mx:0.00}";
        }
    }
}
