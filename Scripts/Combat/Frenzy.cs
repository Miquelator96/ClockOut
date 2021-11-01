using UnityEngine;

/// <summary>
/// Handles frenzy (currently only used for player, though made for both)
/// </summary>
public class Frenzy : MonoBehaviour
{
    public static System.Action<float, bool> OnFrenzyValueChanged;
    public static System.Action<bool, bool> OnFrenzy;

    #region Variables
    private float availableFrenzy;
    public static float maxFrenzy = 600;
    private float frenzyDepletionRate => 0.166f * maxFrenzy;
    public const float frenzyAgility = 0.133f;
    const float FrenzyPerAction = 40;
    const float ComboMultiplier = 1.1f;

    bool inFrenzyMode = false;
    bool frenzyEnabled;

    bool _paused = false;
    public bool IsPlayer;
    #endregion

    #region Unity Functions
    private void Awake()
    {
        Block.OnHit -= SuccessfulPunch;
        Block.OnHit += SuccessfulPunch;
        GameManager.OnPause -= Paused;
        GameManager.OnPause += Paused;
    }
    private void OnDestroy()
    {
        Block.OnHit -= SuccessfulPunch;
        GameManager.OnPause -= Paused;
    }
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Console.LogWarning("GameManager uninitialized, ignore if moved to initialize scene.");
            return;
        }
        if (MechanicManager.Instance == null)
        {
            Console.LogWarning("MechanicManager uninitialized, ignore if moved to initialize scene.");
            return;
        }

        //Default: all mechanics unlocked
        MechanicManager.Mechanics[] ms = MechanicManager.Instance.Levels[7].PlayerMechanics;
        if (GameManager.Instance.currentGameMode == GameMode.StoryMode) ms = MechanicManager.Instance.Levels[GameManager.Instance.StoryLevel.Value - 1].PlayerMechanics;

        frenzyEnabled = System.Array.Exists(ms, x => x == MechanicManager.Mechanics.Frenzy);
        RadialFrenzyBarFill.ChangeMaxValue(maxFrenzy);

        availableFrenzy = 0;
        SetFrenzyValue();
    }
    /// <summary>
    /// decreases frenzy value when activated
    /// </summary>
    void Update()
    {
        if(inFrenzyMode && !_paused)
        {
            availableFrenzy -= Time.deltaTime * frenzyDepletionRate;
            if(availableFrenzy < 0)
            {
                inFrenzyMode = false;
                availableFrenzy = 0;
                OnFrenzy?.Invoke(false, IsPlayer);
            }
            SetFrenzyValue();
        }
    }
    #endregion

    #region Pause
    void Paused(bool paused) => _paused = paused;
    #endregion

    #region Frenzy Inputs
    /// <summary>
    /// update frenzy based on a successful hit
    /// </summary>
    /// <param name="isPlayer">player or enemy hit</param>
    /// <param name="damage">damage done</param>
    /// <param name="position">upper or lower</param>
    void SuccessfulPunch(bool isPlayer, float damage, bool position, bool success)
    {
        if (success)
        {
            if(!inFrenzyMode) availableFrenzy += isPlayer ? -FrenzyPerAction : FrenzyPerAction * Mathf.Pow(ComboMultiplier, PunchEvaluator.Instance.rank);
            SetFrenzyValue();
        }
    }
    #endregion

    /// <summary>
    /// handles changes to frenzy amount
    /// </summary>
    void SetFrenzyValue()
    {
        if (frenzyEnabled)
        {
            availableFrenzy = Mathf.Max(availableFrenzy, 0);

            OnFrenzyValueChanged?.Invoke(availableFrenzy, IsPlayer);

            //if enough frenzy amount to activate frenzy
            if(availableFrenzy > maxFrenzy)
            {
                inFrenzyMode = true;
                availableFrenzy = maxFrenzy - 1;
                OnFrenzy?.Invoke(true, IsPlayer);
            }
        }
    }
}
