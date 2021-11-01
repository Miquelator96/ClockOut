using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// To be put on <see cref="Image"/> that is input for <see cref="Block"/>, handles input for either upper or lower
/// </summary>
[RequireComponent(typeof(Image))]
public class PlayerInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static System.Action<bool, bool, bool> OnInputBlock;
    public static System.Action<bool, bool> OnInputPunch;
    public static System.Action OnPunchCooldown;
    public static System.Action<bool> OnRecoverFromExhaustion;

    static List<PlayerInput> inputs = new List<PlayerInput>();

    #region Variables
    public Button[] PlayerButtons;
    public Image PunchButton;
    protected bool blocking;

    bool inFrenzy = false;
    float agility;
    public bool blockPosition;
    bool punchCooldown = false;
    static bool exhausted = false;
    bool punchEnabled = true;
    bool blockEnabled = true;
    bool punchEnabledOther = true;
    bool blockEnabledOther = true;
    const float exhaustionTime = 3.5f;

    private const MechanicManager.Mechanics upPunch = MechanicManager.Mechanics.High_Punch;
    private const MechanicManager.Mechanics lowPunch = MechanicManager.Mechanics.Low_Punch;
    private const MechanicManager.Mechanics upBlock = MechanicManager.Mechanics.High_Block;
    private const MechanicManager.Mechanics lowBlock = MechanicManager.Mechanics.Low_Block;
    #endregion

    #region Unity Functions
    private void Awake()
    {
        Stamina.OnExhaustion -= RecoverFromExhaustion;
        Stamina.OnExhaustion += RecoverFromExhaustion;
        Frenzy.OnFrenzy -= OnFrenzy;
        Frenzy.OnFrenzy += OnFrenzy;
        OnPunchCooldown -= StartCooldown;
        OnPunchCooldown += StartCooldown;
        GameManager.OnStateMove -= EndBattle;
        GameManager.OnStateMove += EndBattle;
        GameManager.OnPause -= OnPause;
        GameManager.OnPause += OnPause;

        inputs.Add(this);
    }
    private void OnDestroy()
    {
        Stamina.OnExhaustion -= RecoverFromExhaustion;
        Frenzy.OnFrenzy -= OnFrenzy;
        OnPunchCooldown -= StartCooldown;
        GameManager.OnStateMove -= EndBattle;
        GameManager.OnPause -= OnPause;

        inputs.Remove(this);
    }
    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Console.LogWarning("GameManager uninitialized, ignore if moved to initialize scene.");
            return;
        }

        if (GameManager.Instance.currentGameMode == GameMode.StoryMode) agility = GameManager.Instance.PlayerStats.GetAgility(true);
        else agility = GameManager.Instance.PlayerNotStoryStats.GetAgility(true);
        exhausted = false;

        //Default: all mechanics unlocked
        MechanicManager.Mechanics[] ms = ms = MechanicManager.Instance.Levels[7].PlayerMechanics;

        if (GameManager.Instance.currentGameMode == GameMode.StoryMode) ms = MechanicManager.Instance.Levels[GameManager.Instance.StoryLevel.Value - 1].PlayerMechanics;

        punchEnabled = System.Array.Exists(ms, x => x == (blockPosition ? upPunch : lowPunch));
        blockEnabled = System.Array.Exists(ms, x => x == (blockPosition ? upBlock : lowBlock));
        punchEnabledOther = System.Array.Exists(ms, x => x == (!blockPosition ? upPunch : lowPunch));
        blockEnabledOther = System.Array.Exists(ms, x => x == (!blockPosition ? upBlock : lowBlock));

        GetComponent<Image>().enabled = blockEnabled;
        PunchButton.enabled = punchEnabled;
    }
#if UNITY_EDITOR
    void Update() => KeyboardControlCheck();
#endif
    #endregion

    #region Cooldown
    void StartCooldown()
    {
        punchCooldown = true;
        Invoke(nameof(ResetCooldown), agility);
    }
    void ResetCooldown() => punchCooldown = false;
    #endregion

    #region Frenzy
    /// <summary>
    /// modifies agility based on frenzy state
    /// </summary>
    /// <param name="activated">frenzy is activated or deactivated</param>
    /// <param name="isPlayer">if players or enemies frenzy</param>
    private void OnFrenzy(bool activated, bool isPlayer)
    {
        if (activated)
        {
            agility = Frenzy.frenzyAgility;
            return;
        }
        agility = GameManager.Instance.currentGameMode == GameMode.StoryMode? GameManager.Instance.PlayerStats.GetAgility(true) : GameManager.Instance.PlayerNotStoryStats.GetAgility(true);
        Invoke(nameof(ResetCooldown), agility);
    }
    #endregion
    
    //Keyboard Stuff is not included in the build
    #region Keyboard Stuff
