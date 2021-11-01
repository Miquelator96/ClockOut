using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles punching for a character depending on <see cref="IsPlayer"/>
/// </summary>
public class Punch : MonoBehaviour
{
    //first = isPlayer, second = damage, third = positionPunch
    public static System.Action<bool, float, bool> OnTakePunch;
    public static System.Action<bool, bool> OnThrowPunch;

    #region Variables
    static List<Punch> Instances = new List<Punch>();

    public bool IsPlayer;
    bool playerAndInFrenzy = false;
    float playerExhaustedModifier = 1f;

    //Damage stuff
    private float Power;
    const float UpperMultiplier = 1.3f;
    const float LowerMultiplier = 0.77f;
    #endregion

    #region Unity Functions
    private void Awake()
    {
        Instances.Add(this);

        PlayerInput.OnInputPunch -= CallPunch;
        PlayerInput.OnInputPunch += CallPunch;
        OpponentAI.OnAIPunch -= CallPunch;
        OpponentAI.OnAIPunch += CallPunch;
        Frenzy.OnFrenzy -= UpdateEnemyFrenzy;
        Frenzy.OnFrenzy += UpdateEnemyFrenzy;
        PlayerInput.OnRecoverFromExhaustion -= ResetExhaustedModifier;
        PlayerInput.OnRecoverFromExhaustion += ResetExhaustedModifier;
        Stamina.OnExhaustion -= ApplyExhaustedModifier;
        Stamina.OnExhaustion += ApplyExhaustedModifier;

    }
    private void OnDestroy()
    {
        Instances.Remove(this);

        OpponentAI.OnAIPunch -= CallPunch;
        PlayerInput.OnInputPunch -= CallPunch;
        Frenzy.OnFrenzy -= UpdateEnemyFrenzy;
        PlayerInput.OnRecoverFromExhaustion -= ResetExhaustedModifier;
        Stamina.OnExhaustion -= ApplyExhaustedModifier;
    }
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Console.LogWarning("GameManager uninitialized, ignore if moved to initialize scene.");
            return;
        }
        AssignPower();
    }
    #endregion

    #region Exhaustion
    void ResetExhaustedModifier(bool isPlayer)
    {
        if (isPlayer) playerExhaustedModifier = 1f;
    }

    void ApplyExhaustedModifier(bool isPlayer)
    {
        Console.Log("applying modifier");
        if (isPlayer) playerExhaustedModifier = 1.5f;
    }
    #endregion

    #region Frenzy
    private void UpdateEnemyFrenzy(bool inFrenzy, bool isPlayer) => playerAndInFrenzy = inFrenzy && IsPlayer;
    #endregion

    #region Punch Stuff
    /// <summary>
    /// Handles Input to call punch
    /// </summary>
    /// <param name="isPlayer">if player or enemy wants to punch</param>
    /// <param name="position">upper or lower</param>
    void CallPunch(bool isPlayer, bool position)
    {
        if (isPlayer == IsPlayer)
        {
            string triggerName = position ? "Upper_Punch" : "Lower_Punch";
            AnimationManager.AnimationTrigger(IsPlayer, triggerName);
            OnThrowPunch?.Invoke(IsPlayer, position);
        }
    }
    /// <summary>
    /// called via animation event, punch connected
    /// </summary>
    /// <param name="position">upper or lower</param>
    public void DoPunch(int position)
    {
        bool intPos = position == 1;
        float multiplier = intPos ? UpperMultiplier : LowerMultiplier;

        if (playerAndInFrenzy) multiplier *= 0.35f;
        if (!IsPlayer) multiplier *= playerExhaustedModifier;

        Instances.Find(x => x != this).TakePunch(Mathf.RoundToInt(Power * multiplier), intPos);
    }
    /// <summary>
    /// Punch has been taken
    /// </summary>
    /// <param name="damage">damage taken</param>
    /// <param name="positionPunch">upper or lower</param>
    protected void TakePunch(float damage, bool positionPunch) => OnTakePunch?.Invoke(IsPlayer, damage, positionPunch);
    #endregion

    /// <summary>
    /// Sets <see cref="Power"/> using <see cref="GameManager"/> and <see cref="EnemyCreator"/>
    /// </summary>    
    void AssignPower()
    {
        Stats s;
        if (!IsPlayer) s = EnemyCreator.Animal.enemyStatsType;
        else if (GameManager.Instance.currentGameMode == GameMode.StoryMode) s = GameManager.Instance.PlayerStats;
        else s = GameManager.Instance.PlayerNotStoryStats;
        Power = s.GetPower(IsPlayer);
    }

    /// <summary>
    /// Changes state back to idle, used by <see cref="Animator"/>
    /// </summary>
    public void ReturnToIdle() => AnimationManager.ChangeAnimationState(IsPlayer, "Idle");
}