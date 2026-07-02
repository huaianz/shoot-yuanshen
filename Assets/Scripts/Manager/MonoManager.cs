using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 任务处理管理器
/// </summary>
public class MonoManager : SingleMonoBase<MonoManager>
{
    private Action updateAction;//任务集合

    /// <summary>
    /// 添加任务
    /// </summary>
    /// <param name="task"></param>
    public void AddUpdateAction(Action task)
    {
        updateAction+=task;
    }

    public void RemoveUpdateAction(Action task)
    {
        updateAction-=task;
    }
    void Update()
    {
        updateAction?.Invoke();
    }
}
