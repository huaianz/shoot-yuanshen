using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 对话数据资产
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/DialogueData")]
public class DialogueData_SO : ScriptableObject
{
    [Header("对话基础数据")]
    public int dialogueID;
    public string speakerName;
    public Sprite speakerAvatar;
    [Header("对话内容")]
    [TextArea(3, 6)]
    public string dialogueText;
    [Header("对话选项")]
    public List<DialogueOption> options;
}
