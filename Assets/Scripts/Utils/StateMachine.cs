using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStateMachineOwner{} //状态机宿主
/// <summary>
/// 角色状态机
/// </summary>
public class StateMachine
{
    //当前状态
    private StateBase currentState;
    private IStateMachineOwner owner;//状态宿主
    //状态字典
    private Dictionary<Type,StateBase> stateDic=new Dictionary<Type, StateBase>();

    /// <summary>
    /// 进入动画状态
    /// </summary>
    /// <typeparam name="T">状态实例</typeparam>
    public void EnterState<T>()where T: StateBase,new()
    {
        //防止重复进入同一个动画状态
        if(currentState.GetType()==typeof(T))
            return;
        if(currentState!=null)
            currentState.Exit();
        currentState=LoadState<T>();
        currentState.Enter();
    }


    /// <summary>
    /// 尝试从字典中取出状态
    /// </summary>
    /// <typeparam name="T">状态类</typeparam>
    /// <returns></returns>
    private StateBase LoadState<T>() where T : StateBase, new()
    {
        Type stateType = typeof(T);
        if(!stateDic.TryGetValue(stateType,out StateBase state))
        {
            state = new T();
            state.Init(owner);
            //将新创建的状态记录到字典中
            stateDic.Add(stateType,state);
        }
        return state;
    }


    public void Stop()
    {
        if(currentState!=null)
        currentState.Exit();
        foreach(var state in stateDic.Values)
            state.Destroy();
        stateDic.Clear();
    }
}
