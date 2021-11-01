using UnityEngine;

/// <summary>
/// <see cref="ScriptableObject"/> that holds all <see cref="Animal"/> of <see cref="AnimalType"/>
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyInfo", menuName = "EnemyInfo")]
public class EnemyInfo : ScriptableObject
{
    [System.Serializable] public enum AnimalType { Bunny, Wolf, Panda, Pig, ChadRaccoon, Hippo, Snake, Cat, Sloth, Ox, PunchingBag, Beaver, Walrus, Orca, Badger, CEO, Hawk}

    public Animal[] Animals;

    #region Structs
    [System.Serializable] public struct Animal
    {
        public AnimalType Type;
        public GameObject Prefab;
        public string[] LastNames; //was last names, now is used for the whole name
        public Sprite[] Portraits;
        public Sprite[] Sprites;

        public Sprite[] Icons;
        public bool Gender;
        public string Position;
        //For the CharacterSizeSetter. Every Animal has a different scale factor
        public float ScaleFactor;
        public Stats enemyStatsType;
    }
    #endregion
}