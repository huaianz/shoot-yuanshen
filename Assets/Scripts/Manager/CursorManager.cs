using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : SingleMonoBase<CursorManager>
{
    private MyInputSystem inputActions;
    protected override void Awake()
    {
        base.Awake();
        inputActions = new MyInputSystem();
    }

    private void Start()
    {
        //游戏开始时默认锁定并隐藏光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        //启用输入系统
        inputActions.Enable();
        //订阅Alt事件
        inputActions.Player.ShowCursor.performed += OnAltPerformed;
        inputActions.Player.ShowCursor.canceled += OnAltCanceled;
    }
    private void OnDisable()
    {
        //禁用输入系统
        inputActions.Disable();
        //取消订阅Alt事件
        inputActions.Player.ShowCursor.performed -= OnAltPerformed;
        inputActions.Player.ShowCursor.canceled -= OnAltCanceled;
    }

    #region Alt事件回调
    private void OnAltPerformed(InputAction.CallbackContext context)
    {
        // Alt 被按住时，显示并解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnAltCanceled(InputAction.CallbackContext context)
    {
        // Alt 被松开时，锁定并隐藏鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    #endregion

}
