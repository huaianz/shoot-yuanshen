using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : SingleMonoBase<PlayerController>
{
    //当前所操控的角色模型
    public PlayerModel currentPlayerModel;
    private MyInputSystem input;//输入系统

    #region 玩家输入相关
    public Vector2 moveIput;//移动输入
    public bool isSprint;//冲刺输入
    public bool isAiming;//瞄准输入
    public bool isJumping;//跳跃输入
    #endregion
    protected override void Awake()
    {
        base.Awake();
        input =new MyInputSystem();
    }
    void Start()
    {
        
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
