using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// handles combo
/// </summary>
[RequireComponent (typeof (Text))]
public class PunchEvaluator : MonoBehaviour
{
    public static PunchEvaluator Instance;

    public static System.Action<bool> OnComboUpdate;

    private Text comboCounterText;
    public Text comboRankText;
    public int punchCounter = 0;
    public float ComboTime = 5;

    public int MaxCombo { get; private set; }
    public List<string> rankList;
    public int rank = 0;

    #region Unity Functions
    private void Awake()
    {
        Instance = this;
       
        Block.OnHit -= EnemyBlock;
        Block.OnHit += EnemyBlock;
        Block.OnHit -= SuccessfulPunch;
        Block.OnHit += SuccessfulPunch;

        comboCounterText = GetComponent<Text>();
        comboRankText = comboRankText ?? GameObject.Find("Rank_Text").GetComponent<Text>();
    }
    private void OnDestroy()
    {
        Block.OnHit -= EnemyBlock;
        Block.OnHit -= SuccessfulPunch;
    }
    private void OnDisable()
    {
        Block.OnHit -= EnemyBlock;
        Block.OnHit -= SuccessfulPunch;
    }
    #endregion

    #region Evaluation Functions
    /// <summary>
    /// updates combo when a punch is made
    /// </summary>
    /// <param name="isPlayer">if player or enemy got punched</param>
    /// <param name="damage">damage done</param>
    /// <param name="positionPunch">upper or lower</param>
    void SuccessfulPunch(bool isPlayer, float damage, bool positionPunch, bool success)
    {
        if (success)
        {
            //If the enemey is blocking, set the counter to 0. If not, increase the punch counter by one
            if (!isPlayer)
            {
                punchCounter ++;
                if (punchCounter > MaxCombo) MaxCombo++;
                //if 3 or over, display counter
                if (punchCounter >= 3)
                {
                    comboCounterText.text = (punchCounter + " Hits!");
                    //every 3, increase combo rank
                    if (punchCounter % 3 == 0 && rank < rankList.Count - 1)
                    {
                        rank = (punchCounter / 3) - 1;
                        comboRankText.text = rankList[Mathf.Clamp(rank, 0, rankList.Count - 1)];
                        comboRankText.fontSize = 5 + rank * 2;
                    }
                }
                //reset combo after ComboTime
                CancelInvoke(nameof(ResetCombo));
                Invoke(nameof(ResetCombo), ComboTime);

                OnComboUpdate?.Invoke(true);
            }
            else ResetCombo();
        }
        
    }
    /// <summary>
    /// resets combo if enemy blocks player attack
    /// </summary>
    /// <param name="isPlayer">if player or enemy blocks</param>
    /// <param name="damage">damage done</param>
    /// <param name="positionPunch">upper or lower</param>
    void EnemyBlock(bool isPlayer, float damage, bool positionPunch, bool success)
    {
        if(!success) ResetCombo(!isPlayer);
    }
    #endregion

    /// <summary>
    /// Resets combo
    /// </summary>
    void ResetCombo() => ResetCombo(true);
    /// <summary>
    /// Resets combo
    /// </summary>
    /// <param name="reset">to reset or not</param>
    void ResetCombo(bool reset)
    {
        if (reset)
        {
            punchCounter = 0;
            comboRankText.text = "";
            comboCounterText.text = "";
            OnComboUpdate?.Invoke(false);
        }
    }
}