using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MMDHandIKTuner))]
public sealed class MMDHandIKTunerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MMDHandIKTuner tuner = (MMDHandIKTuner)target;
        Vector3 leftPositionBefore = tuner.leftHandPositionOffset;
        Vector3 leftRotationBefore = tuner.leftHandRotationOffset;
        Vector3 rightPositionBefore = tuner.rightHandPositionOffset;
        Vector3 rightRotationBefore = tuner.rightHandRotationOffset;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            bool leftOffsetChanged =
                leftPositionBefore != tuner.leftHandPositionOffset
                || leftRotationBefore != tuner.leftHandRotationOffset;
            bool rightOffsetChanged =
                rightPositionBefore != tuner.rightHandPositionOffset
                || rightRotationBefore != tuner.rightHandRotationOffset;

            if (leftOffsetChanged && tuner.leftHandWeight < 0.9999f)
                tuner.leftHandWeight = 1f;
            if (rightOffsetChanged && tuner.rightHandWeight < 0.9999f)
                tuner.rightHandWeight = 1f;

            tuner.ApplyImmediate();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "进入 Play 后切换 F3(甘雨) 或 F4(宵宫)，再调整下面的偏移。\n" +
            "右手 Target 移动整条右臂和枪；左手 Target 控制左手扶枪位置；" +
            "WeaponMount 微调枪在右手掌中的位置和角度。",
            MessageType.Info);

        if (!Application.isPlaying || !tuner.IsInitialized)
        {
            EditorGUILayout.HelpBox(
                "进入 Play 模式并切换到甘雨或宵宫后，才能实时预览和调整。",
                MessageType.Warning);
        }
        else if (!tuner.gameObject.activeInHierarchy)
        {
            EditorGUILayout.HelpBox(
                "当前角色未激活。你修改的是后台角色，Game 视图中不会显示变化。",
                MessageType.Warning);
            if (GUILayout.Button("切换到此角色"))
                tuner.ActivateThisRole();
        }

        if (Application.isPlaying && tuner.IsInitialized
            && tuner.leftHandWeight < 0.9999f)
        {
            EditorGUILayout.HelpBox(
                $"左手 Weight={tuner.leftHandWeight:F2}，偏移只会部分生效。建议设为 1。",
                MessageType.Warning);
        }
        if (Application.isPlaying && tuner.IsInitialized
            && tuner.rightHandWeight < 0.9999f)
        {
            EditorGUILayout.HelpBox(
                $"右手 Weight={tuner.rightHandWeight:F2}，偏移只会部分生效。建议设为 1。",
                MessageType.Warning);
        }

        if (GUILayout.Button("启用左右手 IK（Weight = 1）"))
            tuner.EnableHandIK();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存当前调整"))
            tuner.SaveTuning();
        if (GUILayout.Button("恢复默认"))
            tuner.ResetTuning();
        if (GUILayout.Button("立即应用 IK"))
            tuner.RebuildRig();
        if (GUILayout.Button("读取 Target 拖动值"))
            tuner.ReadTargetTransform();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(tuner.LeftHandTarget == null))
        {
            if (GUILayout.Button("选中左手 Target"))
                Selection.activeGameObject = tuner.LeftHandTarget.gameObject;
        }
        using (new EditorGUI.DisabledScope(tuner.RightHandTarget == null))
        {
            if (GUILayout.Button("选中右手 Target"))
                Selection.activeGameObject = tuner.RightHandTarget.gameObject;
        }
        using (new EditorGUI.DisabledScope(tuner.WeaponMount == null))
        {
            if (GUILayout.Button("选中 WeaponMount"))
                Selection.activeGameObject = tuner.WeaponMount.gameObject;
        }
        EditorGUILayout.EndHorizontal();
    }
}
