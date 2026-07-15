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

    private void Start()
    {
        //初始化   
        InitData();
    }

    /// <summary>
    /// 初始化资料界面
    /// </summary>
    public void InitData()
    {
        nameText.text = Player.INSTANCE.currentCharacter.characterName;
        birthdayText.text = Player.INSTANCE.currentCharacter.birthday;
        affText.text = Player.INSTANCE.currentCharacter.address;
        constellationText.text = Player.INSTANCE.currentCharacter.constellation;
        informationText.text = Player.INSTANCE.currentCharacter.information;
    }
}
