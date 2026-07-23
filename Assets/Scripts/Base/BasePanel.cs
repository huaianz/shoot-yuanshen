using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUIPanel : MonoBehaviour
{
    public bool isRemove = false;
    protected new string name;

    protected virtual void Awake()
    {

    }

    /// <summary>
    /// 启动和禁止面板
    /// </summary>
    /// <param name="active"></param>
    public virtual void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    /// <summary>
    /// 打开面板
    /// </summary>
    /// <param name="name"></param>
    public virtual void OpenPanel(string name)
    {
        this.name = name;
        SetActive(true);
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    public virtual void ClosePanel()
    {
        isRemove = true;
        SetActive(false);
        Destroy(gameObject);
    }
}
