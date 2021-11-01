using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;


#region Enums
public enum GameStates
{
    Initialize,
    Menu,
    Fight,
    Results,
    Rest,
    History
}

public enum GameMode
{
    StoryMode,
    RematchMode,
    ArcadeMode
}
#endregion

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public static Action<GameStates, GameStates> OnStateMove;
    public static Action<bool> OnPause;

    #region Variables
    public Stats PlayerStats;
    public Stats PlayerNotStoryStats;
    public const int MaxPlayerStat = 9;
    public int EnemyStatTotal = 0;
    public EnemyInfo enemyInfo;

    public GameMode currentGameMode;
    public int enemyRematchLevel;
    public int playerRematchLevel;

    public float PlayerHealthArcade;
    public int ArcadeFightsCompleted;
    public SaveDataGeneric<int> BestArcadeRun;

    public SaveDataGeneric<bool> PlayerHasEnteredResume;
    public SaveDataGeneric<bool> PlayerHasEnteredRematch;
    public SaveDataGeneric<bool> PlayerHasEnteredHistory;

    public SaveDataGeneric <List<EnemyInfo.AnimalType>> TypesDiscovered;
    public SaveDataGeneric<List<EnemyInfo.AnimalType>> StoryModeAnimalList;


    public EnemyInfo.AnimalType chosenType;
    public EnemyInfo.AnimalType currentlyFightingType;
    public SaveDataGeneric<bool> HasFinishedStoryMode;

    SaveDataGeneric<int> UpgradesMade;
    SaveDataGeneric<int> JobExperience;
    SaveDataGeneric<int> AuxJobExperience;
    int lastPointsUpdate = 0;
    // public Stats EnemyStats;

    public GameStates CurrentState { get; private set; }
    //This is a list of the instance ids of subscribed things that are currently doing tasks to wait for
    public List<int> counter = new List<int>();

    [HideInInspector] public bool Win;

    //variables for devtodev
    [HideInInspector] public int BattleCount;
    [HideInInspector] public int BattlesWon;
    [HideInInspector] public int BattlesLost;

    [HideInInspector] public SaveDataGeneric<int> StoryLevel;
    public int HighestStoryLevel => ForcedRespiteLevels[ForcedRespiteLevels.Count - 1];
    [SerializeField] [Tooltip("For Display Purposes Only")] private int storyLevel;
    public List<int> ForcedRespiteLevels = new List<int>();
    [HideInInspector] public SaveDataGeneric<int> LastRespiteScene;
    #endregion

    #region Unity Functions
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        OnStateMove -= StateMove;
        OnStateMove += StateMove;

        SaveManagement.SaveManager.OnEraseSaveData -= InitSavedData;
        SaveManagement.SaveManager.OnEraseSaveData += InitSavedData;

        //Theoretically, Game Manager will only be loaded before the first frame in which the game opens
        DevAnalytics.OpenedGame();
    }
    private void OnDestroy()
    {
        OnStateMove -= StateMove;

        SaveManagement.SaveManager.OnEraseSaveData -= InitSavedData;

        //Theoretically, whenever game manager is destroyed, the session ends. OnApplicationQuit does not work with Android.
        if (StoryLevel != null) DevAnalytics.ClosedGame(BattleCount, Win, StoryLevel.Value, BattlesWon, BattlesLost);
        else DevAnalytics.ClosedGame(BattleCount, Win, 0, BattlesWon, BattlesLost);
    }
    private void Start()
    {
        InitSavedData();
        TransitionState(GameStates.Initialize);
    }
    private void LateUpdate() => CheckState();

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) Screenshotter.TakeScreenshot();
    }
