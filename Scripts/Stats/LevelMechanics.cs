using UnityEngine;

/// <summary>
/// <see cref="ScriptableObject"/> holding <see cref="MechanicManager.Mechanics"/> for Player and Enemy
/// </summary>
[CreateAssetMenu(fileName = "New Level", menuName = "Level")]
public class LevelMechanics : ScriptableObject
{
    public MechanicManager.Mechanics[] PlayerMechanics;
    public MechanicManager.Mechanics[] EnemyMechanics;

    /// <summary>
    /// Sorts on validate just in case something requires the mechanics to be in a specific order
    /// </summary>
    private void OnValidate()
    {
        System.Array.Sort(PlayerMechanics);
        System.Array.Sort(EnemyMechanics);
    }
}