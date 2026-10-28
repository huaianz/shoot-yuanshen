using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>房间列表里的单项</summary>
public class RoomItemUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text countText;
    public TMP_Text stateText;
    public Image stateDot;
    public Button button;

    private string _roomId;
    private Action<string> _onClick;

    public void Bind(RoomInfo info, Action<string> onClick)
    {
        _roomId = info.roomId;
        _onClick = onClick;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(info.name) ? info.roomId : info.name;

        if (countText != null)
            countText.text = info.players + "/" + info.maxPlayers;

        if (stateText != null)
            stateText.text = string.IsNullOrEmpty(info.state) ? "等待中" : info.state;

        if (stateDot != null)
            stateDot.color = info.players >= info.maxPlayers
                ? new Color(1f, 0.45f, 0.35f)
                : new Color(0.45f, 1f, 0.65f);

        bool full = info.players >= info.maxPlayers;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_roomId));
            button.interactable = !full;   // 满员不可点
        }
    }
}