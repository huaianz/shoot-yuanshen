using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStateMachineOwner{} //状态机宿主
/// <summary>
/// 角色状态机
/// </summary>
public class StateMachine : MonoBehaviour
{
    private StateBase currentState;
    private IStateMachineOwner owner;//状态宿主

    
}
