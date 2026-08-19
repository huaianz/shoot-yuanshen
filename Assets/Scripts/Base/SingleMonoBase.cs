using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 单例限制器
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingleMonoBase<T> : MonoBehaviour where T :SingleMonoBase<T>
{
    public static T INSTANCE;

    protected virtual void Awake()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            // 重复实例(比如误挂了两份): 自动销毁自己, 保留第一个, 不再报错
            Destroy(gameObject);
            return;
        }
        INSTANCE = (T)this;
    }

    protected virtual void OnDestroy()
    {
        INSTANCE = null;
    }
}