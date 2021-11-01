using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Punch), typeof(Block))]
public class Health : MonoBehaviour
{
    public static System.Action OnEndFight;

    #region Variables
    //references
    public bool IsPlayer;
    Slider healthSlider;
    BarFlashOnLow barFlashOnLow;
    private float availableHealth;
    float maxHealth;
    GameManager gameManager = GameManager.Instance;
    #endregion

    #region Unity Functions
    private void Awake()
    {
        Block.OnHit -= Hit;
        Block.OnHit += Hit;
    }
    private void OnDestroy() => Block.OnHit -= Hit;
    //init
    private void Start()
    {
        if (gameManager == null)
        {
            Console.LogWarning("GameManager uninitialized, ignore if moved to initialize scene.");
            return;
        }

        //Changing player position if in story and against CEO
        if (IsPlayer)
        {
            if (gameManager.currentlyFightingType == EnemyInfo.AnimalType.CEO)
            {
                transform.localPosition = new Vector3(-2.15f, 1.15f, 0f);
                transform.localScale = new Vector3(0.4f, 0.4f, 0.25f);
            }
            else
            {
                transform.localPosition = new Vector3(-3f, 1.7f, 0f);
                transform.localScale = new Vector3(0.5f, 0.5f, 0.4f);
            }

        }

        //Assign the right health slider depending on IsPlayer
        healthSlider = IsPlayer ? GameObject.FindGameObjectWithTag("PlayerHealthBar").GetComponent<Slider>() : GameObject.FindGameObjectWithTag("BossHealthBar").GetComponent<Slider>();

        //Player always has the same max health in arcade mode
        maxHealth = IsPlayer ? (gameManager.currentGameMode == GameMode.ArcadeMode ? gameManager.PlayerNotStoryStats.GetMaxHealth(true) : gameManager.PlayerStats.GetMaxHealth(IsPlayer)) : EnemyCreator.Animal.enemyStatsType.GetMaxHealth(IsPlayer);
        if (!IsPlayer && EnemyCreator.Animal.Type == EnemyInfo.AnimalType.PunchingBag) maxHealth *= .5f;
        if (!IsPlayer && EnemyCreator.Animal.Type == EnemyInfo.AnimalType.CEO) maxHealth *= 2.25f;
        if (IsPlayer && PerformanceReport.Instance != null) PerformanceReport.Instance.PlayerMaxHealth = Mathf.RoundToInt(maxHealth);

        if (healthSlider == null) Console.LogError($"{healthSlider} {nameof(healthSlider)}\nmay cause problems");

        //If we are in arcade mode and assigning player's health, we get the value from the end of the previous arcade fight
        availableHealth = gameManager.currentGameMode == GameMode.ArcadeMode && IsPlayer ? gameManager.PlayerHealthArcade : maxHealth;
        if (IsPlayer && PerformanceReport.Instance != null) PerformanceReport.Instance.PlayerHealth = Mathf.RoundToInt(availableHealth);

        barFlashOnLow = IsPlayer ? GameObject.Find("Player_HP_Fill").GetComponent<BarFlashOnLow>() : GameObject.Find("Boss_HP_Fill").GetComponent<BarFlashOnLow>();

        SetHealthValue();
    }
    #endregion

