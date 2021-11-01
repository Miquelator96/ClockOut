using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIBlocking : MonoBehaviour
{
    Health bossHealth;

    public bool canBlockHigh, canBlockLow;

    public bool perfectBlockIsRunning = false;
    IEnumerator SpamHigh, SpamLow;
    int spamMeterHigh = 0, spamMeterLow = 0;
    float spamModifier = 2.65f, levelModifier = 1.25f;
    int levelIncrease = 1;
    float chanceThreshold = 9;

    //Testing 
    float punchResetTime = 1.5f;

    OpponentAI opponentAI;

    void Start()
    {
        //SimulatePunches(spamModifier, levelModifier, chanceThreshold);
        bossHealth = GetComponent<Health>();
        opponentAI = GetComponent<OpponentAI>();
    }
    void Awake()
    {
        PlayerInput.OnInputPunch -= IncomingPlayerPunch;
        PlayerInput.OnInputPunch += IncomingPlayerPunch;
        GameManager.OnStateMove -= EndBattle;
        GameManager.OnStateMove += EndBattle;
    }
    private void OnDestroy()
    {
        PlayerInput.OnInputPunch -= IncomingPlayerPunch;
        GameManager.OnStateMove -= EndBattle;
    }

    void EndBattle(GameStates current, GameStates next)
    {
        if (current == GameStates.Fight)
        {
            CancelInvoke();
            enabled = false;
        }
    }
    //Execute to realize an immidiate block
    IEnumerator ExecuteAutomaticBlock(float time, bool position)
    {
        perfectBlockIsRunning = true;
        yield return new WaitForSeconds(time);
        Console.Log("Execute automatic done");

        perfectBlockIsRunning = false;

        if (bossHealth.GetHealth() > 0)
        {
            opponentAI.goToBlocking = position ? 1 : 2;
        }
        CancelInvoke();
        opponentAI.StateChoose();
    }

    void SimulatePunches(float spamModifier, float levelModifier, float barrier)
    {
#if UNITY_EDITOR
        for (int i = 0; i < 11; i++)
        {
            for (int k = 1; k <= 4; k++)
            {
                int blocksRealized = 0;
                for (int j = 0; j < 1000; j++) if (Random.Range(0, Mathf.Pow(k, spamModifier) + Mathf.Pow(i + levelIncrease, levelModifier)) > barrier) blocksRealized++;
                Console.Log($"Level: {i}. Blocks realized: {blocksRealized}.");
            }
        }
#endif
    }

    //Executes when player presses one punch
    void IncomingPlayerPunch(bool isPlayer, bool position)
    {

        //if level progression doesn't allow to block this player punch, leave
        if (position && !canBlockHigh || !position && !canBlockLow) return;

        if (isPlayer)
        {
            //We update the spams and reset the coroutine that waits for the player to stop attacking to reset the spam counters
            if (position)
            {
                spamMeterHigh = Mathf.Min(spamMeterHigh++, 10);
                if (opponentAI.WaitingPlayerAttackHighIsRunning)
                {
                    StopCoroutine(SpamHigh);
                }
                
                opponentAI.WaitingPlayerAttackHighIsRunning = true;
                SpamHigh = ResetSpamHigh();
               
                StartCoroutine(SpamHigh);
            }
            else
            {
                spamMeterLow = Mathf.Min(spamMeterLow++, 10);
                if (opponentAI.WaitingPlayerAttackLowIsRunning)
                {
                    Console.LogWarning("Stopped Low");
                    StopCoroutine(SpamLow);
                }
                opponentAI.WaitingPlayerAttackLowIsRunning = true;
                SpamLow = ResetSpamLow();
                StartCoroutine(SpamLow);
            }

            GameManager gameManager = GameManager.Instance;

            if (!opponentAI.onFrenzyPlayer)
            {
                int level = 0;
                switch (gameManager.currentGameMode)
                {
                    case GameMode.StoryMode:
                        level = gameManager.StoryLevel.Value - 1;
                        break;
                    case GameMode.RematchMode:
                        level = gameManager.enemyRematchLevel - 1;
                        break;
                    case GameMode.ArcadeMode:
                        level = Mathf.Min(gameManager.ArcadeFightsCompleted+4, 16);
                        break;
                }

                float spam =  Mathf.Pow((position ? spamMeterHigh : spamMeterLow), spamModifier) + Mathf.Pow(level + levelIncrease, levelModifier);
                
                if(Random.Range(0f, spam) > chanceThreshold && !opponentAI.exhausted)
                {
                    float t = 0;//Mathf.Max(0, 3 - spam);
                    StartCoroutine(ExecuteAutomaticBlock(t, position));
                }
            }
        }
    }
    //if no attack is received from the player, the AI no longer cares about interrupting alernate attacks
    IEnumerator ResetSpamHigh()
    {

        yield return new WaitForSeconds(punchResetTime);
        spamMeterHigh = 0;
        opponentAI.goToBlocking = 0;
        opponentAI.WaitingPlayerAttackHighIsRunning = false;
    }
    IEnumerator ResetSpamLow()
    {
        yield return new WaitForSeconds(punchResetTime);
        spamMeterLow = 0;
        opponentAI.goToBlocking = 0;
        opponentAI.WaitingPlayerAttackHighIsRunning = false;
    }
}