#endif
    #endregion

    #region Job Experience
    public int GetPoints() => JobExperience.Value;
    public int GetUpgradesMade() => UpgradesMade.Value;
    public EnemyInfo.Animal GetAnimalFromType(EnemyInfo.AnimalType type) => Array.Find(enemyInfo.Animals, a => a.Type == type);
    public int AmountToUpgrade() => 50;
    public bool CanUpgrade() => AmountToUpgrade() <= GetPoints();
    public void ResetUpgradesMade() => UpgradesMade.Value = 0;
    public void ResetJobExperience()
    {
        JobExperience.Value = 0;
        AuxJobExperience.Value = 0;
    }
    public void TransferJobExperience()
    {
        JobExperience.Value += AuxJobExperience.Value;
        AuxJobExperience.Value = 0;
    }
    public void UpdateUpgradesMade(Text text = null)
    {
        UpgradesMade.Value++;
        UpdatePoints(-AmountToUpgrade(), text);
    }
    public bool UpdatePoints(int amount, Text text = null)
    {
        if (amount > 0) lastPointsUpdate = amount;

        if (CurrentState == GameStates.Rest)
        {
            if (JobExperience.Value + amount >= 0)
            {
                JobExperience.Value += amount;
                if (text != null) text.text = JobExperience.Value.ToString();
                return true;
            }
        }
        else if (CurrentState == GameStates.Results)
        {
            if (AuxJobExperience.Value + amount > 0)
            {
                AuxJobExperience.Value += amount;
                if (text != null) text.text = AuxJobExperience.Value.ToString();
                return true;
            }
        }

        return false;
    }
    #endregion

    public void BackToMenu()
    {
        Instance.TransitionState(GameStates.Menu);
    }
    void InitSavedData()
    {
        StoryLevel = new SaveDataGeneric<int>(nameof(StoryLevel), 1);
        storyLevel = StoryLevel.Value;
        JobExperience = new SaveDataGeneric<int>(nameof(JobExperience), 0);
        AuxJobExperience = new SaveDataGeneric<int>(nameof(AuxJobExperience), 0);
        UpgradesMade = new SaveDataGeneric<int>(nameof(UpgradesMade), 0);
        TypesDiscovered = new SaveDataGeneric<List<EnemyInfo.AnimalType>>(nameof(TypesDiscovered), new List<EnemyInfo.AnimalType>());
        LastRespiteScene = new SaveDataGeneric<int>(nameof(LastRespiteScene), 0);
        HasFinishedStoryMode = new SaveDataGeneric<bool> (nameof( HasFinishedStoryMode), false);
        BestArcadeRun = new SaveDataGeneric<int>(nameof(BestArcadeRun), 0);
        PlayerHasEnteredResume = new SaveDataGeneric<bool>(nameof(PlayerHasEnteredResume), false);
        PlayerHasEnteredRematch = new SaveDataGeneric<bool>(nameof(PlayerHasEnteredRematch), false);
        PlayerHasEnteredHistory = new SaveDataGeneric<bool>(nameof(PlayerHasEnteredHistory), false);


        PlayerStats?.InitSavedData();
        PlayerNotStoryStats?.InitSavedData();

        ResetAnimalPool();
    }

    public bool IsRespiteLevel()
    {
        foreach (int l in ForcedRespiteLevels)
        {
            if (l == StoryLevel.Value) return true;
        }
        return false;
    }

    public void StartArcade()
    {
        currentGameMode = GameMode.ArcadeMode;
        ArcadeFightsCompleted = 0;

        PlayerNotStoryStats.SetTotalStats(ArcadeFightsCompleted);
        PlayerHealthArcade = PlayerNotStoryStats.GetMaxHealth(true);
    }
    public void StartRematch(int enemyLevel, int playerLevel, EnemyInfo.AnimalType type)
    {
        PlayerHasEnteredRematch.Value = true;
        currentGameMode = GameMode.RematchMode;
        if (type == EnemyInfo.AnimalType.PunchingBag) enemyRematchLevel = 1;
        else enemyRematchLevel = enemyLevel;

        PlayerNotStoryStats.SetTotalStats(playerLevel+1);
        Console.Log("Player Not Story Stats " + PlayerNotStoryStats.GetPower(true));
        //playerRematchLevel = playerLevel;
        currentlyFightingType = type;
        TransitionState(GameStates.Fight);
    }
    public void RevertToRespite() //not used currently
    {
        for (int i = ForcedRespiteLevels.Count - 1; i >= 0; i--)
        {
            if (ForcedRespiteLevels[i] <= StoryLevel.Value)
            {
                StoryLevel.Value = ForcedRespiteLevels[i];
                return;
            }
        }
        StoryLevel.Value = 1;
    }

    void FillAnimalTier(int number, List<EnemyInfo.AnimalType> tierList)
    {
        //We get all undiscovered animals that are in the desired tier
        List<EnemyInfo.AnimalType> UndiscoveredInTier = tierList.Except(TypesDiscovered.Value).ToList();
        List<EnemyInfo.AnimalType> DiscoveredInTier = tierList.Except(UndiscoveredInTier).ToList();

        //Console.Log("Undiscovered in this tier: " + UndiscoveredInTier.Count);
        //We add all undiscovered animals if we need more animals than the amount of undiscovered animals, if we want less animals than undiscovered animals in the tier, we just take as many as we need
        int quantityToAdd = number > UndiscoveredInTier.Count ? UndiscoveredInTier.Count : number;
        for (int i = 0; i < quantityToAdd; i++)
        {
            EnemyInfo.AnimalType aux = UndiscoveredInTier[UnityEngine.Random.Range(0, UndiscoveredInTier.Count)];
            //Console.Log("adding " + aux);
            UndiscoveredInTier.Remove(aux);
            StoryModeAnimalList.Value.Add(aux);
            number--;
        }

        //If we have exhausted all undiscovered animals and we still need more to fill the tier, we get them from the discovered list
        if (number > 0) for (int i = 0; i < number; i++) StoryModeAnimalList.Value.Add(DiscoveredInTier[UnityEngine.Random.Range(0, DiscoveredInTier.Count)]);
    }

    public void ResetAnimalPool()
    {
        StoryModeAnimalList = new SaveDataGeneric<List<EnemyInfo.AnimalType>>(nameof(StoryModeAnimalList), new List<EnemyInfo.AnimalType>());

        //We add the first enemy, punching bag
        StoryModeAnimalList.Value.Add(EnemyInfo.AnimalType.PunchingBag);

        //We add the three tiers with randomized enemies, saving the beaver for level 4
        FillAnimalTier(2, new List<EnemyInfo.AnimalType>(new EnemyInfo.AnimalType[] { EnemyInfo.AnimalType.Badger, EnemyInfo.AnimalType.Pig, EnemyInfo.AnimalType.Hippo, EnemyInfo.AnimalType.Sloth, EnemyInfo.AnimalType.Walrus, EnemyInfo.AnimalType.Hawk }));
        FillAnimalTier(1, new List<EnemyInfo.AnimalType>(new EnemyInfo.AnimalType[] { EnemyInfo.AnimalType.Beaver}));
        // FillAnimalTier(3, new List<EnemyInfo.AnimalType>(new EnemyInfo.AnimalType[] { EnemyInfo.AnimalType.Badger, EnemyInfo.AnimalType.Pig, EnemyInfo.AnimalType.Hippo, EnemyInfo.AnimalType.Beaver, EnemyInfo.AnimalType.Sloth, EnemyInfo.AnimalType.Walrus, EnemyInfo.AnimalType.Hawk }));
        FillAnimalTier(3, new List<EnemyInfo.AnimalType>(new EnemyInfo.AnimalType[] { EnemyInfo.AnimalType.Orca, EnemyInfo.AnimalType.Wolf, EnemyInfo.AnimalType.Ox, EnemyInfo.AnimalType.Snake, EnemyInfo.AnimalType.Cat}));
        FillAnimalTier(3, new List<EnemyInfo.AnimalType>(new EnemyInfo.AnimalType[] { EnemyInfo.AnimalType.Bunny, EnemyInfo.AnimalType.Panda, EnemyInfo.AnimalType.ChadRaccoon }));

        //We add the final tier, CEOtter
        StoryModeAnimalList.Value.Add(EnemyInfo.AnimalType.CEO);
    }
    
    /// <summary>
    /// Called in late update to check changes in <see cref="CurrentState"/>
    /// </summary>
    void CheckState()
    {
        if (counter.Count == 0)
        {
            counter.Add(GetInstanceID());

            switch (CurrentState)
            {
                case GameStates.Initialize:
                    TransitionState(GameStates.Menu);
                    AudioManager.SharedInstance.PlaySong(AudioManager.Clips.MainMenuBackground);
                    break;

                case GameStates.Menu:
                    AudioManager.SharedInstance.PlaySong(AudioManager.Clips.MainMenuBackground);
                    SceneManager.LoadScene(1);
                    break;

                case GameStates.Fight:
                    if (currentGameMode == GameMode.StoryMode)
                    {
                        if (StoryLevel.Value <= 1) AudioManager.SharedInstance.PlaySong(AudioManager.Clips.MainMenuBackground);
                        else if (StoryLevel.Value > 1 && StoryLevel.Value < 5) AudioManager.SharedInstance.PlaySong(AudioManager.Clips.FightBackground);
                        else if (StoryLevel.Value >= 5 && StoryLevel.Value < 8) AudioManager.SharedInstance.PlaySong(AudioManager.Clips.FightBackground2);
                        else if (StoryLevel.Value >= 8 && StoryLevel.Value < 11) AudioManager.SharedInstance.PlaySong(AudioManager.Clips.FightBackground3);
                        else if (StoryLevel.Value >= 11) AudioManager.SharedInstance.PlaySong(AudioManager.Clips.FightBackground4);
                    }
                    else
                    {
                        switch (UnityEngine.Random.Range(0, 3))
                        {
                            case 0:
                                AudioManager.SharedInstance.PlaySong(AudioManager.Clips.FightBackground);
                                break;
                            case 1:
                                AudioManager.SharedInstance.PlaySong(AudioManager.Clips.FightBackground2);
                                break;
                            case 2:
                                AudioManager.SharedInstance.PlaySong(AudioManager.Clips.FightBackground3);
                                break;
                        }
                    }

                    int level = 0;
                    switch (currentGameMode)
                    {
                        case GameMode.StoryMode:
                            level = 3 * (StoryLevel.Value - 1);
                            break;
                        case GameMode.RematchMode:
                            level = 3 * enemyRematchLevel;
                            break;
                        case GameMode.ArcadeMode:
                            level = 4 * ArcadeFightsCompleted;
                            break;
                    }
                    foreach (EnemyInfo.Animal animal in enemyInfo.Animals) animal.enemyStatsType.SetTotalStats(EnemyStatTotal + level);
                    SceneManager.LoadScene(2);
                    break;

                case GameStates.Results:
                    AudioManager.SharedInstance.PlaySong(Win ? AudioManager.Clips.WinBackground : AudioManager.Clips.LoseBackground);
                    PerformanceReport.SendPerformanceReport();
                    PerformanceReport.Instance.UpdatePerformancePointsText(lastPointsUpdate);
                    CollectAndSendFightInfoToDevtoDev();
                    break;

                case GameStates.Rest:
                    AudioManager.SharedInstance.PlaySong(AudioManager.Clips.RestBackground);
                    SceneManager.LoadScene(3);
                    break;

                case GameStates.History:
                    AudioManager.SharedInstance.PlaySong(AudioManager.Clips.MainMenuBackground);
                    break;
            }
        }
    }
    public void TransitionState(GameStates state)
    {
        OnStateMove?.Invoke(CurrentState, state);
        CurrentState = state;
    }
    void StateMove(GameStates current, GameStates next) => counter.Remove(GetInstanceID());

    void CollectAndSendFightInfoToDevtoDev()
    {
        int level = StoryLevel.Value - 1;
        bool won = Win;
        int combatLength = Mathf.RoundToInt(Time.time - LevelManager.Instance.startTime);
        int punchesHit = PerformanceReport.Instance.BodyDoneGood + PerformanceReport.Instance.JabDoneGood;
        int punchesWhiffed = PerformanceReport.Instance.BodyDoneDone + PerformanceReport.Instance.JabDoneDone - punchesHit;
        float punchFavorability = MathFunctions.RoundFloat(MathFunctions.Ratio(PerformanceReport.Instance.JabDoneDone, PerformanceReport.Instance.BodyDoneDone), 2);
        int hitsTaken = PerformanceReport.Instance.BodyBlockDone + PerformanceReport.Instance.JabBlockDone;
        int hitsBlocked = PerformanceReport.Instance.BodyBlockGood + PerformanceReport.Instance.JabBlockGood - hitsTaken;
        int highestCombo = PunchEvaluator.Instance.MaxCombo;
        char stamp = PerformanceReport.Instance.Stamp;

        DevAnalytics.CombatFinished(level, won, combatLength, punchesHit, punchesWhiffed, punchFavorability, hitsTaken, hitsBlocked, highestCombo, stamp);
    }

    public void Pause(bool paused) => OnPause?.Invoke(paused);
}