    #region Hit
    /// <summary>
    /// Hit taken through block
    /// </summary>
    /// <param name="isPlayer">players or enemies health</param>
    /// <param name="damage">damage taken</param>
    /// <param name="positionPunch">upper or lower</param>
    void Hit(bool isPlayer, float damage, bool positionPunch, bool success)
    {
        if (success)
        {
            if (isPlayer == IsPlayer)
            {
                availableHealth -= damage;
                SetHealthValue();

                //if in arcade mode, set all damage as a permanent reductuion of future max healths
                if (IsPlayer && gameManager.currentGameMode == GameMode.ArcadeMode)
                {
                    gameManager.PlayerHealthArcade = availableHealth;
                    Console.Log(gameManager.PlayerHealthArcade + " max future health");
                }
                //update performance
                if (IsPlayer && PerformanceReport.Instance != null) PerformanceReport.Instance.PlayerHealth = Mathf.RoundToInt(availableHealth);

                //if dead
                if (availableHealth <= 0)
                {

                    gameManager.BattleCount++;
                    if (isPlayer)
                    {
                        //Enemy wins
                        AnimationManager.ChangeAnimationState(!IsPlayer, "Victory_0");
                        LevelManager.Instance.UpdateTextForDefeat();


                        //if (gameManager.currentGameMode == GameMode.StoryMode) gameManager.StoryLevel.Value = 1;
                        if (gameManager.currentGameMode == GameMode.ArcadeMode && gameManager.BestArcadeRun.Value < gameManager.ArcadeFightsCompleted)
                        {
                            gameManager.BestArcadeRun.Value = gameManager.ArcadeFightsCompleted;
                        }
                        GameManager.Instance.BattlesLost++;
                    }
                    else
                    {
                        //Player wins
                        List<EnemyInfo.AnimalType> aux = gameManager.TypesDiscovered.Value;

                        if (!aux.Exists(u => u == gameManager.currentlyFightingType)) gameManager.TypesDiscovered.Value.Add(gameManager.currentlyFightingType);

                        AnimationManager.ChangeAnimationState(!IsPlayer, $"Victory_{Random.Range(0, 3)}");
                        LevelManager.Instance.UpdatePerformanceText();
                        if (GameManager.Instance.currentGameMode == GameMode.StoryMode)
                        {
                            gameManager.StoryLevel.Value = Mathf.Clamp(GameManager.Instance.StoryLevel.Value + 1, 1, MechanicManager.Instance.Levels.Length + 1);
                            //If we finished story mode
                            if (LevelManager.Instance.IsLastWin)
                            {
                                gameManager.HasFinishedStoryMode.Value = true;
                                gameManager.PlayerStats.ResetValuesToZero();
                                gameManager.ResetUpgradesMade();
                                gameManager.ResetJobExperience();
                                CutSceneManager.Instance.SetCutScene(CutSceneManager.CutScene.End, true);
                            }
                        }
                        if (gameManager.currentGameMode == GameMode.ArcadeMode)
                        {
                            //Update number of fights fought in arcade and check if we have bested our best run
                            gameManager.ArcadeFightsCompleted++;
                            if (gameManager.ArcadeFightsCompleted > gameManager.BestArcadeRun.Value) gameManager.BestArcadeRun.Value = gameManager.ArcadeFightsCompleted;
                        }
                        GameManager.Instance.BattlesWon++;
                    }

                    AnimationManager.ChangeAnimationState(IsPlayer, "Faint");
                    OpponentAI.Instance.fightEnd = true;

                    //Fighter is knocked out
                    Console.Log("Combat should end");
                    //gameManager.PlayedStoryLevelAfterRespite.Value = true;
                    GameManager.Instance.Win = !isPlayer;
                    GameManager.Instance.TransitionState(GameStates.Results);
                    return;
                }
                else
                {
                    //Never flinch if ceotter
                    Console.Log(GameManager.Instance.currentlyFightingType.ToString());
                    if (GameManager.Instance.currentlyFightingType != EnemyInfo.AnimalType.CEO || IsPlayer)
                    {
                        //chance to flinch
                        if (Random.Range(0f, 1f) > (IsPlayer ? 0.4f : 0.75f)
                            //cases in which enemy always flinches, when punching bag
                            || GameManager.Instance.currentlyFightingType == EnemyInfo.AnimalType.PunchingBag)
                        {
                            AnimationManager.ChangeAnimationState(IsPlayer, "Flinch");
                            AnimationManager.AnimationTrigger(IsPlayer, "Idle");
                        }
                    }

                }
            }
        }
    }
    #endregion

    #region Health Stuff
    /// <summary>
    /// set health slider value
    /// </summary>
    void SetHealthValue()
    {
        healthSlider.value = availableHealth / maxHealth;
        if (barFlashOnLow.enabled)
            barFlashOnLow?.FlashBar();
    }
    /// <summary>
    /// get available health
    /// </summary>
    public float GetHealth() => availableHealth;
    /// <summary>
    /// get max health
    /// </summary>
    public float GetMaxHealth() => maxHealth;
    #endregion
}