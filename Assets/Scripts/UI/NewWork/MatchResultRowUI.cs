using TMPro;
using UnityEngine;

/// <summary>结算战绩表里的一行</summary>
public class MatchResultRowUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text teamText;
    public TMP_Text killsText;
    public TMP_Text deathsText;
    public TMP_Text assistsText;

    public void Bind(MatchResultPlayerDto dto, bool isSelf)
    {
        if (nameText != null)
        {
            nameText.text = isSelf ? dto.name + "（你）" : dto.name;
            nameText.color = isSelf ? new Color(1f, 0.9f, 0.5f) : Color.white;
        }

        if (teamText != null)
            teamText.text = dto.team == 0 ? "进攻" : "防守";

        if (killsText != null) killsText.text = dto.kills.ToString();
        if (deathsText != null) deathsText.text = dto.deaths.ToString();
        if (assistsText != null) assistsText.text = dto.assists.ToString();
    }
}
