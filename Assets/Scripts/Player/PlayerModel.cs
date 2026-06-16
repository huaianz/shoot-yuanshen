using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public enum PlayerState
{
    Idle
}
/// <summary>
/// 角色模型
/// </summary>
public class PlayerModel : MonoBehaviour,IStateMachineOwner
{
    //[HideInInspector]的作用是隐藏，因为在Awake中有获取组件
    [HideInInspector]
    public Animator animtor;
    private StateMachine stateMachine;//动画状态机
    private PlayerState currentState;//当前状态
    private void Awake()
    {
        stateMachine = new StateMachine(this);
        animtor =GetComponent<Animator>();
    }

    void Start()
    {
        SwitchState(PlayerState.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void SwitchState(PlayerState state)
    {
        switch(state)
        {
            case PlayerState.Idle:
                stateMachine.EnterState<PlayerIdleState>();
                break;
        }
        currentState=state;
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="animationName">动画名称</param>
    /// <param name="transition">过渡时间</param>
    /// <param name="layer">动画层级</param>
    public void PlayStateAnimation(string animationName,float transition=0.25f,int layer = 0)
    {
        animtor.CrossFadeInFixedTime(animationName,transition,layer);
    }
}
