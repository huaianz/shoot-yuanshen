using UnityEngine;
using UnityEngine.UI;

/// <summary>退出联机房间的按钮</summary>
public class RoomLeaveButton : MonoBehaviour
{
    [Header("按钮")]
    public Button button;

    [Header("可选: 退出后要隐藏/关闭的界面")]
    public GameObject roomHud;
    public GameObject roomPanel;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(Leave);
    }

    private void Update()
    {
        if (button != null)
            button.interactable = RoomSession.InRoom;
    }

    public void Leave()
    {
        if (!RoomSession.InRoom)
        {
            ToastUI.ShowMessage("当前不在房间里", new Color(1f, 0.65f, 0.35f));
            return;
        }

        RoomSession.Leave();

        if (roomHud != null) roomHud.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);

        ToastUI.ShowMessage("已退出房间", new Color(0.8f, 0.95f, 1f));
    }
}
