#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GhostAnimatorSetup
{
    [MenuItem("PacStudent/Setup Ghost Animator (4-dir cycle)")]
    public static void Setup()
    {
        // Load controller
        string ctrlPath = "Assets/Animations/Ghosts/GhostAnimator_.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            EditorUtility.DisplayDialog("Ghost Animator Setup", "Controller not found:\n" + ctrlPath, "OK");
            return;
        }

        // Load clips
        string baseAnimPath = "Assets/Animations";
        var walkUp   = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseAnimPath}/G_Walk_Up.anim");
        var walkDown = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseAnimPath}/G_Walk_Down.anim");
        var walkLeft = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseAnimPath}/G_Walk_Left.anim");
        var walkRight= AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseAnimPath}/G_Walk_Right.anim");
        var scared   = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseAnimPath}/G_Scared.anim");
        var recover  = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseAnimPath}/G_Recover.anim");
        var dead     = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseAnimPath}/G_Dead.anim");

        if (walkUp == null || walkDown == null || walkLeft == null || walkRight == null ||
            scared == null || recover == null || dead == null)
        {
            EditorUtility.DisplayDialog("Ghost Animator Setup", "Missing one or more animation clips under Assets/Animations.", "OK");
            return;
        }

        var sm = ctrl.layers[0].stateMachine;

        // Helper: find or create state by name and assign clip
        AnimatorState FindOrCreate(string name, AnimationClip clip)
        {
            foreach (var s in sm.states)
            {
                if (s.state != null && s.state.name == name) return s.state;
            }
            var st = sm.AddState(name, new Vector3(Random.Range(150, 400), Random.Range(0, 300)));
            st.motion = clip;
            return st;
        }

        // Create/assign states
        var sWalkUp    = FindOrCreate("Walk_Up", walkUp);
        var sWalkRight = FindOrCreate("Walk_Right", walkRight);
        var sWalkDown  = FindOrCreate("Walk_Down", walkDown);
        var sWalkLeft  = FindOrCreate("Walk_Left", walkLeft);
        var sScared    = FindOrCreate("Scared", scared);
        var sRecover   = FindOrCreate("Recover", recover);
        var sDead      = FindOrCreate("Dead", dead);

        // Set default state if needed
        sm.defaultState = sWalkUp;

        // Helper: ensure a transition exists between two states with desired exit timing
        void EnsureTransition(AnimatorState from, AnimatorState to, float exitSeconds)
        {
            foreach (var t in from.transitions)
            {
                if (t.destinationState == to)
                {
                    t.hasExitTime = true;
                    t.hasFixedDuration = true;
                    t.duration = 0f;
                    t.exitTime = exitSeconds; // seconds because hasFixedDuration=true
                    return;
                }
            }
            var nt = from.AddTransition(to);
            nt.hasExitTime = true;
            nt.hasFixedDuration = true;
            nt.duration = 0f;
            nt.exitTime = exitSeconds;
        }

        // Build cycle: Walk_Up -> Walk_Right -> Walk_Down -> Walk_Left -> Scared -> Recover -> Dead -> Walk_Up
        EnsureTransition(sWalkUp,    sWalkRight, 2f);
        EnsureTransition(sWalkRight, sWalkDown,  2f);
        EnsureTransition(sWalkDown,  sWalkLeft,  2f);
        EnsureTransition(sWalkLeft,  sScared,    2f);
        EnsureTransition(sScared,    sRecover,   2f);
        EnsureTransition(sRecover,   sDead,      2f);
        EnsureTransition(sDead,      sWalkUp,    2f);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Ghost Animator Setup", "Ghost Animator updated: 4-direction walk + Scared/Recover/Dead cycle.", "OK");
    }
}
#endif

