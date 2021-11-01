using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;


/// <summary>
/// Controls AI state logic
/// </summary>
[RequireComponent(typeof(Punch), typeof(Block), typeof(Stamina))]
public class OpponentAI : MonoBehaviour
{
    #region Setting Variables
    public static OpponentAI Instance;

    //States that the PunchIndicator can be in
    [System.Serializable]
    public enum State { Random = -1, Idle, BlockHigh, BlockLow, PunchPrepHigh, PunchHigh, PunchPrepLow, PunchLow }

    //List of combos he can do
    public List<EnemyCombo> HighCombos = new List<EnemyCombo>();
    public List<EnemyCombo> LowCombos = new List<EnemyCombo>();
    public List<EnemyCombo> HighCombosLowHealth = new List<EnemyCombo>();
    public List<EnemyCombo> LowCombosLowHealth = new List<EnemyCombo>();
    public EnemyCombo FrenzyCombo;

    //0-don't go 1-go high 2-go low
    public int goToBlocking = 0;

    //list of possible states and their respective info
    public List<StateInfo> States = new List<StateInfo>();

    //the currest state that the OpponentAI is in
    public State currentState;

    //references
    AudioSource audioSource;

    //AI only stats
    int attacksMade = 0;
    readonly float aggressiveness = 0.7f;
    int maxConsecutiveAttacks;
    bool isBlocking = false;
    public bool onFrenzyPlayer = false;
    public bool onFrenzyEnemy = false;
    public bool executingCombo;
    bool canCombo = true;
    EnemyCombo currentCombo;
    public bool fightEnd = false;
    bool playerExhausted = false;
    [HideInInspector] public bool exhausted = false;
    public bool WaitingPlayerAttackHighIsRunning = false;
    public bool WaitingPlayerAttackLowIsRunning = false;
    private bool ResetHitsLandedIsRunning = false;
    int hitsLanded = 0;
    IEnumerator hitCounter;

    //level progression
    public bool canPunchHigh;
    public bool canPunchLow;
    private const MechanicManager.Mechanics upPunch = MechanicManager.Mechanics.High_Punch;
    private const MechanicManager.Mechanics lowPunch = MechanicManager.Mechanics.Low_Punch;
    private const MechanicManager.Mechanics upBlock = MechanicManager.Mechanics.High_Block;
    private const MechanicManager.Mechanics lowBlock = MechanicManager.Mechanics.Low_Block;

    //actions
    public static System.Action<State> OnStateChange;
    public static System.Action OnComboInterrupt;
    public static System.Action<float> OnEnemyPunch;
    public static System.Action OnStartFight;


    //First = isPlayer, second = blockState, third = position, it is listened by Block 
    public static System.Action<bool, bool, bool> OnAIBlock;

    //First = isPlayer, second = punchPosition, it is listened by Punch
    public static System.Action<bool, bool> OnAIPunch;

    Health bossHealth;
    Stamina bossStamina;
    Health playerHealth;
    Stamina playerStamina;

    AIBlocking aIBlocking;

    //0:not blocking   1:blocking high  2:blocking low
    private int playerBlockState;

    //Evaluation variables
    float bossHealthPercentage;
    float playerHealthPercentage;
    float bossStaminaValue;
    float bossStaminaPercentage;
    float playerStaminaPercentage;

    //particle point
    Transform upperParticlePoint, lowerParticlePoint;
    #endregion

