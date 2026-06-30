using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : SingleMonoBase<PlayerController>
{
    //当前所操控的角色模型
    public PlayerModel currentPlayerModel;
    private Transform cameraTransform;
 

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

    [Tooltip("转向速度")]
    public float rotationSpeed=300;

    [HideInInspector]
    public Vector3 localMovement;//本地空间下的玩家移动方向
    [HideInInspector]
    public Vector3 worldMovement;//世界空间下的玩家移动方向
    protected override void Awake()
    {
        base.Awake();
        input =new MyInputSystem();
    }
    void Start()
    {
        cameraTransform=Camera.main.transform;
        Cursor.lockState=CursorLockMode.Locked;//锁定光标
    }

    // Update is called once per frame
    void Update()
    {
        #region 更新玩家输入
        moveIput=input.Player.Move.ReadValue<Vector2>().normalized;
        isSprint=input.Player.IsSprint.IsPressed();
        isAiming=input.Player.IsAiming.IsPressed();
        isJumping=input.Player.IsJumping.IsPressed();
        #endregion

        #region 计算玩家移动方向
        //获取相机的移动方向
        Vector3 cameraForwardProhection=new Vector3(cameraTransform.forward.x,0,cameraTransform.forward.z).normalized;
        //计算世界空间下的方向向量
        worldMovement=cameraForwardProhection*moveIput.y+cameraTransform.right*moveIput.x;
        //将世界空间下的方向向量转换为模型本地看空间下的方向向量
        localMovement=currentPlayerModel.transform.InverseTransformVector(worldMovement);
        #endregion
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
