using UnityEditor;
using UnityEngine;

public static class AnimationTools
{
    [MenuItem("Tools/Animations/Set Additive Reference To Self")]
    private static void SetAdditiveReferenceToSelf()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;

        if (clip == null)
        {
            Debug.LogError("Select an AnimationClip first.");
            return;
        }

        AnimationUtility.SetAdditiveReferencePose(
            clip,
            clip,
            0f
        );

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();

        Debug.Log("Additive reference pose set to frame 0 of: " + clip.name);
    }
}