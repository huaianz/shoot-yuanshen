using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions.Must;

public class PlayerController : SingleMonoBase<PlayerController>
{
    //当前所操控的角色模型
    public PlayerModel currentPlayerModel;
    private Transform cameraTransform;

    [Tooltip("正常视角相机")]
    public CinemachineFreeLook freeLookCamera;
    [Tooltip("瞄准视角相机")]
    public CinemachineFreeLook aimingCamera;

    #region 玩家输入相关
    private MyInputSystem input;//输入系统
    [HideInInspector]
    public Vector2 moveIput;//移动输入
    [HideInInspector]
    public bool isSprint;//冲刺输入
    [HideInInspector]
    public bool isAiming;//瞄准输入
    [HideInInspector]
    public bool isJumping;//跳跃输入
    #endregion

    #region 瞄准相关
    [Tooltip("瞄准目标")]
    public Transform AimTarget;
    [Tooltip("射线检测的最大距离")]
    public float maxRayDistance = 1000f;
    [Tooltip("射线检测的层级")]
    public LayerMask aimLayerMask = ~0;
    #endregion
    [Tooltip("转向速度")]
    public float rotationSpeed = 300;

    [HideInInspector]
    public Vector3 localMovement;//本地空间下的玩家移动方向
    [HideInInspector]
    public Vector3 worldMovement;//世界空间下的玩家移动方向
    protected override void Awake()
    {
        base.Awake();
        input = new MyInputSystem();
    }
    void Start()
    {
        cameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;//锁定光标
        ExitAim();
    }

    // Update is called once per frame
    void Update()
    {
        #region 更新玩家输入
        moveIput = input.Player.Move.ReadValue<Vector2>().normalized;
        isSprint = input.Player.IsSprint.IsPressed();
        isAiming = input.Player.IsAiming.IsPressed();
        isJumping = input.Player.IsJumping.IsPressed();
        #endregion

        #region 计算玩家移动方向
        //获取相机的移动方向
        Vector3 cameraForwardProhection = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        //计算世界空间下的方向向量
        worldMovement = cameraForwardProhection * moveIput.y + cameraTransform.right * moveIput.x;
        //将世界空间下的方向向量转换为模型本地看空间下的方向向量
        localMovement = currentPlayerModel.transform.InverseTransformVector(worldMovement);
        #endregion
    }

    /// <summary>
    /// 进入瞄准状态
    /// </summary>
    public void EnterAim()
    {
        //同步瞄准相机和自由相机的旋转角度
        aimingCamera.m_XAxis.Value = freeLookCamera.m_XAxis.Value;
        aimingCamera.m_YAxis.Value = freeLookCamera.m_YAxis.Value;

        //启动瞄准约束
        currentPlayerModel.rightHandAimConstraint.weight = 1;
        currentPlayerModel.BodyAimConstraint.weight = 1;
        currentPlayerModel.rightHandConstraint.weight = 0;

        //设置相机的优先级，使瞄准相机生效
        freeLookCamera.Priority = 0;
        aimingCamera.Priority = 100;
    }

    /// <summary>
    /// 退出瞄准状态
    /// </summary>
    public void ExitAim()
    {
        //同步自由相机和瞄准相机的旋转角度
        freeLookCamera.m_XAxis.Value = aimingCamera.m_XAxis.Value;
        freeLookCamera.m_YAxis.Value = aimingCamera.m_YAxis.Value;

        //关闭瞄准约束
        currentPlayerModel.rightHandAimConstraint.weight = 0;
        currentPlayerModel.BodyAimConstraint.weight = 0;
        currentPlayerModel.rightHandConstraint.weight = 1;

        //设置相机的优先级，使瞄准相机生效
        freeLookCamera.Priority = 100;
        aimingCamera.Priority = 0;
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }
}
