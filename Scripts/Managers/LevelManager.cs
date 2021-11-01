using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In charge of changing non-performance specific values when performance happens
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public Text touchText;
    public Text TimerText;
    public Button FightButton, ResumeButton, MenuButton;
    public bool IsLastWin;

    public ResultString[] Results;

    public float startTime { get; private set; }

    #region Unity Functions
    void Awake()
    {
        OpponentAI.OnStartFight -= StartTimer;
        OpponentAI.OnStartFight += StartTimer;
        Instance = this;
    }
    private void OnDestroy()
    {
        OpponentAI.OnStartFight -= StartTimer;
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        System.Array.Sort(Results, (x, y) => x.type.CompareTo(y.type));
    }
    #endregion

    #region Text
    /// <summary>
    /// Called when player loses
    /// </summary>
    public void UpdateTextForDefeat()
    {
        ResultString result = System.Array.Find(Results, x => x.type == ResultString.Type.Lose);
        touchText.text = $"{result.text}{result.textAfter}";

        UpdateTimeText();

        FightButton.interactable = false;
        ResumeButton.interactable = false;
    }
    /// <summary>
    /// Called when player wins
    /// </summary>
    public void UpdatePerformanceText()
    {
        switch (GameManager.Instance.currentGameMode)
        {
            case GameMode.StoryMode:
                IsLastWin = GameManager.Instance.StoryLevel.Value == GameManager.Instance.HighestStoryLevel;
                if (IsLastWin)
                {
                    GameManager.Instance.StoryLevel.Value = 1;
                    GameManager.Instance.ResetAnimalPool();
                }
                Console.Log($"Story Level: {GameManager.Instance.StoryLevel.Value}, Max Level: {GameManager.Instance.HighestStoryLevel} Equal: {IsLastWin}");
                ResultString.Type type = IsLastWin ? ResultString.Type.FinalWin : ResultString.Type.Win;
                ResultString result = System.Array.Find(Results, x => x.type == type);
                touchText.text = result.text + result.textAfter;
                UpdateTimeText();
                FightButton.interactable = !IsLastWin; 
                ResumeButton.interactable = !IsLastWin;
                break;
            case GameMode.RematchMode:
                FightButton.interactable = false;
                ResumeButton.interactable = false;
                touchText.text = "You finished the rematch!";
                UpdateTimeText();
                break;
            case GameMode.ArcadeMode:
                FightButton.interactable = true;
                ResumeButton.interactable = false;
                touchText.text = "You finished the arcade fight!";
                UpdateTimeText();
                break;
        }
    }
    /// <summary>
    /// Sets <see cref="TimerText"/> <see cref="Text.text"/>
    /// </summary>
    void UpdateTimeText()
    {
        if (GameManager.Instance.currentGameMode != GameMode.ArcadeMode) TimerText.text = $"Time elapsed: {Mathf.RoundToInt(Time.time - startTime)} seconds";
        else TimerText.text = $"Fights fought: {GameManager.Instance.ArcadeFightsCompleted + 1}";
    }
    #endregion

    /// <summary>
    /// Tells <see cref="GameManager"/> to transistion to <see cref="GameStates.Fight"/>
    /// </summary>
    public void NextFight() => GameManager.Instance.TransitionState(GameStates.Fight);
    /// <summary>
    /// Tells <see cref="GameManager"/> to transistion to <see cref="GameStates.Menu"/>
    /// </summary>
    public void GoToMenu() => GameManager.Instance.TransitionState(GameStates.Menu);
    void StartTimer() => startTime = Time.time;

    #region Structs
    [System.Serializable] public struct ResultString
    {
        [System.Serializable] public enum Type { Win, Lose, FinalWin }
        public Type type;
        public string text;
        public string textAfter;
    }
    #endregion
}