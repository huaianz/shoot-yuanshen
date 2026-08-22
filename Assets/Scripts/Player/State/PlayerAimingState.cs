using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瞄准状态
/// </summary>
public class PlayerAimingState : PlayerStateBase
{
    private Camera _cam;   // 缓存主相机(避免每帧 Camera.main 查找)

    #region 动画器相关
    private int aimingXHash;
    private int aimingYHash;
    private float aimingX = 0;
    private float aimingY = 0;
    private float transitionSpeed = 5;
    #endregion

    public override void Init(IStateMachineOwner owner)
    {
        base.Init(owner);
        aimingXHash = Animator.StringToHash("AimingX");
        aimingYHash = Animator.StringToHash("AimingY");
    }
    public override void Enter()
    {
        base.Enter();
        playerModel.PlayStateAnimation("Aiming");
        if (IsBeControl())
        {
            _cam = Camera.main;   // 缓存主相机
            if (playerModel.weapon != null) playerModel.weapon.RefreshWeaponData();   // 没挂武器组件也不崩
            UpdateAimingTarget();
            playerController.EnterAim();
        }
    }

    public override void Update()
    {
        base.Update();

        if (IsBeControl())
        {
            //让模型立刻旋转至相机方向
            playerModel.transform.rotation = Quaternion.Euler(0, _cam.transform.eulerAngles.y, 0);
            UpdateAimingTarget();

            #region 退出瞄准监听
            if (!playerController.isAiming && !playerController.isFire)
            {
                //松开瞄准/开枪: 如果还按着下蹲键, 回到下蹲状态, 否则回待机
                playerModel.SwitchState(playerController.isCrouch ? PlayerState.Crouch : PlayerState.Idle);
                return;
            }
            #endregion


            #region 处理移动输入
            aimingX = Mathf.Lerp(aimingX, playerController.moveIput.x, transitionSpeed * Time.deltaTime);
            aimingY = Mathf.Lerp(aimingY, playerController.moveIput.y, transitionSpeed * Time.deltaTime);
            playerModel.animtor.SetFloat(aimingXHash, aimingX);
            playerModel.animtor.SetFloat(aimingYHash, aimingY);
            #endregion

            //让开枪产生声音
            if (playerController.isFire && !UIManager.IsAnyUIOpen)
            {
                playerModel.weapon.Fire(playerController.AimTarget.position);
                playerController.ShakeCamera();
                EventHandler.CallSoundEvent(playerModel.transform.position, 20f);   // 开枪声源,半径20米
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (IsBeControl())
            playerController.ExitAim();
    }

    /// <summary>
    /// 从屏幕中心发射射线确认瞄准位置
    /// </summary>
    private void UpdateAimingTarget()
    {
        //发射射线
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        //如果射线击中了物体
        if (Physics.Raycast(ray, out hit, playerController.maxRayDistance, playerController.aimLayerMask))
        {
            //更新瞄准目标位置
            playerController.AimTarget.position = hit.point;
        }
        else
        {
            playerController.AimTarget.position = ray.origin + ray.direction * playerController.maxRayDistance;
        }
    }
}
