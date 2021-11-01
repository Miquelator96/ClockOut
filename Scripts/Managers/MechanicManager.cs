using UnityEngine;

/// <summary>
/// Holds <see cref="LevelMechanics"/> that everything should reference
/// </summary>
public class MechanicManager : MonoBehaviour
{
    [System.Serializable] public enum Mechanics { Low_Punch, High_Punch, Low_Block, High_Block, Frenzy }

    public static MechanicManager Instance;

    public LevelMechanics[] Levels;

    #region Unity Functions
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion
}