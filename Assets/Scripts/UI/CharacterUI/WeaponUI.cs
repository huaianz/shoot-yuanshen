using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("佩戴武器UI")]
    public TextMeshProUGUI weaponName;
    public TextMeshProUGUI weaponType;
    public TextMeshProUGUI weaponAtk;
    public TextMeshProUGUI fireRate;
    public TextMeshProUGUI BulletNum;
    public TextMeshProUGUI weaponDesc;


    private void Start()
    {
        //初始化
        InitWeapon();
    }
    public void InitWeapon()
    {
        weaponName.text = Player.INSTANCE.currentWeapon.weaponName;
        weaponType.text = Player.INSTANCE.currentWeapon.weaponType;
        weaponAtk.text = Player.INSTANCE.currentWeapon.weaponATK.ToString();
        fireRate.text = Player.INSTANCE.currentWeapon.fireRate.ToString();
        BulletNum.text = Player.INSTANCE.currentWeapon.BulletNum.ToString();
        weaponDesc.text = Player.INSTANCE.currentWeapon.weaponDescription;
    }
}
