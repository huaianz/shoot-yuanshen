using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

/// <summary>
/// 为场景中的甘雨和宵宫创建与 Lumine 相同类型的手部 Target 和 Two Bone IK。
/// 只在 Game 场景中处理这两个角色。
/// </summary>
[InitializeOnLoad]
public static class MMDPlayerRigBuilder
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    static MMDPlayerRigBuilder()
    {
        EditorApplication.delayCall += SetupIfNeeded;
    }

    [MenuItem("Tools/Setup Ganyu Yoimiya Hand IK")]
    private static void SetupFromMenu()
    {
        SetupAndSave();
    }

    private static void SetupIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameScenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
                return;
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }

        bool needsSetup = false;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "甘雨" && root.name != "宵宫")
                continue;
            PlayerModel model = root.GetComponent<PlayerModel>();
            if (root.transform.Find("Rigs") == null
                || model == null
                || model.leftHandConstraint == null
                || model.rightHandConstraint == null
                || model.mmdHandRigVersion < 14)
            {
                needsSetup = true;
                break;
            }
        }

        if (needsSetup)
            SetupScene(scene);
    }

    private static void SetupAndSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameScenePath)
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        SetupScene(scene);
    }

    private static void SetupScene(Scene scene)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "甘雨" && root.name != "宵宫")
                continue;
            if (SetupCharacter(root))
                count++;
        }

        if (count == 0)
            return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MMDPlayerRigBuilder] 已完成 {count} 个角色的双手 IK 场景配置");
    }

    private static bool SetupCharacter(GameObject root)
    {
        PlayerModel playerModel = root.GetComponent<PlayerModel>();
        PlayerWeapon weapon = root.GetComponentInChildren<PlayerWeapon>(true);
        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (playerModel == null || animator == null)
        {
            Debug.LogWarning($"[MMDPlayerRigBuilder] {root.name} 缺少 PlayerModel 或 Animator");
            return false;
        }

        int removedMmdComponents = RemoveMmdMecanimComponents(root);
        if (removedMmdComponents > 0)
            Debug.Log($"[MMDPlayerRigBuilder] {root.name} 已删除 {removedMmdComponents} 个 MMD4MecanimModel");

        Transform leftUpper = GetBone(animator, HumanBodyBones.LeftUpperArm,
            root.transform, "joint_LeftArm");
        Transform leftForearm = GetBone(animator, HumanBodyBones.LeftLowerArm,
            root.transform, "joint_LeftElbow");
        Transform leftHand = GetBone(animator, HumanBodyBones.LeftHand,
            root.transform, "joint_LeftWrist");
        Transform rightUpper = GetBone(animator, HumanBodyBones.RightUpperArm,
            root.transform, "joint_RightArm");
        Transform rightForearm = GetBone(animator, HumanBodyBones.RightLowerArm,
            root.transform, "joint_RightElbow");
        Transform rightHand = GetBone(animator, HumanBodyBones.RightHand,
            root.transform, "joint_RightWrist");
        if (leftUpper == null || leftForearm == null || leftHand == null
            || rightUpper == null || rightForearm == null || rightHand == null)
        {
            Debug.LogWarning($"[MMDPlayerRigBuilder] {root.name} 手部骨骼不完整");
            return false;
        }

        Transform weaponMount = weapon != null ? weapon.rightHand : null;
        if (weaponMount == null)
            weaponMount = FindDeep(root.transform, "WeaponMount");
        if (weaponMount == null)
            weaponMount = rightHand;

        Transform rigs = EnsureDirectChild(root.transform, "Rigs");
        Rig rig = GetOrAdd<Rig>(rigs.gameObject);
        rig.weight = 1f;

        // 优先使用你自己已经摆好的场景 Target，不在 WeaponMount 下另建重复目标。
        Transform leftTarget = FindLegacyWeaponTarget(root.transform,
            weaponMount, "Left Hand Target");
        if (leftTarget == null)
            leftTarget = FindDeep(root.transform, "Left Hand Target");
        if (leftTarget == null)
        {
            Debug.LogError($"[MMDPlayerRigBuilder] {root.name} 找不到你已有的 Left Hand Target");
            return false;
        }

        Transform rightTarget = FindDeep(root.transform, "Right Hand Target");
        if (rightTarget == null)
        {
            Debug.LogError($"[MMDPlayerRigBuilder] {root.name} 找不到你已有的 Right Hand Target");
            return false;
        }

        Transform obsoleteAimTarget = weaponMount.Find("Left Hand Aim Target");
        if (obsoleteAimTarget != null)
            Object.DestroyImmediate(obsoleteAimTarget.gameObject);

        TwoBoneIKConstraint leftConstraint = EnsureConstraint(rigs,
            "Left Hand Constraint", leftUpper, leftForearm, leftHand, leftTarget);
        TwoBoneIKConstraint rightConstraint = EnsureConstraint(rigs,
            "Right Hand Constraint", rightUpper, rightForearm, rightHand, rightTarget);
        TwoBoneIKConstraintData leftData = leftConstraint.data;
        leftData.targetPositionWeight = 1f;
        leftData.targetRotationWeight = 0f;
        leftData.hintWeight = 0f;
        leftConstraint.data = leftData;

        TwoBoneIKConstraintData rightData = rightConstraint.data;
        rightData.targetPositionWeight = 1f;
        rightData.targetRotationWeight = 0f;
        rightData.hintWeight = 0f;
        rightConstraint.data = rightData;

        DisableConstraintIfExists<MultiAimConstraint>(
            rigs, "Right Hand Aim Constraint");
        DisableConstraintIfExists<MultiAimConstraint>(
            rigs, "Body Aim Constraint");

        // 左右手保留给用户调姿势；MMD骨骼不叠加瞄准Aim约束。
        leftConstraint.weight = 0.8f;
        leftConstraint.enabled = true;
        rightConstraint.weight = 0.8f;
        rightConstraint.enabled = true;
        playerModel.leftHandConstraint = leftConstraint;
        playerModel.rightHandConstraint = rightConstraint;
        playerModel.rightHandAimConstraint = null;
        playerModel.BodyAimConstraint = null;
        playerModel.useRightHandConstraint = false;
        playerModel.mmdHandRigVersion = 14;

        if (weapon != null)
            weapon.leftHandTarget = leftTarget;

        RigBuilder rigBuilder = GetOrAdd<RigBuilder>(animator.gameObject);
        bool hasLayer = false;
        foreach (RigLayer layer in rigBuilder.layers)
        {
            if (layer.rig == rig)
            {
                hasLayer = true;
                break;
            }
        }
        if (!hasLayer)
            rigBuilder.layers.Add(new RigLayer(rig));

        rigBuilder.enabled = true;
        rigBuilder.Build();
        return true;
    }

    private static int RemoveMmdMecanimComponents(GameObject root)
    {
        int removed = 0;
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour.GetType().Name != "MMD4MecanimModel")
                continue;
            Object.DestroyImmediate(behaviour);
            removed++;
        }
        return removed;
    }

    private static TwoBoneIKConstraint EnsureConstraint(Transform parent, string name,
        Transform root, Transform mid, Transform tip, Transform target)
    {
        Transform constraintTransform = EnsureDirectChild(parent, name);
        TwoBoneIKConstraint constraint =
            GetOrAdd<TwoBoneIKConstraint>(constraintTransform.gameObject);
        constraint.data = new TwoBoneIKConstraintData
        {
            root = root,
            mid = mid,
            tip = tip,
            target = target,
            targetPositionWeight = 1f,
            targetRotationWeight = 1f,
            hintWeight = 1f
        };
        return constraint;
    }

    private static MultiAimConstraint EnsureAimConstraint(Transform parent, string name,
        Transform constrainedObject, Transform aimTarget,
        MultiAimConstraintData.Axis aimAxis,
        MultiAimConstraintData.Axis upAxis,
        Vector3 offset)
    {
        Transform constraintTransform = EnsureDirectChild(parent, name);
        MultiAimConstraint constraint =
            GetOrAdd<MultiAimConstraint>(constraintTransform.gameObject);

        WeightedTransformArray sources = new WeightedTransformArray(1);
        sources.SetTransform(0, aimTarget);
        sources.SetWeight(0, 1f);

        constraint.data = new MultiAimConstraintData
        {
            constrainedObject = constrainedObject,
            sourceObjects = sources,
            offset = offset,
            limits = new Vector2(-180f, 180f),
            aimAxis = aimAxis,
            upAxis = upAxis,
            worldUpType = MultiAimConstraintData.WorldUpType.None,
            worldUpAxis = MultiAimConstraintData.Axis.Y,
            maintainOffset = false,
            constrainedXAxis = true,
            constrainedYAxis = true,
            constrainedZAxis = true
        };
        constraint.weight = 1f;
        constraint.enabled = true;
        return constraint;
    }

    private static Transform EnsureDirectChild(Transform parent, string name)
    {
        return EnsureDirectChild(parent, name, out _);
    }

    private static Transform EnsureDirectChild(Transform parent, string name, out bool created)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            created = false;
            return existing;
        }

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        created = true;
        return child.transform;
    }

    private static Transform FindLegacyWeaponTarget(Transform root,
        Transform weaponMount, string targetName)
    {
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name != targetName)
                continue;
            if (candidate == weaponMount || candidate.IsChildOf(weaponMount))
                continue;
            if (candidate.parent != null
                && candidate.parent.name.IndexOf("Weapon", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return candidate;
        }
        return null;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void DisableConstraintIfExists<T>(Transform parent, string name)
        where T : Behaviour
    {
        Transform child = parent.Find(name);
        if (child == null)
            return;

        T component = child.GetComponent<T>();
        if (component != null)
            component.enabled = false;
    }

    private static Transform GetBone(Animator animator, HumanBodyBones bone,
        Transform root, params string[] fallbackNames)
    {
        Transform result = animator.GetBoneTransform(bone);
        if (result != null)
            return result;

        foreach (string fallbackName in fallbackNames)
        {
            result = FindDeep(root, fallbackName);
            if (result != null)
                return result;
        }
        return null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Transform FindSceneTransform(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeep(root.transform, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
