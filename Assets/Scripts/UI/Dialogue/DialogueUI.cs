using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话UI
/// </summary>
public class DialogueUI : SingleMonoBase<DialogueUI>
{
    [Header("UI组件")]
    public GameObject dialoguePanel;
    public Image speakerAvatar;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Transform optionContainer;
    public GameObject optionPrefab;

    //储存当前可用的选项按钮
    private Queue<GameObject> _optionPool = new Queue<GameObject>();
    //当前正在使用的选项按钮
    private List<GameObject> _activeOptions = new List<GameObject>();

    protected override void Awake()
    {
        //默认隐藏对话面板
        dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// 显示对话
    /// </summary>
    /// <param name="dialogue"></param>
    public void ShowDialogue(DialogueData_SO dialogue)
    {
        if (dialogue == null)
        {
            return;
        }
        //显示面板
        dialoguePanel.SetActive(true);

        //更新UI信息
        if (speakerAvatar != null)
        {
            speakerAvatar.sprite = dialogue.speakerAvatar;
        }
        if (speakerNameText != null)
        {
            speakerNameText.text = dialogue.speakerName;
        }
        if (dialogueText != null)
        {
            dialogueText.text = dialogue.dialogueText;
        }

        //发送说话事件
        EventHandler.CallDialogueTalkEvent();

        //清空旧选项按钮
        ClearOptions();

        //生成新选项按钮
        if (dialogue.options != null && dialogue.options.Count > 0)
        {
            //发送思考事件
            EventHandler.CallDialogueThinkEvent();

            //遍历所有选项，创建对应的按钮
            foreach (var option in dialogue.options)
            {
                CreateOptionButton(option);
            }
        }
        else
        {
            //如果没有选项，对话在两秒后自动结束
            StartCoroutine(AutoEndDialogue(2f));
        }
    }

    /// <summary>
    /// 创建选项按钮
    /// </summary>
    /// <param name="option"></param>
    private void CreateOptionButton(DialogueOption option)
    {
        if (optionPrefab == null || optionContainer == null)
        {
            return;
        }

        //从对象池中取出一个按钮
        GameObject btnGO = GetFromPool();
        btnGO.transform.SetParent(optionContainer, false);
        btnGO.SetActive(true);


        //设置按钮文字
        var text = btnGO.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = option.optionText;
        }

        var btn = btnGO.GetComponent<Button>();
        if (btn != null)
        {
            //清除旧的监听器，避免重复注册
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                DialogueManager.INSTANCE.SelectOption(option);
            });
        }

        //添加到活动选项列表
        _activeOptions.Add(btnGO);
    }

    /// <summary>
    /// 从对象池里获取按钮
    /// </summary>
    /// <returns></returns>
    private GameObject GetFromPool()
    {
        if (_optionPool.Count > 0)
        {
            var go = _optionPool.Dequeue();
            return go;
        }
        //池子中没有就创建一个新的
        return Instantiate(optionPrefab);
    }

    /// <summary>
    /// 清除选项
    /// </summary>
    private void ClearOptions()
    {
        foreach (var btn in _activeOptions)
        {
            //重置按钮状态
            btn.SetActive(false);
            btn.transform.SetParent(null);//临时移除层级
            //放入池子里
            _optionPool.Enqueue(btn);
        }
        _activeOptions.Clear();
    }

    /// <summary>
    /// 隐藏对话面板
    /// </summary>
    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        ClearOptions();
    }

    private System.Collections.IEnumerator AutoEndDialogue(float delay)
    {
        yield return new WaitForSeconds(delay);
        DialogueManager.INSTANCE?.EndDialogue();
    }

    protected override void OnDestroy()
    {
        // 清理资源
        ClearOptions();
    }
}
