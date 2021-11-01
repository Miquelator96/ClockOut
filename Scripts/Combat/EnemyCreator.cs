using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// generates an enemy to fight on start
/// </summary>
[RequireComponent(typeof(Image))]
public class EnemyCreator : MonoBehaviour
{
    public static int Variation { get; private set; }
    public static EnemyInfo.Animal Animal { get; private set; }

    GameObject LocalPivot;
    CharacterSizeSetter SizeSetter;

    Image idCard;
    Image fighterIcon;
    public Text NameText;
    public Text TitleText;

    #region Unity Functions
    void Start()
    {
        idCard = GetComponent<Image>();
        fighterIcon = GameObject.Find("Boss_Icon").GetComponent<Image>();
        LocalPivot = GameObject.FindGameObjectWithTag("BossPivot");
        SizeSetter = LocalPivot.GetComponent<CharacterSizeSetter>();
        GenerateBoss();
    }
    #endregion

    /// <summary>
    /// generates an enemy using <see cref="GameManager"/>
    /// </summary>
    void GenerateBoss()
    {
        EnemyInfo Info = GameManager.Instance.enemyInfo;
        //setting animal

        //Getting animal from tier if in story mode
        switch (GameManager.Instance.currentGameMode)
        {
            case GameMode.StoryMode:
                Animal = GameManager.Instance.GetAnimalFromType(GameManager.Instance.StoryModeAnimalList.Value[GameManager.Instance.StoryLevel.Value - 1]);
                GameManager.Instance.currentlyFightingType = Animal.Type;
                break;
            case GameMode.ArcadeMode:
                Animal = Info.Animals[Random.Range(1, Info.Animals.Length-1)];
                GameManager.Instance.currentlyFightingType = Animal.Type;
                break;
            case GameMode.RematchMode:
                Animal = GameManager.Instance.GetAnimalFromType(GameManager.Instance.currentlyFightingType);
                break;
        }

      //  Animal = Info.Animals[4];
      //  GameManager.Instance.currentlyFightingType = Animal.Type;

        NameText.text = $"{Animal.LastNames[0]}";
       

        //color variation
        Variation = Random.Range(0, Animal.Portraits.Length);

        //setting the fighter icon on the UI Fighter Canvas
        fighterIcon.sprite = Animal.Icons[Variation];

        //setting rest of card
        TitleText.text = Animal.Position;
        idCard.sprite = Animal.Portraits[Variation];

        //spawning boss
        SizeSetter.ScaleFactor = Animal.ScaleFactor;
        Vector3 positionBoss = Animal.Prefab.transform.localPosition;
        Vector3 scaleBoss = Animal.Prefab.transform.localScale;
        GameObject boss = Instantiate(Animal.Prefab, Vector3.zero, Quaternion.identity, LocalPivot.transform);
        boss.transform.localScale = scaleBoss;
        boss.transform.localPosition = positionBoss;
        boss.GetComponent<SpriteRenderer>().sprite = Animal.Sprites[Variation];
    }
}