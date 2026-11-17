using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>准备大厅里的一个席位</summary>
public class LobbySlotUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text emptyHint;
    public Image avatar;
    public Image readyIcon;
    public Button button;

    public void Bind(LobbyPlayerDto player, bool isSelf, System.Action onClick)
    {
        bool empty = player == null;

        if (nameText != null)
        {
            nameText.text = empty ? "" : (isSelf ? player.name + "（你）" : player.name);
            nameText.color = isSelf ? new Color(1f, 0.9f, 0.5f) : Color.white;
        }

        if (emptyHint != null) emptyHint.gameObject.SetActive(empty);

        if (readyIcon != null)
        {
            readyIcon.gameObject.SetActive(!empty && player.ready);
            readyIcon.color = new Color(0.4f, 1f, 0.55f);
        }

        if (avatar != null)
            avatar.gameObject.SetActive(!empty);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
