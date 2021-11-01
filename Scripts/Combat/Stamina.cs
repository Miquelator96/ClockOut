using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stamina : MonoBehaviour
{
    public static System.Action<bool> OnExhaustion;

    static List<Stamina> Instances = new List<Stamina>();
    public bool IsPlayer;

    #region Variables
    Slider staminaSlider;
    BarFlashOnLow barFlashOnLow;
    //Stamina
    public float AvailableStamina { get; private set; }
    public float MaxStamina { get; private set; }

    //multipliers
    const float UpperMultiplier = 1.2f;
    const float LowerMultiplier = 0.83f;

    //For recovery
    float StaminaRecoveryRate => .5f * MaxStamina;
    float StaminaRecoveryDelay = 2.75f;

    //Checks for punchability
    const float StaminaPerPunch = 15.65f;
    float defense;
    public bool CheckStaminaAmount => AvailableStamina > StaminaPerPunch;
    bool recovering = false;

    //For frenzy
    bool inFrenzy = false;

    //For pause
    bool paused;
    #endregion

    #region Unity Functions
    private void Awake()
    {
        Block.OnHit -= UseStamina;
        Block.OnHit += UseStamina;
        Punch.OnThrowPunch -= StopRecovery;
        Punch.OnThrowPunch += StopRecovery;
        Frenzy.OnFrenzy -= OnFrenzy;
        Frenzy.OnFrenzy += OnFrenzy;
        AIFrenzy.OnAIFrenzy -= OnFrenzy;
        AIFrenzy.OnAIFrenzy += OnFrenzy;
        GameManager.OnPause -= Paused;
        GameManager.OnPause += Paused;

        Instances.Add(this);
    }
    private void OnDestroy()
    {
        Block.OnHit -= UseStamina;
        Punch.OnThrowPunch -= StopRecovery;
        AIFrenzy.OnAIFrenzy -= OnFrenzy;
        Frenzy.OnFrenzy -= OnFrenzy;
        GameManager.OnPause -= Paused;

        Instances.Remove(this);
    }
    private void Start()
    {
        //Assign the right stamina slider depending on IsPlayer
        staminaSlider = IsPlayer ? GameObject.FindGameObjectWithTag("PlayerStaminaBar").GetComponent<Slider>() : GameObject.FindGameObjectWithTag("BossStaminaBar").GetComponent<Slider>();

        if (GameManager.Instance.currentGameMode == GameMode.StoryMode) MaxStamina = IsPlayer ? GameManager.Instance.PlayerStats.GetMaxStamina(IsPlayer) : EnemyCreator.Animal.enemyStatsType.GetMaxStamina(IsPlayer);
        else MaxStamina = IsPlayer ? GameManager.Instance.PlayerNotStoryStats.GetMaxStamina(IsPlayer) : EnemyCreator.Animal.enemyStatsType.GetMaxStamina(IsPlayer);

        if (!IsPlayer && GameManager.Instance.currentlyFightingType == EnemyInfo.AnimalType.CEO) MaxStamina *= 2.25f;
        if (staminaSlider == null) Console.LogError($"{staminaSlider} {nameof(staminaSlider)}\nmay cause problems");

        defense = IsPlayer ? (GameManager.Instance.currentGameMode != GameMode.StoryMode ? GameManager.Instance.PlayerNotStoryStats.GetDefense(true) : GameManager.Instance.PlayerStats.GetDefense(IsPlayer)) : EnemyCreator.Animal.enemyStatsType.GetDefense(IsPlayer);

        AvailableStamina = MaxStamina;
        barFlashOnLow = IsPlayer ? GameObject.Find("Player_SP_Fill").GetComponent<BarFlashOnLow>() : GameObject.Find("Boss_SP_Fill").GetComponent<BarFlashOnLow>();
        SetStaminaValue();
    }
    private void Update() => Recover(Time.deltaTime * StaminaRecoveryRate);
    #endregion

    #region Frenzy
    /// <summary>
    /// Sets stamina unlimited when in frenzy
    /// </summary>
    /// <param name="activated">if frenzy is activated or deactivated</param>
    /// <param name="isPlayer">if player or enemy frenzy</param>
    private void OnFrenzy(bool activated, bool isPlayer)
    {
        if (isPlayer == IsPlayer) inFrenzy = activated;
    }
    #endregion

    #region Pause
    /// <summary>
    /// Stop recovery when paused
    /// </summary>
    /// <param name="paused">is paused or unpaused</param>
    void Paused(bool paused)
    {
        this.paused = paused;
        if (!recovering)
        {
            recovering = false;
            CancelInvoke(nameof(ReturnRecovery));
            if (!paused) Invoke(nameof(ReturnRecovery), StaminaRecoveryDelay);
        }
    }
    #endregion

    #region Recovery
    /// <summary>
    /// Stops recovery for StaminaRecoveryDelay time
    /// </summary>
    /// <param name="isPlayer">if stamina is players or enemies</param>
    /// <param name="position">upper or lower</param>
    void StopRecovery(bool isPlayer, bool position)
    {
        if (isPlayer == IsPlayer)
        {
            recovering = false;
            CancelInvoke(nameof(ReturnRecovery));
            Invoke(nameof(ReturnRecovery), inFrenzy ? 0 : StaminaRecoveryDelay);
        }
    }
    /// <summary>
    /// enables recovery
    /// </summary>
    void ReturnRecovery() => recovering = true;
    /// <summary>
    /// recovers stamina and sets slider
    /// </summary>
    /// <param name="amount">amount to recover</param>
    void Recover(float amount)
    {
        //If true, recover
        if (recovering && !paused && AvailableStamina < MaxStamina)
        {
            AvailableStamina += amount;
            AvailableStamina = Mathf.Min(MaxStamina, AvailableStamina);
            SetStaminaValue();
        }
    }
    #endregion

    /// <summary>
    /// Handles stamina loss
    /// </summary>
    /// <param name="isPlayer">if the players or enemies stamina</param>
    /// <param name="damage">damage taken</param>
    /// <param name="position">upper or lower</param>
    void UseStamina(bool isPlayer, float damage, bool position, bool success)
    {
        float positionMultiplier = position ? UpperMultiplier : LowerMultiplier;
        if (isPlayer != IsPlayer)
        {
            float multiplier = positionMultiplier * (success ? 1 : Instances.Find(x => x != this).defense);
            float staminaUsed = (inFrenzy ? 0 : StaminaPerPunch) * multiplier;
            AvailableStamina -= staminaUsed;
            AvailableStamina = Mathf.Max(0, AvailableStamina);
            SetStaminaValue();
            CheckForExhaustion(IsPlayer);
        }
    }
    /// <summary>
    /// Checks if player is exhausted
    /// </summary>
    /// <param name="isPlayer">is players or enemies stamina</param>
    public static void CheckForExhaustion(bool isPlayer)
    {
        if (OpponentAI.Instance.fightEnd) return;
        Stamina s = Instances.Find(x => x.IsPlayer == isPlayer);
        if (s.AvailableStamina <= 0)
        {
            AnimationManager.ChangeAnimationState(isPlayer, "Exhausted");
            OnExhaustion?.Invoke(isPlayer);
        }
    }
    /// <summary>
    /// Sets slider value
    /// </summary>
    void SetStaminaValue()
    {
        staminaSlider.value = AvailableStamina / MaxStamina;
        if (barFlashOnLow.enabled)
            barFlashOnLow?.FlashBar();
    }
}