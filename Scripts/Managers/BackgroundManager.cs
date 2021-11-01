using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sets <see cref="Image"/> in <see cref="Transform.GetChild(int)"/> to proper background depending on <see cref="GameManager.StoryLevel"/>
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    public List<Sprite> punchingBagBackgroundList;
    public List<Sprite> tier0BackgroundList;
    public List<Sprite> tier1BackgroundList;
    public List<Sprite> tier2BackgroundList;
    public List<Sprite> bossBackgroundList;

    public List<Sprite> backgroundList;

    /// <summary>
    /// Sets <see cref="Image"/> in <see cref="Transform.GetChild(int)"/> to proper background depending on <see cref="GameManager.StoryLevel"/>
    /// </summary>
    void Start()
    {
        Sprite s;

        if (GameManager.Instance.currentGameMode == GameMode.StoryMode)
        {
            int level = GameManager.Instance.StoryLevel.Value;
            if (level < GameManager.Instance.ForcedRespiteLevels[0]) s = punchingBagBackgroundList[Random.Range(0, punchingBagBackgroundList.Count)];
            else if (level < GameManager.Instance.ForcedRespiteLevels[1]) s = tier0BackgroundList[Random.Range(0, tier0BackgroundList.Count)];
            else if (level < GameManager.Instance.ForcedRespiteLevels[2]) s = tier1BackgroundList[Random.Range(0, tier1BackgroundList.Count)];
            else if (level < GameManager.Instance.ForcedRespiteLevels[3]) s = tier2BackgroundList[Random.Range(0, tier2BackgroundList.Count)];
            else s = bossBackgroundList[Random.Range(0, bossBackgroundList.Count)];
        }
        //If in rematch or arcade, we get any background except respite, which is the last element
        else s = backgroundList[Random.Range(0, backgroundList.Count - 1)];
        GetComponentInChildren<Image>().sprite = s;
    }
}