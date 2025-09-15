#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

public static class A3AnimatorBuilder
{
    private const string CharsDir = "Assets/Art/Sprites/Characters";
    private const string ItemsDir = "Assets/Art/Sprites/Items";
    private const string AnimBase = "Assets/Animations";

    [MenuItem("PacStudent/Build Animators (A3)")]
    public static void BuildAnimators()
    {
        Directory.CreateDirectory(AnimBase);
        Directory.CreateDirectory(Path.Combine(AnimBase, "PacStudent"));
        Directory.CreateDirectory(Path.Combine(AnimBase, "Ghosts"));
        Directory.CreateDirectory(Path.Combine(AnimBase, "Items"));

        BuildPacStudentAnimator();
        BuildGhostAnimator();
        BuildPelletAnimator();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("A3 Animators and Clips created.");
    }

    [MenuItem("PacStudent/Assign Animators To Scene (A3)")]
    public static void AssignToScene()
    {
        var pacCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Path.Combine(AnimBase, "PacStudent/PacStudent.controller"));
        var gCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Path.Combine(AnimBase, "Ghosts/GhostAnimator_.controller"));
        if (pacCtrl != null)
        {
            var pac = GameObject.Find("PacStudent");
            if (pac != null)
            {
                var an = pac.GetComponent<Animator>();
                if (an == null) an = pac.AddComponent<Animator>();
                an.runtimeAnimatorController = pacCtrl;
            }
        }
        if (gCtrl != null)
        {
            for (int i=1;i<=4;i++)
            {
                var g = GameObject.Find($"Ghost_{i}");
                if (g != null)
                {
                    var an = g.GetComponent<Animator>();
                    if (an == null) an = g.AddComponent<Animator>();
                    an.runtimeAnimatorController = gCtrl;
                }
            }
        }
        Debug.Log("Assigned animators to scene objects (if found).");
    }

    private static void BuildPacStudentAnimator()
    {
        var left0 = LoadSprite("pac_left_0.png");
        var left1 = LoadSprite("pac_left_1.png");
        var right0 = LoadSprite("pac_right_0.png");
        var right1 = LoadSprite("pac_right_1.png");
        var up0 = LoadSprite("pac_up_0.png");
        var up1 = LoadSprite("pac_up_1.png");
        var down0 = LoadSprite("pac_down_0.png");
        var down1 = LoadSprite("pac_down_1.png");
        var dead0 = LoadSprite("pac_dead_0.png");
        var dead1 = LoadSprite("pac_dead_1.png");

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(Path.Combine(AnimBase, "PacStudent/PacStudent.controller"));

        var stLeft = ctrl.AddMotion(CreateSpriteClip("Pac_Walk_Left", new[]{left0,left1}, 3f));
        var stUp = ctrl.AddMotion(CreateSpriteClip("Pac_Walk_Up", new[]{up0,up1}, 3f));
        var stRight = ctrl.AddMotion(CreateSpriteClip("Pac_Walk_Right", new[]{right0,right1}, 3f));
        var stDown = ctrl.AddMotion(CreateSpriteClip("Pac_Walk_Down", new[]{down0,down1}, 3f));
        var stDead = ctrl.AddMotion(CreateSpriteClip("Pac_Dead", new[]{dead0,dead1}, 3f));

        ctrl.layers[0].stateMachine.defaultState = stLeft;
        // chain transitions Left->Up->Right->Down->Dead->Left, each exit at clip end (3s)
        AddNext(ctrl, stLeft, stUp);
        AddNext(ctrl, stUp, stRight);
        AddNext(ctrl, stRight, stDown);
        AddNext(ctrl, stDown, stDead);
        AddNext(ctrl, stDead, stLeft);
    }

    private static void BuildGhostAnimator()
    {
        var body = LoadSprite("ghost_body.png");
        var scared = LoadSprite("ghost_scared.png");
        var rec0 = LoadSprite("ghost_recover_0.png");
        var rec1 = LoadSprite("ghost_recover_1.png");
        var eyes = LoadSprite("ghost_eyes.png");

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(Path.Combine(AnimBase, "Ghosts/GhostAnimator_.controller"));

        var stLeft = ctrl.AddMotion(CreateSpriteClip("G_Walk_Left", new[]{body,body}, 3f));
        var stUp = ctrl.AddMotion(CreateSpriteClip("G_Walk_Up", new[]{body,body}, 3f));
        var stRight = ctrl.AddMotion(CreateSpriteClip("G_Walk_Right", new[]{body,body}, 3f));
        var stDown = ctrl.AddMotion(CreateSpriteClip("G_Walk_Down", new[]{body,body}, 3f));
        var stScared = ctrl.AddMotion(CreateSpriteClip("G_Scared", new[]{scared,scared}, 3f));
        var stRecover = ctrl.AddMotion(CreateSpriteClip("G_Recover", new[]{rec0,rec1}, 3f));
        var stDead = ctrl.AddMotion(CreateSpriteClip("G_Dead", new[]{eyes,eyes}, 3f));

        ctrl.layers[0].stateMachine.defaultState = stLeft;
        AddNext(ctrl, stLeft, stUp);
        AddNext(ctrl, stUp, stRight);
        AddNext(ctrl, stRight, stDown);
        AddNext(ctrl, stDown, stScared);
        AddNext(ctrl, stScared, stRecover);
        AddNext(ctrl, stRecover, stDead);
        AddNext(ctrl, stDead, stLeft);
    }

    private static void BuildPelletAnimator()
    {
        var p0 = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(ItemsDir, "power_pellet_0.png"));
        var p1 = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(ItemsDir, "power_pellet_1.png"));
        var clip = CreateSpriteClip("PowerPellet_Flash", new[]{p0,p1}, 2f);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(Path.Combine(AnimBase, "Items/PowerPellet.controller"));
        ctrl.AddMotion(clip);
    }

    private static AnimationClip CreateSpriteClip(string name, Sprite[] frames, float length)
    {
        var clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 12f;
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        var keys = new ObjectReferenceKeyframe[Mathf.Max(2, frames.Length)];
        float[] times;
        if (frames.Length == 2)
        {
            times = new float[] { 0f, length/2f };
        }
        else
        {
            times = new float[frames.Length];
            for (int i=0;i<frames.Length;i++) times[i] = i*(length/(frames.Length-1));
        }
        for (int i=0;i<keys.Length;i++)
        {
            keys[i] = new ObjectReferenceKeyframe{ time = times[i], value = frames[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, Path.Combine(AnimBase, name+".anim"));
        return clip;
    }

    private static void AddNext(AnimatorController ctrl, AnimatorState from, AnimatorState to)
    {
        // Create transition from a state to another state using AnimatorState API (not AnimatorStateMachine)
        var t = from.AddTransition(to);
        t.hasExitTime = true; // transition occurs at the end of the current clip
        t.exitTime = 1f;      // normalized time
        t.hasFixedDuration = true;
        t.duration = 0.1f;    // brief blend
    }

    private static Sprite LoadSprite(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(CharsDir, file));
    }
}
#endif

