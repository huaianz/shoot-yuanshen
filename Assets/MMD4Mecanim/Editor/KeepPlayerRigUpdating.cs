using UnityEngine;
using UnityEditor;

public class KeepPlayerRigUpdating
{
    [MenuItem("Tools/玩家骨骼持续更新(防子弹错位)")]
    public static void Apply()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("请先选中玩家模型(Lumine FBX / Pilot Furina)");
            return;
        }

        int animCount = 0;
        int smrCount = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            // Animator 永远更新,骨骼不会冻结
            foreach (Animator anim in go.GetComponentsInChildren<Animator>(true))
            {
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animCount++;
            }
            // 双保险:蒙皮也持续更新
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                smrCount++;
            }
        }
        Debug.Log("已设置 " + animCount + " 个 Animator、" + smrCount + " 个 SkinnedMeshRenderer");
    }
}