using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

/// <summary>
/// 恢复 Lumine/Furina 场景实例中被误删的左右手 Two Bone IK 组件。
/// </summary>
[InitializeOnLoad]
public static class PlayerArmConstraintRestorer
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    static PlayerArmConstraintRestorer()
    {
        EditorApplication.delayCall += RestoreGameSceneAndSave;
    }

    [MenuItem("Tools/Restore Player Arm Constraints")]
    private static void RestoreGameSceneAndSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameScenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
            {
                Debug.LogError($"[PlayerArmConstraintRestorer] 找不到场景: {GameScenePath}");
                return;
            }
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }

        GameObject[] roots = scene.GetRootGameObjects();
        Debug.Log($"[PlayerArmConstraintRestorer] 场景根节点: {string.Join(", ", System.Array.ConvertAll(roots, root => root.name))}");

        int restored = 0;
        foreach (GameObject sceneRoot in roots)
        {
            PlayerModel[] models = sceneRoot.GetComponentsInChildren<PlayerModel>(true);
            foreach (PlayerModel sceneModel in models)
            {
                GameObject root = sceneModel.gameObject;
                if (!root.name.Contains("Lumine") && !root.name.Contains("Pilot Furina"))
                    continue;

                Transform rigs = FindDeep(root.transform, "Rigs");
                Rig rig = rigs != null ? rigs.GetComponent<Rig>() : null;
                RigBuilder builder = root.GetComponent<RigBuilder>();

                Transform leftHand, leftUpper, leftForearm;
                Transform rightHand, rightUpper, rightForearm;
                bool leftArmFound = FindArmBones(root.transform, "L",
                    out leftHand, out leftUpper, out leftForearm);
                bool rightArmFound = FindArmBones(root.transform, "R",
                    out rightHand, out rightUpper, out rightForearm);

                Transform leftTarget = FindDeep(root.transform, "Left Hand Target");
                Transform rightTarget = FindDeep(root.transform, "Right Hand Target");

                if (leftArmFound)
                    restored += RestoreConstraint(root.transform, "Left Hand Constraint",
                        leftUpper, leftForearm, leftHand, leftTarget);
                if (rightArmFound)
                    restored += RestoreConstraint(root.transform, "Right Hand Constraint",
                        rightUpper, rightForearm, rightHand, rightTarget, writePlayerModelField: true);

                if (builder != null && rig != null && builder.layers != null)
                {
                    bool hasLayer = false;
                    foreach (RigLayer layer in builder.layers)
                    {
                        if (layer.rig == rig)
                        {
                            hasLayer = true;
                            break;
                        }
                    }
                    if (!hasLayer)
                        builder.layers.Add(new RigLayer(rig));
                    builder.enabled = true;
                }
            }
        }

        if (restored > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PlayerArmConstraintRestorer] 已恢复 {restored} 个左右手 Two Bone IK");
        }
    }

    private static int RestoreConstraint(Transform root, string constraintName,
        Transform upper, Transform forearm, Transform hand, Transform target,
        bool writePlayerModelField = false)
    {
        if (upper == null || forearm == null || hand == null || target == null)
        {
            Debug.LogWarning($"[PlayerArmConstraintRestorer] {root.name}/{constraintName} 缺少骨骼或 Target");
            return 0;
        }

        Transform constraintTransform = FindDeep(root, constraintName);
        if (constraintTransform == null)
        {
            constraintTransform = new GameObject(constraintName).transform;
            Transform rigs = FindDeep(root, "Rigs");
            constraintTransform.SetParent(rigs != null ? rigs : root, false);
        }

        TwoBoneIKConstraint constraint = constraintTransform.GetComponent<TwoBoneIKConstraint>();
        if (constraint == null)
            constraint = constraintTransform.gameObject.AddComponent<TwoBoneIKConstraint>();

        constraint.weight = 1f;
        constraint.enabled = true;
        constraint.data = new TwoBoneIKConstraintData
        {
            root = upper,
            mid = forearm,
            tip = hand,
            target = target,
            targetPositionWeight = 1f,
            targetRotationWeight = 1f,
            hintWeight = 1f
        };

        if (writePlayerModelField)
        {
            PlayerModel playerModel = root.GetComponent<PlayerModel>();
            if (playerModel != null)
                playerModel.rightHandConstraint = constraint;
        }

        return 1;
    }

    private static bool FindArmBones(Transform root, string side,
        out Transform hand, out Transform upperArm, out Transform forearm)
    {
        string handName = $"DEF-hand.{side}";
        string upperName = $"DEF-upper_arm.{side}";
        string forearmName = $"DEF-forearm.{side}";

        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name != handName) continue;

            Transform foundForearm = null;
            Transform foundUpper = null;
            Transform current = candidate.parent;
            while (current != null && current != root)
            {
                if (foundForearm == null && current.name == forearmName)
                    foundForearm = current;
                if (foundUpper == null && current.name == upperName)
                {
                    foundUpper = current;
                    break;
                }
                current = current.parent;
            }

            if (foundForearm != null && foundUpper != null)
            {
                hand = candidate;
                upperArm = foundUpper;
                forearm = foundForearm;
                return true;
            }
        }

        hand = null;
        upperArm = null;
        forearm = null;
        Debug.LogWarning($"[PlayerArmConstraintRestorer] {root.name}/{side} 手找不到完整的上臂-前臂骨骼链");
        return false;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
