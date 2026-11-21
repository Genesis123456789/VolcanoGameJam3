#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// À placer dans un dossier nommé "Editor" (ex: Assets/Editor/).
/// Menu : Tools > Créer Animator du joueur.
/// Génère un Animator Controller avec les paramètres Speed (float) et Attack (trigger),
/// un état Idle, un blend tree Move (marche -> course) et, si fourni, un état Attack.
/// </summary>
public class CreatePlayerAnimatorWizard : ScriptableWizard
{
    [Tooltip("Optionnel. Sans clip, l'état Idle sera vide.")]
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip run;
    [Tooltip("Optionnel. Sans clip, aucun état d'attaque n'est créé.")]
    public AnimationClip attack;

    [MenuItem("Tools/Créer Animator du joueur")]
    private static void Open()
    {
        DisplayWizard<CreatePlayerAnimatorWizard>("Animator du joueur", "Créer");
    }

    private void OnWizardUpdate()
    {
        isValid = walk != null && run != null;
        errorString = isValid ? "" : "Il faut au moins le clip de marche et le clip de course.";
    }

    private void OnWizardCreate()
    {
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/PlayerAnimator.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Idle
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idle;
        sm.defaultState = idleState;

        // Move : blend tree sur Speed (marche à demi-vitesse, course à pleine vitesse)
        BlendTree tree = new BlendTree();
        tree.name = "Move";
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(walk, 0.5f);
        tree.AddChild(run, 1f);
        AssetDatabase.AddObjectToAsset(tree, controller);

        AnimatorState moveState = sm.AddState("Move");
        moveState.motion = tree;

        // Idle -> Move
        AnimatorStateTransition toMove = idleState.AddTransition(moveState);
        toMove.hasExitTime = false;
        toMove.duration = 0.1f;
        toMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        // Move -> Idle
        AnimatorStateTransition toIdle = moveState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Attack (optionnel)
        if (attack != null)
        {
            AnimatorState attackState = sm.AddState("Attack");
            attackState.motion = attack;

            AnimatorStateTransition anyToAttack = sm.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.05f;
            anyToAttack.canTransitionToSelf = false;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.1f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
    }
}
#endif
