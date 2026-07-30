using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DataUI : MonoBehaviour
{
    [Header("资料UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI birthdayText;
    public TextMeshProUGUI affText;
    public TextMeshProUGUI constellationText;
    public TextMeshProUGUI informationText;

    public void RefreshUI(int roleID = -1)
    {
        if (roleID < 0)
        {
            roleID = GameManager.INSTANCE.GetActiveRoleID();
        }

        if (roleID < 0)
        {
            ClearDataDisplay();
            return;
        }
        var roleData = GameManager.INSTANCE.GetRoleData(roleID);
        if (roleData == null)
        {
            ClearDataDisplay();
            return;
        }
        var baseData = roleData.baseData;
        if (nameText != null)
        {
            nameText.text = baseData.characterName;
        }
        if (birthdayText != null)
        {
            birthdayText.text = baseData.birthday;
        }
        if (affText != null)
        {
            affText.text = baseData.address;
        }
        if (constellationText != null)
        {
            constellationText.text = baseData.constellation;
        }
        if (informationText != null)
        {
            informationText.text = baseData.information;
        }
    }

    public void ClearDataDisplay()
    {
        if (nameText != null)
        {
            nameText.text = "未知";
        }
        if (birthdayText != null)
        {
            birthdayText.text = "";
        }
        if (affText != null)
        {
            affText.text = "";
        }
        if (constellationText != null)
        {
            constellationText.text = "";
        }
        if (informationText != null)
        {
            informationText.text = "";
        }
    }
}
