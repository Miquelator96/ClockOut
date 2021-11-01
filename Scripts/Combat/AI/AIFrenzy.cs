using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIFrenzy : MonoBehaviour
{
    // booleans: on/off frenzy, isPlayer
    public static System.Action<bool, bool> OnAIFrenzy;
    public static System.Action<float, bool> OnAIFrenzyValueChanged;

    private float availableFrenzy;
    private float maxFrenzy = 600;
    private float frenzyDepletionRate => 0.166f * maxFrenzy;
    public static float frenzyAgility = 0.133f;
    //Both StaminaPerPunch and StaminaRecoveryDelay become staminaNegationValue during a frenzy.
    public static float staminaNegationValue = 0;
    public static float FrenzyPerAction = 50;
    public static float ComboMultipler = 1.1f;

    public bool inFrenzyMode = false;
    bool frenzyEnabled;
    //int comboCounter = 0;
    bool _paused = false;

    OpponentAI opponentAI;

    #region Unity Functions
    void Start()
    {
        opponentAI = GetComponent<OpponentAI>();
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Console.LogWarning("GameManager uninitialized, ignore if moved to initialize scene.");
            return;
        }
        MechanicManager.Mechanics[] ms = MechanicManager.Instance.Levels[7].EnemyMechanics;
        if (gameManager.currentGameMode == GameMode.StoryMode) ms = MechanicManager.Instance.Levels[gameManager.StoryLevel.Value - 1].EnemyMechanics;

        frenzyEnabled = System.Array.Exists(ms, x => x == MechanicManager.Mechanics.Frenzy);


        RadialFrenzyBarFill.ChangeMaxValue(maxFrenzy);

        availableFrenzy = 0;
        // SetFrenzyValue();
    }
    private void Awake()
    {
        Block.OnAIFeedbackPunch -= UpdateFeedbackPunches;
        Block.OnAIFeedbackPunch += UpdateFeedbackPunches;
        Block.OnHit -= AISuccesfulBlock;
        Block.OnHit += AISuccesfulBlock;
        Block.OnHit -= OnSuccessfulPunch;
        Block.OnHit += OnSuccessfulPunch;
        GameManager.OnPause -= Paused;
        GameManager.OnPause += Paused;
        GameManager.OnStateMove -= EndBattle;
        GameManager.OnStateMove += EndBattle;
    }
    private void OnDestroy()
    {
        Block.OnAIFeedbackPunch -= UpdateFeedbackPunches;
        Block.OnHit -= AISuccesfulBlock;
        Block.OnHit -= OnSuccessfulPunch;
        GameManager.OnPause -= Paused;
        GameManager.OnStateMove -= EndBattle;
    }
    #endregion

    void Paused(bool paused) => _paused = paused;

    //Function executed when the enemy successfully blocks
    private void AISuccesfulBlock(bool isPlayer, float s, bool position, bool success)
    {
        if (!isPlayer && frenzyEnabled && !success)
        {
            //comboCounter = comboCounter + 1;

            //Increase frenzy meter unless the enemy is in frenzy mode
            if (!inFrenzyMode) availableFrenzy += FrenzyPerAction;// + comboCounter;

            //If max frenzy is reach, activate frenzy mode
            if (availableFrenzy >= maxFrenzy && !inFrenzyMode) ActivateFrenzyMode();

            //Update frenzy meter
            OnAIFrenzyValueChanged?.Invoke(availableFrenzy, false);
        }
    }
    private void OnSuccessfulPunch(bool isPlayer, float damage, bool position, bool success)
    {
        if (success)
        {
            if(isPlayer == false) availableFrenzy -=  FrenzyPerAction;
            OnAIFrenzyValueChanged?.Invoke(availableFrenzy, false);
        }
    }

    void ActivateFrenzyMode()
    {
        if (frenzyEnabled && !opponentAI.exhausted)
        {
            inFrenzyMode = true;
            GetComponent<AnimationManager>().SetFrenzy(true);
            //call frenzy to change animation controlers
            OnAIFrenzy?.Invoke(true, false);
        }
    }

    void Update()
    {
        if (inFrenzyMode && !_paused && frenzyEnabled && !opponentAI.exhausted)
        {
            //Decrease frenzy meter over time if we are in frenzy mode
            availableFrenzy -= Time.deltaTime * frenzyDepletionRate;

            //check if we arrived at minimum, in which case frenzy stops
            if (availableFrenzy < 0)
            {
                inFrenzyMode = false;
                availableFrenzy = 0;
                OnAIFrenzy?.Invoke(false, false);
                GetComponent<AnimationManager>().SetFrenzy(false);
            }
            OnAIFrenzyValueChanged?.Invoke(availableFrenzy, false);
        }
        if(availableFrenzy < 0)
        {
            inFrenzyMode = false;
            availableFrenzy = 0;
        }
    }
    //Gets called when the enemy throws a punch, showing whether player has blocked it
    private void UpdateFeedbackPunches(bool position, bool result)
    {
        if (frenzyEnabled)
        {
            //if (result) comboCounter = comboCounter + 1;
            //else comboCounter = 0;

            //Increase frenzy meter if the punch has landed
            if (result && !inFrenzyMode) availableFrenzy += FrenzyPerAction;// + comboCounter;

            //If max frenzy is reached, activate frenzy mode
            if (availableFrenzy >= maxFrenzy && !inFrenzyMode) ActivateFrenzyMode();

            //Update frenzy meter
            OnAIFrenzyValueChanged?.Invoke(availableFrenzy, false);
        }
    }
    void EndBattle(GameStates current, GameStates next)
    {
        if (current == GameStates.Fight)
        {
            CancelInvoke();
            enabled = false;
        }
    }
}