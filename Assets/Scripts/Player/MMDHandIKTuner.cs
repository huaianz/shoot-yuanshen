using UnityEngine;

/// <summary>
/// 甘雨/宵宫专用握枪调整器。
/// 左手使用双骨骼 CCD IK 扶枪，右手武器挂点用于调整枪在手掌里的位置。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public sealed class MMDHandIKTuner : MonoBehaviour
{
    [Header("左手 IK")]
    [Tooltip("左手目标相对角色根节点的位置偏移，使用模型本地坐标")]
    public Vector3 leftHandPositionOffset;
    [Tooltip("左手目标相对角色根节点的欧拉角偏移")]
    public Vector3 leftHandRotationOffset;
    [Range(0f, 1f)]
    public float leftHandWeight = 1f;

    [Header("右手 IK")]
    [Tooltip("右手目标相对角色根节点的位置偏移，使用模型本地坐标")]
    public Vector3 rightHandPositionOffset;
    [Tooltip("右手目标相对角色根节点的欧拉角偏移")]
    public Vector3 rightHandRotationOffset;
    [Range(0f, 1f)]
    public float rightHandWeight = 1f;

    [Header("枪械掌内偏移")]
    [Tooltip("枪相对右手 WeaponMount 的位置偏移")]
    public Vector3 weaponPositionOffset;
    [Tooltip("枪相对右手 WeaponMount 的欧拉角偏移")]
    public Vector3 weaponRotationOffset;

    [Header("调试")]
    public bool drawTargetGizmos = true;

    private Transform leftUpperArm;
    private Transform leftElbow;
    private Transform leftHand;
    private Transform leftHandTarget;
    private Transform rightUpperArm;
    private Transform rightElbow;
    private Transform rightHand;
    private Transform rightHandTarget;
    private Transform weaponMount;

    private Vector3 baseLeftTargetLocalPosition;
    private Quaternion baseLeftTargetLocalRotation;
    private Vector3 baseRightTargetLocalPosition;
    private Quaternion baseRightTargetLocalRotation;
    private Vector3 baseWeaponLocalPosition;
    private Quaternion baseWeaponLocalRotation;
    private string prefsPrefix;
    private bool initialized;
    private int playerModelIndex = -1;
    private Vector3 lastLoggedPositionOffset;
    private Vector3 lastLoggedRotationOffset;
    private Vector3 lastLoggedRightPositionOffset;
    private Vector3 lastLoggedRightRotationOffset;
    private Vector3 lastLoggedWeaponPositionOffset;
    private Vector3 lastLoggedWeaponRotationOffset;

    public bool IsInitialized => initialized;
    public Transform LeftHandTarget => leftHandTarget;
    public Transform RightHandTarget => rightHandTarget;
    public Transform WeaponMount => weaponMount;

    public void Initialize(PlayerModel owner, PlayerWeapon weapon, Animator animator)
    {
        if (initialized || owner == null)
            return;

        prefsPrefix = $"MMDHandIKTuner.{owner.name}";
        if (GameManager.INSTANCE != null && GameManager.INSTANCE.playerModels != null)
        {
            for (int i = 0; i < GameManager.INSTANCE.playerModels.Length; i++)
            {
                if (GameManager.INSTANCE.playerModels[i] == owner)
                {
                    playerModelIndex = i;
                    break;
                }
            }
        }
        ResolveBones(owner.transform, animator);

        if (leftUpperArm == null || leftElbow == null || leftHand == null
            || rightUpperArm == null || rightElbow == null || rightHand == null)
        {
            Debug.LogWarning($"[MMDHandIKTuner] {owner.name} 缺少左右手骨骼，无法创建握枪 IK");
            return;
        }

        CreateLeftHandTarget();
        CreateRightHandTarget();
        weaponMount = weapon != null ? weapon.rightHand : null;
        if (weaponMount == null)
            weaponMount = FindDeep(owner.transform, "WeaponMount");

        if (weaponMount != null)
        {
            baseWeaponLocalPosition = weaponMount.localPosition;
            baseWeaponLocalRotation = weaponMount.localRotation;
        }

        LoadSettings();
        initialized = true;
        ApplyTuning();

        Debug.Log($"[MMDHandIKTuner] {owner.name} 握枪调整已启用；" +
                  $"左上臂={leftUpperArm.name}, 左前臂={leftElbow.name}, 左手={leftHand.name}, " +
                  $"右上臂={rightUpperArm.name}, 右前臂={rightElbow.name}, 右手={rightHand.name}, " +
                  $"武器挂点={(weaponMount != null ? weaponMount.name : "无")}, " +
                  $"挂点跟随右手={(weaponMount == rightHand || (weaponMount != null && weaponMount.IsChildOf(rightHand)))}");
    }

    private void ResolveBones(Transform root, Animator animator)
    {
        leftUpperArm = GetBone(animator, HumanBodyBones.LeftUpperArm, root,
            "joint_LeftArm", "DEF-upper_arm.L");
        leftElbow = GetBone(animator, HumanBodyBones.LeftLowerArm, root,
            "joint_LeftElbow", "DEF-forearm.L");
        leftHand = GetBone(animator, HumanBodyBones.LeftHand, root,
            "joint_LeftWrist", "DEF-hand.L");
        rightUpperArm = GetBone(animator, HumanBodyBones.RightUpperArm, root,
            "joint_RightArm", "DEF-upper_arm.R");
        rightElbow = GetBone(animator, HumanBodyBones.RightLowerArm, root,
            "joint_RightElbow", "DEF-forearm.R");
        rightHand = GetBone(animator, HumanBodyBones.RightHand, root,
            "joint_RightWrist", "DEF-hand.R");
    }

    private void CreateLeftHandTarget()
    {
        leftHandTarget = FindDeep(transform, "MMD Left Hand Target");
        if (leftHandTarget == null)
        {
            leftHandTarget = new GameObject("MMD Left Hand Target").transform;
            leftHandTarget.SetParent(transform, false);
        }

        // 初始时把目标放在左手当前位置，之后通过 Inspector 或 Scene 视图微调。
        baseLeftTargetLocalPosition = transform.InverseTransformPoint(leftHand.position);
        baseLeftTargetLocalRotation =
            Quaternion.Inverse(transform.rotation) * leftHand.rotation;
        leftHandTarget.localPosition = baseLeftTargetLocalPosition;
        leftHandTarget.localRotation = baseLeftTargetLocalRotation;
        leftHandTarget.localScale = Vector3.one;
    }

    private void CreateRightHandTarget()
    {
        rightHandTarget = FindDeep(transform, "MMD Right Hand Target");
        if (rightHandTarget == null)
        {
            rightHandTarget = new GameObject("MMD Right Hand Target").transform;
            rightHandTarget.SetParent(transform, false);
        }

        baseRightTargetLocalPosition = transform.InverseTransformPoint(rightHand.position);
        baseRightTargetLocalRotation =
            Quaternion.Inverse(transform.rotation) * rightHand.rotation;
        rightHandTarget.localPosition = baseRightTargetLocalPosition;
        rightHandTarget.localRotation = baseRightTargetLocalRotation;
        rightHandTarget.localScale = Vector3.one;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        Vector3 leftHandBefore = leftHand != null ? leftHand.position : Vector3.zero;
        Vector3 rightHandBefore = rightHand != null ? rightHand.position : Vector3.zero;
        Vector3 weaponBefore = weaponMount != null ? weaponMount.position : Vector3.zero;
        SolveLeftArm();
        SolveRightArm();

        bool changed = lastLoggedPositionOffset != leftHandPositionOffset
            || lastLoggedRotationOffset != leftHandRotationOffset
            || lastLoggedRightPositionOffset != rightHandPositionOffset
            || lastLoggedRightRotationOffset != rightHandRotationOffset
            || lastLoggedWeaponPositionOffset != weaponPositionOffset
            || lastLoggedWeaponRotationOffset != weaponRotationOffset;
        if (changed)
        {
            lastLoggedPositionOffset = leftHandPositionOffset;
            lastLoggedRotationOffset = leftHandRotationOffset;
            lastLoggedRightPositionOffset = rightHandPositionOffset;
            lastLoggedRightRotationOffset = rightHandRotationOffset;
            lastLoggedWeaponPositionOffset = weaponPositionOffset;
            lastLoggedWeaponRotationOffset = weaponRotationOffset;

            Vector3 leftHandAfter = leftHand != null ? leftHand.position : Vector3.zero;
            Vector3 rightHandAfter = rightHand != null ? rightHand.position : Vector3.zero;
            Vector3 weaponAfter = weaponMount != null ? weaponMount.position : Vector3.zero;
            Vector3 leftTargetPosition = leftHandTarget != null
                ? leftHandTarget.position
                : Vector3.zero;
            Vector3 rightTargetPosition = rightHandTarget != null
                ? rightHandTarget.position
                : Vector3.zero;
            Debug.Log($"[MMDHandIKTuner][诊断] {name}\n" +
                      $"左手 offset={leftHandPositionOffset}, weight={leftHandWeight:F2}, " +
                      $"before={leftHandBefore}, after={leftHandAfter}, target={leftTargetPosition}\n" +
                      $"右手 offset={rightHandPositionOffset}, weight={rightHandWeight:F2}, " +
                      $"before={rightHandBefore}, after={rightHandAfter}, target={rightTargetPosition}\n" +
                      $"枪挂点 before={weaponBefore}, after={weaponAfter}");
        }
    }

    private void OnValidate()
    {
        // Inspector 修改偏移后立即应用并求解，保证调整马上可见。
        if (Application.isPlaying && initialized)
            ApplyImmediate();
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        ApplyTuning();
        SolveLeftArm();
        SolveRightArm();
    }

    /// <summary>
    /// 手动读取 Scene 中拖动后的 Target/WeaponMount 变换。
    /// 不在 LateUpdate 自动调用，避免覆盖 Inspector 中刚修改的偏移。
    /// </summary>
    public void ReadTargetTransform()
    {
        if (leftHandTarget != null)
        {
            Vector3 expectedPosition = baseLeftTargetLocalPosition + leftHandPositionOffset;
            if ((leftHandTarget.localPosition - expectedPosition).sqrMagnitude > 0.000001f)
                leftHandPositionOffset = leftHandTarget.localPosition - baseLeftTargetLocalPosition;

            Quaternion expectedRotation =
                baseLeftTargetLocalRotation * Quaternion.Euler(leftHandRotationOffset);
            if (Quaternion.Angle(leftHandTarget.localRotation, expectedRotation) > 0.05f)
                leftHandRotationOffset = (Quaternion.Inverse(baseLeftTargetLocalRotation)
                    * leftHandTarget.localRotation).eulerAngles;
        }

        if (weaponMount != null)
        {
            Vector3 expectedPosition = baseWeaponLocalPosition + weaponPositionOffset;
            if ((weaponMount.localPosition - expectedPosition).sqrMagnitude > 0.000001f)
                weaponPositionOffset = weaponMount.localPosition - baseWeaponLocalPosition;

            Quaternion expectedRotation =
                baseWeaponLocalRotation * Quaternion.Euler(weaponRotationOffset);
            if (Quaternion.Angle(weaponMount.localRotation, expectedRotation) > 0.05f)
                weaponRotationOffset = (Quaternion.Inverse(baseWeaponLocalRotation)
                    * weaponMount.localRotation).eulerAngles;
        }

        if (rightHandTarget != null)
        {
            Vector3 expectedPosition = baseRightTargetLocalPosition + rightHandPositionOffset;
            if ((rightHandTarget.localPosition - expectedPosition).sqrMagnitude > 0.000001f)
                rightHandPositionOffset = rightHandTarget.localPosition - baseRightTargetLocalPosition;

            Quaternion expectedRotation =
                baseRightTargetLocalRotation * Quaternion.Euler(rightHandRotationOffset);
            if (Quaternion.Angle(rightHandTarget.localRotation, expectedRotation) > 0.05f)
                rightHandRotationOffset = (Quaternion.Inverse(baseRightTargetLocalRotation)
                    * rightHandTarget.localRotation).eulerAngles;
        }
    }

    public void ApplyTuning()
    {
        if (!initialized)
            return;

        if (leftHandTarget != null)
        {
            leftHandTarget.localPosition = baseLeftTargetLocalPosition + leftHandPositionOffset;
            leftHandTarget.localRotation =
                baseLeftTargetLocalRotation * Quaternion.Euler(leftHandRotationOffset);
        }

        if (rightHandTarget != null)
        {
            rightHandTarget.localPosition =
                baseRightTargetLocalPosition + rightHandPositionOffset;
            rightHandTarget.localRotation =
                baseRightTargetLocalRotation * Quaternion.Euler(rightHandRotationOffset);
        }

        if (weaponMount != null)
        {
            weaponMount.localPosition = baseWeaponLocalPosition + weaponPositionOffset;
            weaponMount.localRotation =
                baseWeaponLocalRotation * Quaternion.Euler(weaponRotationOffset);
        }
    }

    public void RebuildRig()
    {
        if (!initialized)
            return;

        ApplyImmediate();
        Debug.Log($"[MMDHandIKTuner] {name} 已重新计算左手 IK");
    }

    public void ApplyImmediate()
    {
        ApplyTuning();
        SolveLeftArm();
        SolveRightArm();
    }

    // Inspector 按钮入口：避免用户调整后台角色的参数。
    public void ActivateThisRole()
    {
        if (playerModelIndex >= 0 && PlayerController.INSTANCE != null)
            PlayerController.INSTANCE.SwitchPlayerModel(playerModelIndex);
    }

    /// <summary>
    /// 不依赖 RigBuilder 的双骨骼 CCD。Animator 先完成动画计算，
    /// 这里再在 LateUpdate 中修正上臂/前臂，保证 Inspector 调整立即可见。
    /// </summary>
    private void SolveLeftArm()
    {
        if (leftHandWeight <= 0.0001f || leftHandTarget == null
            || leftUpperArm == null || leftElbow == null || leftHand == null)
            return;

        // 两轮足以让手掌接近 Target，同时比完整 IK 求解更稳定。
        SolveJoint(leftElbow, leftHand, leftHandTarget.position, leftHandWeight);
        SolveJoint(leftUpperArm, leftHand, leftHandTarget.position, leftHandWeight);
        SolveJoint(leftElbow, leftHand, leftHandTarget.position, leftHandWeight);
        SolveJoint(leftUpperArm, leftHand, leftHandTarget.position, leftHandWeight);

        // 手腕朝向只做平滑跟随，避免把手腕扭成不自然角度。
        leftHand.rotation = Quaternion.Slerp(
            leftHand.rotation, leftHandTarget.rotation, leftHandWeight);
    }

    private void SolveRightArm()
    {
        if (rightHandWeight <= 0.0001f || rightHandTarget == null
            || rightUpperArm == null || rightElbow == null || rightHand == null)
            return;

        SolveJoint(rightElbow, rightHand, rightHandTarget.position, rightHandWeight);
        SolveJoint(rightUpperArm, rightHand, rightHandTarget.position, rightHandWeight);
        SolveJoint(rightElbow, rightHand, rightHandTarget.position, rightHandWeight);
        SolveJoint(rightUpperArm, rightHand, rightHandTarget.position, rightHandWeight);
        rightHand.rotation = Quaternion.Slerp(
            rightHand.rotation, rightHandTarget.rotation, rightHandWeight);
    }

    private static void SolveJoint(Transform joint, Transform tip,
        Vector3 targetPosition, float weight)
    {
        if (joint == null || tip == null)
            return;

        Vector3 toTip = tip.position - joint.position;
        Vector3 toTarget = targetPosition - joint.position;
        if (toTip.sqrMagnitude < 0.000001f || toTarget.sqrMagnitude < 0.000001f)
            return;

        Quaternion delta = Quaternion.FromToRotation(toTip, toTarget);
        joint.rotation = Quaternion.Slerp(Quaternion.identity, delta, weight) * joint.rotation;
    }

    public void SaveTuning()
    {
        if (!initialized || string.IsNullOrEmpty(prefsPrefix))
            return;

        SaveVector($"{prefsPrefix}.LeftPosition", leftHandPositionOffset);
        SaveVector($"{prefsPrefix}.LeftRotation", leftHandRotationOffset);
        SaveVector($"{prefsPrefix}.RightPosition", rightHandPositionOffset);
        SaveVector($"{prefsPrefix}.RightRotation", rightHandRotationOffset);
        SaveVector($"{prefsPrefix}.WeaponPosition", weaponPositionOffset);
        SaveVector($"{prefsPrefix}.WeaponRotation", weaponRotationOffset);
        PlayerPrefs.SetFloat($"{prefsPrefix}.LeftWeight", leftHandWeight);
        PlayerPrefs.SetFloat($"{prefsPrefix}.RightWeight", rightHandWeight);
        PlayerPrefs.SetInt($"{prefsPrefix}.Initialized", 1);
        PlayerPrefs.Save();
        Debug.Log($"[MMDHandIKTuner] {name} 握枪调整已保存");
    }

    public void ResetTuning()
    {
        leftHandPositionOffset = Vector3.zero;
        leftHandRotationOffset = Vector3.zero;
        rightHandPositionOffset = Vector3.zero;
        rightHandRotationOffset = Vector3.zero;
        weaponPositionOffset = Vector3.zero;
        weaponRotationOffset = Vector3.zero;
        leftHandWeight = 1f;
        rightHandWeight = 1f;
        ApplyTuning();
        SaveTuning();
    }

    private void LoadSettings()
    {
        if (!PlayerPrefs.HasKey($"{prefsPrefix}.Initialized"))
            return;

        leftHandPositionOffset = LoadVector($"{prefsPrefix}.LeftPosition");
        leftHandRotationOffset = LoadVector($"{prefsPrefix}.LeftRotation");
        rightHandPositionOffset = LoadVector($"{prefsPrefix}.RightPosition");
        rightHandRotationOffset = LoadVector($"{prefsPrefix}.RightRotation");
        weaponPositionOffset = LoadVector($"{prefsPrefix}.WeaponPosition");
        weaponRotationOffset = LoadVector($"{prefsPrefix}.WeaponRotation");
        leftHandWeight = PlayerPrefs.GetFloat($"{prefsPrefix}.LeftWeight", 1f);
        rightHandWeight = PlayerPrefs.GetFloat($"{prefsPrefix}.RightWeight", 1f);

        // 旧版本可能把权重保存成了 0。只要存在非零偏移，就自动恢复 IK，
        // 避免出现“Target 已经移动，但手臂完全不动”的情况。
        if (leftHandWeight < 0.9999f
            && (leftHandPositionOffset.sqrMagnitude > 0.000001f
                || leftHandRotationOffset.sqrMagnitude > 0.000001f))
            leftHandWeight = 1f;

        if (rightHandWeight < 0.9999f
            && (rightHandPositionOffset.sqrMagnitude > 0.000001f
                || rightHandRotationOffset.sqrMagnitude > 0.000001f))
            rightHandWeight = 1f;
    }

    public void EnableHandIK()
    {
        leftHandWeight = 1f;
        rightHandWeight = 1f;
        ApplyImmediate();
        SaveTuning();
    }

    private void OnDisable()
    {
        ReadTargetTransform();
        SaveTuning();
    }

    private void OnApplicationQuit()
    {
        ReadTargetTransform();
        SaveTuning();
    }

    private void OnDrawGizmos()
    {
        if (!drawTargetGizmos || leftHandTarget == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(leftHandTarget.position, 0.025f);
        Gizmos.DrawLine(leftHandTarget.position,
            leftHandTarget.position + leftHandTarget.forward * 0.12f);

        if (rightHandTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rightHandTarget.position, 0.025f);
            Gizmos.DrawLine(rightHandTarget.position,
                rightHandTarget.position + rightHandTarget.forward * 0.12f);
        }
    }

    private static Transform GetBone(Animator animator, HumanBodyBones bone,
        Transform root, params string[] fallbackNames)
    {
        Transform result = animator != null ? animator.GetBoneTransform(bone) : null;
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
        if (root == null)
            return null;
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

    private static void SaveVector(string key, Vector3 value)
    {
        PlayerPrefs.SetFloat($"{key}.X", value.x);
        PlayerPrefs.SetFloat($"{key}.Y", value.y);
        PlayerPrefs.SetFloat($"{key}.Z", value.z);
    }

    private static Vector3 LoadVector(string key)
    {
        return new Vector3(
            PlayerPrefs.GetFloat($"{key}.X"),
            PlayerPrefs.GetFloat($"{key}.Y"),
            PlayerPrefs.GetFloat($"{key}.Z"));
    }
}
