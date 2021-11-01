using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stats", menuName = "Stats")]
public class Stats : ScriptableObject
{
    [Header("Stats")]
    [Range(0, 35)] [SerializeField] private int TotalStats = 5;
    private int previousStats;
    [Range(0, 30)] [SerializeField] public int Health = 0;
    [Range(0, 30)] [SerializeField] public int Stamina = 0;
    [Range(0, 30)] [SerializeField] public int Power = 0;
    [Range(0, 30)] [SerializeField] public int Agility = 0;
    [Range(0, 30)] [SerializeField] public int Defense = 0;
    [Space]
    [Range(.01f, 1f)] [SerializeField] private float BuildHealth;
    [Range(.01f, 1f)] [SerializeField] private float BuildStamina;
    [Range(.01f, 1f)] [SerializeField] private float BuildPower;
    [Range(.01f, 1f)] [SerializeField] private float BuildAgility;
    [Range(.01f, 1f)] [SerializeField] private float BuildDefense;
    [Space]

    SaveDataGeneric<int> PlayerHealth;
    SaveDataGeneric<int> PlayerStamina;
    SaveDataGeneric<int> PlayerPower;
    SaveDataGeneric<int> PlayerAgility;
    SaveDataGeneric<int> PlayerDefense;

    [Space]
    //The base stats for any fighter. They do not change across fighters.

    //Max Health
    public const float BaseHealth = 400;
    const float playerHealthCoefficient = 122;
    const float enemyHealthCoefficient = 1.2f;
    //Max Stamina
    public const float BaseStamina = 200;
    const float playerStaminaCoefficient = 33;
    const float enemyStaminaCoefficient = 1.2f;
    //Fighter inflicts power damage per punch.
    public const float BasePower = 16;
    const float playerPowerCoefficient = 3;
    const float enemyPowerCoefficient = 1.15f;
    //After initiating a punch, the fighter must wait agility seconds before initiating another action
    public const float BaseAgility = 0.42f;
    const float playerAgilityCoefficient = -0.02f;
    const float enemyAgilityCoefficient = 0.95f;
    //After successfully blocking a punch, the puncher loses stamina multiplied by the blocker's defense
    public const float BaseDefense = 1.05f;
    const float playerDefenseCoefficient = 0.1f;
    const float enemyDefenseCoefficient = 1.1f;

    float GetStat(float baseStat, float coefficient, int upgradesOwned, bool isPlayer)
    {
        if (isPlayer)
        {
            return (upgradesOwned * coefficient) + baseStat;
        }
        return (baseStat * Mathf.Pow(coefficient, (upgradesOwned - 2 * Mathf.FloorToInt(upgradesOwned / 4))));
    }
    public float GetMaxHealth(bool isPlayer) => GetStat(BaseHealth, isPlayer ? playerHealthCoefficient : enemyHealthCoefficient, Health, isPlayer);
    public float GetMaxStamina(bool isPlayer) => GetStat(BaseStamina, isPlayer ? playerStaminaCoefficient : enemyStaminaCoefficient, Stamina, isPlayer);
    public float GetPower(bool isPlayer) => GetStat(BasePower, isPlayer ? playerPowerCoefficient : enemyPowerCoefficient, Power, isPlayer);
    public float GetAgility(bool isPlayer) => GetStat(BaseAgility, isPlayer ? playerAgilityCoefficient : enemyAgilityCoefficient, Agility, isPlayer);
    public float GetDefense(bool isPlayer) => GetStat(BaseDefense, isPlayer ? playerDefenseCoefficient : enemyDefenseCoefficient, Defense, isPlayer);
    public int GetBaseStat(string stat)
    {
        int value = 0;
        switch (stat)
        {
            case "Health":
                value = Health;
                break;
            case "Stamina":
                value = Stamina;
                break;
            case "Power":
                value = Power;
                break;
            case "Defense":
                value = Defense;
                break;
            case "Agility":
                value = Agility;
                break;
            default:
                Console.Log("Cannot get non-existing stat");
                break;
        }
        return value;
    }

    public void UpgradeHealth(bool isPlayer)
    {
        Health++;
        if (isPlayer) PlayerHealth.Value = Health;
    }
    public void UpgradeStamina(bool isPlayer)
    {
        Stamina++;
        if (isPlayer) PlayerStamina.Value = Stamina;
    }
    public void UpgradePower(bool isPlayer)
    {
        Power++;
        if (isPlayer) PlayerPower.Value = Power;
    }
    public void UpgradeAgility(bool isPlayer)
    {
        Agility++;
        if (isPlayer) PlayerAgility.Value = Agility;
    }
    public void UpgradeDefense(bool isPlayer)
    {
        Defense++;
        if (isPlayer) PlayerDefense.Value = Defense;
    }

    /// <summary>
    /// call to update enemy total stats
    /// </summary>
    /// <param name="value">value to update to</param>
    public void SetTotalStats(int value)
    {
        TotalStats = value;
        OnValidate();
    }

    public void InitSavedData()
    {
        PlayerHealth = new SaveDataGeneric<int>(nameof(PlayerHealth), 0);
        PlayerStamina = new SaveDataGeneric<int>(nameof(PlayerStamina), 0);
        PlayerPower = new SaveDataGeneric<int>(nameof(PlayerPower), 0);
        PlayerAgility = new SaveDataGeneric<int>(nameof(PlayerAgility), 0);
        PlayerDefense = new SaveDataGeneric<int>(nameof(PlayerDefense), 0);

        Health = PlayerHealth.Value;
        Stamina = PlayerStamina.Value;
        Power = PlayerPower.Value;
        Agility = PlayerAgility.Value;
        Defense = PlayerDefense.Value;
    }

    public void ResetValuesToZero()
    {
        PlayerHealth.Value = 0;
        PlayerStamina.Value = 0;
        PlayerPower.Value = 0;
        PlayerAgility.Value = 0;
        PlayerDefense.Value = 0;

        Health = PlayerHealth.Value;
        Stamina = PlayerStamina.Value;
        Power = PlayerPower.Value;
        Agility = PlayerAgility.Value;
        Defense = PlayerDefense.Value;
    }

    /// <summary>
    /// Method to calculate stats based on total stats
    /// </summary>
    private void OnValidate()
    {
        if (previousStats != TotalStats)
        {
            previousStats = TotalStats;
            float remainingPoints = TotalStats - 5;

            float amount = BuildHealth * remainingPoints;
            int rounded = Mathf.Clamp(Mathf.RoundToInt(amount), 0, 35);
            Health = 1 + rounded;
            remainingPoints -= rounded;

            amount = BuildStamina * remainingPoints / (1 - BuildHealth);
            rounded = Mathf.Clamp(Mathf.RoundToInt(amount), 0, 35);
            Stamina = 1 + rounded;
            remainingPoints -= rounded;

            amount = BuildPower * remainingPoints / (1 - BuildHealth - BuildStamina);
            rounded = Mathf.Clamp(Mathf.RoundToInt(amount), 0, 35);
            Power = 1 + rounded;
            remainingPoints -= rounded;

            amount = BuildAgility * remainingPoints / (1 - BuildHealth - BuildStamina - BuildPower);
            rounded = Mathf.Clamp(Mathf.RoundToInt(amount), 0, 35);
            Agility = 1 + rounded;
            remainingPoints -= rounded;

            amount = BuildDefense * remainingPoints / (1 - BuildHealth - BuildStamina - BuildPower - BuildAgility);
            rounded = Mathf.Clamp(Mathf.RoundToInt(amount), 0, 35);
            Defense = 1 + rounded;
            remainingPoints -= rounded;
        }
    }
}