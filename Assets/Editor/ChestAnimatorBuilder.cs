using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 一键生成宝箱动画控制器：Closed(默认,循环) --Open触发--> Opened(循环)
/// 用法：菜单栏 Game -> 生成宝箱动画控制器
/// </summary>
public static class ChestAnimatorBuilder
{
    private const string FbxPath = "Assets/Resources/Models/Quaternius/FBX/Chest_Wood.fbx";
    private const string SavePath = "Assets/Resources/Animation/Enemy/ChestAnimator.controller";

    [MenuItem("Game/生成宝箱动画控制器")]
    public static void Build()
    {
        // 1. 从 FBX 里找出动画片段（跳过 __preview__ 预览版，用正式片段）
        AnimationClip[] allClips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>()
            .ToArray();

        if (allClips.Length == 0)
        {
            Debug.LogError("FBX 里没有动画！请检查 Chest_Wood.fbx：Rig=Generic，且 Animation 页签勾选了 Import Animation");
            return;
        }

        AnimationClip[] realClips = allClips
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

        string names = string.Join(", ", realClips.Select(c => c.name));
        Debug.Log($"从模型找到正式动画片段: {names}");

        AnimationClip closedClip = realClips.FirstOrDefault(c => c.name.Contains("Closed"));
        AnimationClip openedClip = realClips.FirstOrDefault(c => c.name.Contains("Opened"));
        if (closedClip == null || openedClip == null)
        {
            Debug.LogError($"缺少 Closed/Opened 动画片段，当前有: {names}");
            return;
        }

        // 2. 创建/覆盖控制器
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SavePath) != null)
        {
            AssetDatabase.DeleteAsset(SavePath);
        }
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(SavePath);

        // 3. 加一个 Trigger 参数: Open
        controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);

        // 4. 两个状态: Closed(默认) / Opened
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState closed = sm.AddState("Closed");
        closed.motion = closedClip;          // 关闭动画: 循环, 初始状态

        AnimatorState opened = sm.AddState("Opened");
        opened.motion = openedClip;          // 打开后的动画: 循环, 盖子保持打开

        // 5. 任意状态 -> Opened, 条件是 Open 触发器
        AnimatorStateTransition transition = sm.AddAnyStateTransition(opened);
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Open");
        transition.duration = 0.2f;          // 0.2秒过渡, 和发光时间配合

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"宝箱动画控制器已生成: {SavePath}");
        Debug.Log("下一步: 宝箱加 Animator 组件, 把控制器拖进去; Chest 脚本填 chestAnimator 和 openTrigger=Open");
    }
}