    #region Unity Functions
    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            Console.LogWarning("GameManager uninitialized, ignore if moved to initialize scene.");
            return;
        }
        StateMove(State.Idle);
        OnStartFight?.Invoke();

        aIBlocking = GetComponent<AIBlocking>();
    }
    void Start()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Console.LogWarning("GameManager uninitialized, ignore if moved to initialize scene.");
            return;
        }
        //Console.Log(gameManager.ArcadeFightsCompleted + " arcade fights completed");
        //Console.Log(gameManager.BestArcadeRun + " best run");

        //Default: all mechanics unlocked
        MechanicManager.Mechanics[] ms = MechanicManager.Instance.Levels[7].EnemyMechanics;
        if (GameManager.Instance.currentGameMode == GameMode.StoryMode) ms = MechanicManager.Instance.Levels[GameManager.Instance.StoryLevel.Value - 1].EnemyMechanics;

        canPunchHigh = System.Array.Exists(ms, x => x == upPunch);
        aIBlocking.canBlockHigh = System.Array.Exists(ms, x => x == upBlock);

        canPunchLow = System.Array.Exists(ms, x => x == lowPunch);
        aIBlocking.canBlockLow = System.Array.Exists(ms, x => x == lowBlock);

        WaitingPlayerAttackHighIsRunning = false;
        WaitingPlayerAttackLowIsRunning = false;

        audioSource = Camera.main.GetComponent<AudioSource>();
        bossStamina = GetComponent<Stamina>();
        bossHealth = GetComponent<Health>();
        maxConsecutiveAttacks = Mathf.RoundToInt(5 * aggressiveness) + 1;
        upperParticlePoint = GameObject.Find("UpperParticlePoint").transform;
        lowerParticlePoint = GameObject.Find("LowerParticlePoint").transform;
        Debug.Log(maxConsecutiveAttacks + "max consecuive");
    }

    private void Awake()
    {
        Frenzy.OnFrenzy -= SetPlayerFrenzy;
        Frenzy.OnFrenzy += SetPlayerFrenzy;

        PlayerInput.OnInputBlock -= ChangePlayerBlockState;
        PlayerInput.OnInputBlock += ChangePlayerBlockState;

        GameManager.OnStateMove -= EndBattle;
        GameManager.OnStateMove += EndBattle;

        AIFrenzy.OnAIFrenzy -= SetEnemyFrenzy;
        AIFrenzy.OnAIFrenzy += SetEnemyFrenzy;

        GameManager.OnPause -= Paused;
        GameManager.OnPause += Paused;

        Stamina.OnExhaustion -= SetPlayerExhausted;
        Stamina.OnExhaustion += SetPlayerExhausted;

        PlayerInput.OnRecoverFromExhaustion -= SetPlayerNotExhausted;
        PlayerInput.OnRecoverFromExhaustion += SetPlayerNotExhausted;

        Block.OnHit -= ReceivePlayerHit;
        Block.OnHit += ReceivePlayerHit;


        Instance = this;
        //disabled on start to be enabled by something else to start the actual combat
        CancelInvoke();
        enabled = false;

        playerHealth = GameObject.FindGameObjectWithTag("PlayerTag").GetComponent<Health>();
        playerStamina = GameObject.FindGameObjectWithTag("PlayerTag").GetComponent<Stamina>();
    }

    void OnDestroy()
    {
        Frenzy.OnFrenzy -= SetPlayerFrenzy;
        PlayerInput.OnInputBlock -= ChangePlayerBlockState;
        GameManager.OnStateMove -= EndBattle;
        AIFrenzy.OnAIFrenzy -= SetEnemyFrenzy;
        GameManager.OnPause -= Paused;
        Stamina.OnExhaustion -= SetPlayerExhausted;
        PlayerInput.OnRecoverFromExhaustion -= SetPlayerNotExhausted;
        Block.OnHit -= ReceivePlayerHit;

    }
    #endregion

    #region Pause
    void Paused(bool paused)
    {
        if (paused)
        {
            CancelInvoke();
            return;
        }

        StateInfo info = States.Find(x => x.state == currentState);
        Invoke(nameof(StateChoose), info.duration);
    }
    #endregion

    #region Frenzy
    private void SetEnemyFrenzy(bool frenzy, bool isPlayer)
    {
        if (frenzy)
        {
            onFrenzyEnemy = true;
            executingCombo = true;
            currentCombo.currentState = 1;
            currentCombo = FrenzyCombo;
            CancelInvoke();
            StateMove(currentCombo.states[0]);
        }
        else
        {
            CancelInvoke();
            onFrenzyEnemy = false;
            executingCombo = false;
            StateMove(State.Idle);
        }
    }
    void SetPlayerFrenzy(bool activated, bool isPlayer) => onFrenzyPlayer = activated;
    #endregion

    #region Exhaustion
    void SetPlayerExhausted(bool isPlayer)
    {
        if (fightEnd) return;
        if (exhausted) return;
        if (isPlayer)
        {
            Console.Log("advantage attack");
            goToBlocking = 0;
            playerExhausted = true;
            CancelInvoke();
            StateChoose();
        }
        else
        {
            exhausted = true;
            CancelInvoke();
            Invoke(nameof(StateChoose), 3f);
        }
    }

    void SetPlayerNotExhausted(bool isPlayer)
    {
        if (fightEnd) return;
        if (isPlayer) playerExhausted = false;
    }

    void StatePrepPunch(bool highPunch)
    {
        if (fightEnd) return;
        if (!canPunchHigh)
        {
            StateMove(State.PunchPrepLow);
            return;
        }

        isBlocking = false;
        string punchType = highPunch ? "Upper" : "Lower";
        AnimationManager.ChangeAnimationState(false, $"{punchType}_Punch");

        if (upperParticlePoint != null && lowerParticlePoint != null)
        {
            GameObject particle;
            particle = ParticleManager.SharedInstance.GetPooledParticle($"{punchType}_Attack_Cue(Clone)", "Particle");
            if (particle != null)
            {
                particle.transform.parent = highPunch ? upperParticlePoint : lowerParticlePoint;
                particle.transform.localPosition = Vector3.zero;
                particle.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                particle.SetActive(true);
            }
        }
        else Console.LogError($"Either {nameof(upperParticlePoint)} or {nameof(lowerParticlePoint)} is null. Particle not spawned.");

        AnimationManager.ChangeAnimationBool(false, "Blocking Low", false);
        AnimationManager.ChangeAnimationBool(false, "Blocking High", false);
        OnAIBlock?.Invoke(false, false, false);
        OnAIPunch?.Invoke(false, highPunch);
    }
    #endregion

    #region Combat
    //Registers whether enemy punches land and where
    private void ExecuteCombo(bool high)
    {
        if (fightEnd) return;
        executingCombo = true;
        currentCombo.currentState = 1;

        GameManager gameManager = GameManager.Instance;

        int level = 0;
        switch (gameManager.currentGameMode)
        {
            case GameMode.StoryMode:
                level = gameManager.StoryLevel.Value;
                break;
            case GameMode.RematchMode:
                level = gameManager.enemyRematchLevel;
                break;
            case GameMode.ArcadeMode:
                level = 8;
                break;
        }

        bool lowHealthConditional = bossHealth.GetHealth() / bossHealth.GetMaxHealth() < 0.5f && level > 3;

        List<EnemyCombo> temp = high ? (lowHealthConditional ? HighCombosLowHealth : HighCombos) : (lowHealthConditional ? LowCombosLowHealth : LowCombos);

        playerStaminaPercentage = playerStamina.AvailableStamina / playerStamina.MaxStamina;

        int aux;
        if (playerExhausted) aux = 2;
        else aux = Random.Range(0, temp.Count);

        currentCombo = temp[aux];
        StateMove(temp[aux].states[0]);
    }

    //Registers when player receives damage from AI punches

    public void ReceivePlayerHit(bool player, float damage, bool location, bool success)
    {
        //if the NPC lands a hit the counter is increased, if the player lands a hit the counter resets
        if (success)
        {
            if (player) hitsLanded++;
            else hitsLanded = 0;

         //   hitsLanded = player ? hitsLanded++ : 0; 
        }
    }

    public void StateMove(State state)
    {
        if (fightEnd) return;
        StateInfo info = States.Find(x => x.state == state);
        currentState = state;
        OnStateChange?.Invoke(currentState);

        switch (currentState)
        {
            case State.Idle:
                StateIdle();
                break;
            case State.BlockHigh:
                StateBlock(true);
                break;
            case State.BlockLow:
                StateBlock(false);
                break;
            case State.PunchPrepHigh:
                StatePrepPunch(true);
                break;
            case State.PunchPrepLow:
                StatePrepPunch(false);
                break;
            case State.PunchHigh:
                attacksMade++;
                break;
            case State.PunchLow:
                attacksMade++;
                break;
        }

        Invoke(nameof(StateChoose), info.duration);
    }
    void StateBlock(bool highBlock)
    {
        isBlocking = true;
        attacksMade = 0;
        AnimationManager.ChangeAnimationState(false, $"{(highBlock ? "Upper" : "Lower")}_Block");
        AnimationManager.ChangeAnimationBool(false, $"Blocking {(highBlock ? "High" : "Low")}", true);
        OnAIBlock?.Invoke(false, true, highBlock);
    }
    void StateIdle()
    {
        isBlocking = false;
        attacksMade = 0;
        AnimationManager.ChangeAnimationState(false, "Idle");
        OnAIBlock?.Invoke(false, false, false);
    }
    #endregion

    #region StateEvaluation

    //It detects where the player is blocking and stores it
    private void ChangePlayerBlockState(bool isPlayer, bool blockingState, bool blockPosition)
    {
        if (isPlayer)
        {
            if (blockingState) playerBlockState = blockPosition ? 1 : 2;
            else playerBlockState = 0;
        }
    }
    private float BasePunchEvaluationAmount() => bossHealthPercentage + (1 - playerHealthPercentage) + bossStaminaPercentage + (2 * aggressiveness) + (1 - Mathf.Max(attacksMade / 2.5f, 2));

    //Evaluate suitability of a state base on the combat status
    private float ProcessEvaluation(State auxState)
    {
        float randomAttack = 0;
        //We add randomness to the decision between high and low attack, in case the player is not blocking
        if (playerBlockState == 0) randomAttack = Random.Range(0f, 1f);

        //Boss will remain idle based on stamina, aggressiveness, player's stamina, etc.
        if (auxState == State.Idle)
            return (1 - aggressiveness) + bossHealthPercentage + (1 - playerStaminaPercentage) + (1 - bossStaminaPercentage) * 3 + Mathf.Max(attacksMade / 5, 1) * 2;
        if (attacksMade < maxConsecutiveAttacks && canCombo)
        {
            //We decide whether to attack high or low, mostly depending on where the player is block and if the mechanic is enabled
            if (auxState == State.PunchPrepLow && LowCombos.Count > 0 && canPunchLow)
                return BasePunchEvaluationAmount() + (playerBlockState != 2 ? 3 : 0) + randomAttack;
            else if (auxState == State.PunchPrepHigh && HighCombos.Count > 0 && canPunchHigh)
                return BasePunchEvaluationAmount() + (playerBlockState != 1 ? 3 : 0) + (1 - randomAttack);
        }
        return -1;
    }
    public void StateChoose()
    {
        //If the fight has ended, no need for further enemy states
        if (fightEnd) return;

        exhausted = false;

        //No need for state evaluation in combos, we just want to either interrupt combo to block or go to the next combo state
        if (executingCombo)
        {
            if (onFrenzyEnemy)
            {
                CancelInvoke();
                StateMove(currentCombo.states[currentCombo.currentState]);

                //In frenzy the combo is recurrent, so we check if we have to restart it
                if (currentCombo.currentState == FrenzyCombo.states.Count - 1) currentCombo.currentState = 0;
                else currentCombo.currentState++;
                return;
            }
            //we check if AIBlocking script has marked the need to go to block
            if (goToBlocking != 0)
            {
                //If so, we block to whichever position is marked as necesary
                CancelInvoke();
                StateMove(goToBlocking == 1 ? State.BlockHigh : State.BlockLow);
                return;
            }

            //If we don't have to block, we move on with the next combo state and check if we have reached the end of the combo
            else
            {
                if (currentCombo.states.Count > currentCombo.currentState)
                {
                    CancelInvoke();
                    StateMove(currentCombo.states[currentCombo.currentState]);
                    currentCombo.currentState++;
                    return;
                }
                executingCombo = false;
            }
        }

        StateInfo info = States.Find(x => x.state == currentState);
        if (info.nextStates.Length == 1)
        {
            StateMove(info.nextStates[0]);
            return;
        }

        if (onFrenzyEnemy)
        {
            if (Random.Range(0f, 1f) > 0f) StateMove(State.PunchPrepLow);
            else StateMove(State.PunchPrepHigh);
            return;
        }

        //Get all fight's current info
        bossHealthPercentage = bossHealth.GetHealth() / bossHealth.GetMaxHealth();
        playerHealthPercentage = playerHealth.GetHealth() / playerHealth.GetMaxHealth();
        bossStaminaValue = bossStamina.AvailableStamina;
        bossStaminaPercentage = bossStaminaValue / bossStamina.MaxStamina;
        playerStaminaPercentage = playerStamina.AvailableStamina / playerStamina.MaxStamina;

        List<AIState> evaluatedStates = new List<AIState>();

        //For each possbile next state, evaluate its suitability
        foreach (State auxState in info.nextStates) evaluatedStates.Add(new AIState { state = auxState, eval = ProcessEvaluation(auxState) });

        //Sort all possible states based on evaluation value
        evaluatedStates = evaluatedStates.OrderByDescending(x => x.eval).ToList();

        if (evaluatedStates[0].state == State.PunchPrepHigh) ExecuteCombo(true);
        else if (evaluatedStates[0].state == State.PunchPrepLow) ExecuteCombo(false);
        else StateMove(evaluatedStates[0].state);
    }
    #endregion

    /// <summary>
    /// Called when <see cref="GameManager"/> transistions out of <see cref="GameStates.Fight"/>
    /// </summary>
    void EndBattle(GameStates current, GameStates next)
    {
        if (current == GameStates.Fight)
        {
            CancelInvoke();
            enabled = false;
        }
    }



    #region Structs
    /// <summary>
    /// Holds all the State specific information that can be referenced in punch indicator
    /// </summary>
    [System.Serializable]
    public struct AIState
    {
        public float eval;
        public State state;
    }

    [System.Serializable]
    public struct EnemyCombo
    {
        public List<State> states;
        public int currentState;
    }

    [System.Serializable]
    public struct StateInfo
    {
        public State state;
        public State[] nextStates;
        public float duration;
    }
    #endregion
}