#if UNITY_EDITOR
    void KeyboardControlCheck()
    {
        if (!exhausted)
        {
            if (punchEnabled && !punchCooldown)
            {
                if (blockPosition && Input.GetKeyDown(KeyCode.E))
                {
                    OnInputPunch?.Invoke(true, true);
                    OnPunchCooldown?.Invoke();
                }

                if (!blockPosition && Input.GetKeyDown(KeyCode.D))
                {
                    OnInputPunch?.Invoke(true, false);
                    OnPunchCooldown?.Invoke();
                }
            }
            if (blockEnabled)
            {
                if (blockPosition && Input.GetKeyDown(KeyCode.Q)) OnInputBlock?.Invoke(true, true, true);
                if (!blockPosition && Input.GetKeyDown(KeyCode.A)) OnInputBlock?.Invoke(true, true, false);
                if (!blockPosition && Input.GetKeyUp(KeyCode.A))
                {
                    if (blockEnabledOther && Input.GetKey(KeyCode.Q)) OnInputBlock?.Invoke(true, true, true);
                    else OnInputBlock?.Invoke(true, false, false);
                }
                if (blockPosition && Input.GetKeyUp(KeyCode.Q))
                {
                    if (blockEnabledOther && Input.GetKey(KeyCode.A)) OnInputBlock?.Invoke(true, true, false);
                    else OnInputBlock?.Invoke(true, false, true);
                }
            }
        }
    }
#endif
    #endregion

    #region Button Stuff
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (blockEnabled && !exhausted)
        {
            blocking = true;
            OnInputBlock?.Invoke(true, true, blockPosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (blockEnabled && !exhausted)
        {
            if (inputs.Find(x => x != this).blocking) OnInputBlock?.Invoke(true, false, !blockPosition);
            else
            {
                OnInputBlock?.Invoke(true, false, blockPosition);
                blocking = false;
            }
        }
    }

    /// <summary>
    /// Called by the punch button
    /// </summary>
    public void Punch(bool position)
    {
        if (punchEnabled && !punchCooldown && !exhausted)
        {
            OnInputPunch?.Invoke(true, position);
            OnPunchCooldown?.Invoke();
        }
    }

    /// <summary>
    /// toggles the interactability of the player's buttons
    /// </summary>
    /// <param name="enabled">whether interactable or not</param>
    void ToggleButtons(bool enabled)
    {
        foreach (Button b in PlayerButtons) if (b != null) b.interactable = enabled;
    }
    #endregion

    #region Exhaustion
    /// <summary>
    /// Sets exhaustion effects
    /// </summary>
    /// <param name="isPlayer">if it is the player getting exhausted or not</param>
    void RecoverFromExhaustion(bool isPlayer)
    {
        if (isPlayer)
        {
            if (!exhausted && blockPosition)
            {
                exhausted = true;
                CancelInvoke(nameof(ResetCooldown));
                AnimationManager.ChangeAnimationBool(true, "Exhausted", true);
                ToggleButtons(false);
                Invoke(nameof(ResetExhaustion), exhaustionTime);
            }
        }
    }
    /// <summary>
    /// Removes exhaustion effects
    /// </summary>
    void ResetExhaustion()
    {
        exhausted = false;
        punchCooldown = false;
        AnimationManager.ChangeAnimationState(true, "Idle");
        AnimationManager.ChangeAnimationBool(true, "Exhausted", false);
        ToggleButtons(true);
        OnRecoverFromExhaustion?.Invoke(true);
    }
    #endregion

    #region Pause
    void OnPause(bool paused)
    {
        if (paused) CancelInvoke(nameof(ResetExhaustion));
        else Invoke(nameof(ResetExhaustion), exhaustionTime);
    }
    #endregion

    void EndBattle(GameStates current, GameStates next)
    {
        if (current == GameStates.Fight)
        {
            CancelInvoke();
            enabled = false;
        }
    }
}