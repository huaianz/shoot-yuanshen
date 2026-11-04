using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第一人称 / 第三人称切换
/// </summary>
public class PlayerViewMode : MonoBehaviour
{
    [Header("相机引用")]
    public CinemachineFreeLook thirdPersonCamera;
    public CinemachineFreeLook aimingCamera;
    public CinemachineVirtualCamera firstPersonCamera;

    [Header("第一人称机位")]
    public Transform fpRig;
    public float headHeight = 1.55f;
    public float headForward = 0.08f;

    [Header("手感")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = -75f;
    public float maxPitch = 75f;
    public float bodyTurnSpeed = 18f;

    [Header("按键")]
    public KeyCode toggleKey = KeyCode.V;

    [Header("第一人称要隐藏的头部部件")]
    [Tooltip("留空则按名字自动匹配: Face / Head / Hair / Glasses / Eye")]
    public Renderer[] headRenderers;
    public string[] headNameKeywords = { "Face", "Head", "Hair", "Glasses", "Eye" };

    public bool IsFirstPerson { get; private set; }

    private float _yaw;
    private float _pitch;
    private Transform _hiddenModel;
    private readonly List<Renderer> _hidden = new List<Renderer>();
    private readonly List<bool> _hiddenStates = new List<bool>();

    private void Start()
    {
        SetMode(false, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();

        if (!IsFirstPerson || fpRig == null) return;

        var pc = PlayerController.INSTANCE;
        if (pc == null || pc.currentPlayerModel == null) return;

        Transform model = pc.currentPlayerModel.transform;

        // 换角色时重新隐藏新模型
        if (_hiddenModel != model)
        {
            RestoreModel();
            HideModel(model);
        }

        Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        _yaw += delta.x * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, minPitch, maxPitch);

        fpRig.position = model.position + Vector3.up * headHeight + model.forward * headForward;
        fpRig.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // 第一人称时身体跟随视角水平朝向
        model.rotation = Quaternion.Slerp(model.rotation,
            Quaternion.Euler(0f, _yaw, 0f), bodyTurnSpeed * Time.deltaTime);

        // 瞄准会改相机优先级, 这里每帧强制第一人称优先
        if (thirdPersonCamera != null) thirdPersonCamera.Priority = 0;
        if (aimingCamera != null) aimingCamera.Priority = 0;
        if (firstPersonCamera != null) firstPersonCamera.Priority = 200;
    }

    public void Toggle()
    {
        SetMode(!IsFirstPerson, false);
    }

    public void SetMode(bool firstPerson, bool force)
    {
        if (!force && IsFirstPerson == firstPerson) return;
        IsFirstPerson = firstPerson;

        var pc = PlayerController.INSTANCE;
        if (pc != null) pc.IsFirstPerson = firstPerson;

        if (firstPerson)
        {
            if (firstPersonCamera != null) firstPersonCamera.Priority = 200;
            if (thirdPersonCamera != null) thirdPersonCamera.Priority = 0;
            if (aimingCamera != null) aimingCamera.Priority = 0;

            _yaw = pc != null && pc.currentPlayerModel != null
                ? pc.currentPlayerModel.transform.eulerAngles.y
                : 0f;
            _pitch = 0f;

            if (pc != null && pc.currentPlayerModel != null)
                HideModel(pc.currentPlayerModel.transform);
        }
        else
        {
            if (firstPersonCamera != null) firstPersonCamera.Priority = 0;

            bool aiming = pc != null && pc.isAiming;
            if (aimingCamera != null) aimingCamera.Priority = aiming ? 100 : 0;
            if (thirdPersonCamera != null) thirdPersonCamera.Priority = aiming ? 0 : 100;

            // 回到第三人称时保持水平朝向
            if (thirdPersonCamera != null) thirdPersonCamera.m_XAxis.Value = _yaw;

            RestoreModel();
        }
    }

    private void HideModel(Transform model)
    {
        _hiddenModel = model;
        _hidden.Clear();
        _hiddenStates.Clear();

        // 手动指定了头部部件就只隐藏这些
        if (headRenderers != null && headRenderers.Length > 0)
        {
            foreach (var r in headRenderers) HideRenderer(r);
            return;
        }

        // 否则按名字自动匹配, 身体/手臂/武器保持可见
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (IsHeadPart(r.gameObject.name)) HideRenderer(r);
        }
    }

    private void HideRenderer(Renderer r)
    {
        if (r == null) return;

        _hidden.Add(r);
        _hiddenStates.Add(r.enabled);
        r.enabled = false;
    }

    private bool IsHeadPart(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || headNameKeywords == null) return false;

        foreach (var key in headNameKeywords)
        {
            if (string.IsNullOrEmpty(key)) continue;
            if (objectName.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private void RestoreModel()
    {
        for (int i = 0; i < _hidden.Count; i++)
            if (_hidden[i] != null)
                _hidden[i].enabled = _hiddenStates[i];

        _hidden.Clear();
        _hiddenStates.Clear();
        _hiddenModel = null;
    }
}
