using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("NPC类型")]
    public NPCType npcType = NPCType.Dialogue;

    [Header("对话配置")]
    public int dialogueID = 1;
    [Header("商店配置")]
    public int shopID = 1;
    [Header("模型与动画")]
    public Animator npcAnimator;
    public string idleAnimName = "Idle";
    public string thinkAnimName = "Think";
    public string talkAnimName = "Talk";

    private InteractionUI _interactionUI;
    private bool _isInRange = false;
    private bool _isInDialogue = false;

    private void Awake()
    {
        if (npcAnimator != null)
        {
            npcAnimator = GetComponentInChildren<Animator>();
        }
        //通过单例获取，零查找开销
        _interactionUI = InteractionUI.INSTANCE;
    }

    private void OnEnable()
    {
        EventHandler.DialogueStartEvent += OnDialogueStart;
        EventHandler.DialogueEndEvent += OnDialogueEnd;
        EventHandler.DialogueThinkEvent += OnDialogueThink;
        EventHandler.DialogueTalkEvent += OnDialogueTalk;
        EventHandler.ShopClosedEvent += OnShopClosed;
    }

    private void OnDisable()
    {
        EventHandler.DialogueStartEvent -= OnDialogueStart;
        EventHandler.DialogueEndEvent -= OnDialogueEnd;
        EventHandler.DialogueThinkEvent -= OnDialogueThink;
        EventHandler.DialogueTalkEvent -= OnDialogueTalk;
        EventHandler.ShopClosedEvent -= OnShopClosed;
    }

    private void Update()
    {
        if (_isInRange && !_isInDialogue && Input.GetKeyDown(KeyCode.F))
        {
            Interact();
        }
    }

    private void Interact()
    {
        switch (npcType)
        {
            case NPCType.Dialogue:
                DialogueManager.INSTANCE.StartDialogue(dialogueID);
                break;
            case NPCType.Shop:
                EventHandler.CallOpenShopEvent(shopID);
                break;
            case NPCType.Quest:
                Debug.Log("任务NPC交互");
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isInRange = true;
            if (_interactionUI != null)
            {
                string hint = npcType == NPCType.Shop ? "按 F 打开商店" : "按 F 对话";
                _interactionUI.ShowHint(hint);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isInRange = false;
            if (_interactionUI != null)
                _interactionUI.HideHint();
        }
    }

    #region 事件回调
    private void OnDialogueStart(int id)
    {
        if (id != dialogueID)
        {
            return;
        }
        _isInDialogue = true;
        PlayAnimation(thinkAnimName);
        if (_interactionUI != null)
        {
            _interactionUI.HideHint();
        }
    }

    private void OnDialogueEnd()
    {
        _isInDialogue = false;
        PlayAnimation(idleAnimName);
        if (_isInRange && _interactionUI != null)
        {
            string hint = npcType == NPCType.Shop ? "按 F 打开商店" : "按 F 对话";
            _interactionUI.ShowHint(hint);
        }
    }
    private void OnDialogueThink()
    {
        PlayAnimation(thinkAnimName);
    }


    private void OnDialogueTalk()
    {
        PlayAnimation(talkAnimName);
    }

    private void OnShopClosed()
    {
        if (npcType == NPCType.Shop && _isInRange && _interactionUI != null)
        {
            _interactionUI.ShowHint("按 F 打开商店");
        }
    }

    private void PlayAnimation(string animName)
    {
        if (npcAnimator == null || string.IsNullOrEmpty(animName))
        {
            return;
        }

        var currentState = npcAnimator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName(animName))
        {
            return;
        }

        npcAnimator.Play(animName, 0, 0f);
    }
    #endregion
}
