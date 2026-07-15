using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttibuteUI : MonoBehaviour
{
    [Header("属性界面UI")]
    #region 属性界面UI
    public TextMeshProUGUI CharacterName;
    public TextMeshProUGUI CharacterLevel;
    public TextMeshProUGUI Hp;
    public TextMeshProUGUI Atk;
    public TextMeshProUGUI Def;
    public TextMeshProUGUI description;
    #endregion



    private void Start()
    {
        //初始化
        InitAttibute();
    }

    /// <summary>
    /// 初始化属性
    /// </summary>
    public void InitAttibute()
    {
        CharacterName.text = Player.INSTANCE.currentCharacter.characterName;
        CharacterLevel.text = $"等级{Player.INSTANCE.currentCharacter.characterLevel}/90";
        Hp.text = Player.INSTANCE.currentCharacter.characterHP.ToString();
        Atk.text = Player.INSTANCE.currentCharacter.characterATK.ToString();
        Def.text = Player.INSTANCE.currentCharacter.characterDEF.ToString();
        description.text = Player.INSTANCE.currentCharacter.description;
    }

}
