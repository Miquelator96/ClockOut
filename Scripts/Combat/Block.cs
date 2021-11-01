using UnityEngine;

/// <summary>
/// Handles block of character based on <see cref="IsPlayer"/>
/// </summary>
[RequireComponent(typeof(Punch))]
public class Block : MonoBehaviour
{
    //player, damage, upper/lower, punch success
    public static System.Action<bool, float, bool, bool> OnHit;
    //first bool isPlayer, second bool blockingState, third bool position
    public static System.Action<bool, bool, bool> OnBlock;
    //first bool position, second bool result
    public static System.Action<bool, bool> OnAIFeedbackPunch;

    #region Variables
    public bool IsPlayer;
    Transform upperParticlePoint, lowerParticlePoint;
    
    public bool blocking { get; private set; }
    public bool positionBlock { get; private set; }
    #endregion

    #region Unity Functions
    private void Awake()
    {
        Punch.OnTakePunch -= Hit;
        Punch.OnTakePunch += Hit;
        PlayerInput.OnInputBlock -= ToggleBlock;
        PlayerInput.OnInputBlock += ToggleBlock;
        OpponentAI.OnAIBlock -= ToggleBlock;
        OpponentAI.OnAIBlock += ToggleBlock;
    }
    private void OnDestroy()
    {
        Punch.OnTakePunch -= Hit;
        PlayerInput.OnInputBlock -= ToggleBlock;
        OpponentAI.OnAIBlock -= ToggleBlock;
    }
    void Start()
    {
        upperParticlePoint = GameObject.Find("UpperParticlePoint").transform;
        lowerParticlePoint = GameObject.Find("LowerParticlePoint").transform;
    }
    #endregion

    #region Block Stuff
    /// <summary>
    /// Toggles whether fighter is blocking or not
    /// </summary>
    /// <param name="isPlayer">players or enemies input</param>
    /// <param name="blocking">blocking or not</param>
    /// <param name="position">upper or lower</param>
    void ToggleBlock(bool isPlayer, bool blocking, bool position)
    {
        if (isPlayer == IsPlayer)
        {
            this.blocking = blocking;
            positionBlock = position;

            string animName = position ? "Upper_Block" : "Lower_Block";
            AnimationManager.ChangeAnimationState(IsPlayer, blocking ? animName : "Idle");
        }
    }
    #endregion

    #region Take Hit
    /// <summary>
    /// Hit taken, calculating success
    /// </summary>
    /// <param name="isPlayer">player or enemy hit</param>
    /// <param name="damage">damage done</param>
    /// <param name="positionPunch">upper or lower</param>
    void Hit(bool isPlayer, float damage, bool positionPunch)
    {
        GameObject particle;
        float orientation = isPlayer ? 0 : 180 ;

        //Lets the AI know if the a thrown punch was successful
        if (isPlayer == IsPlayer)
        {

            //in case the punched character is not blocking or blocking in the wrong position
            if (!blocking || positionPunch != positionBlock)
            {
                //If the punched person is the player, let the AIFrenzy script know to update values
                if (isPlayer) OnAIFeedbackPunch?.Invoke(positionPunch, true);
                
                OnHit?.Invoke(IsPlayer, damage, positionPunch, true);
                AudioManager.SharedInstance.PlayClip(AudioManager.Clips.HitPunch);
                particle = ParticleManager.SharedInstance.GetPooledParticle("Hit_Taken" + "(Clone)", "Particle");
                if (particle != null)
                {
                    particle.transform.parent = positionPunch ? upperParticlePoint.transform : lowerParticlePoint.transform;
                    particle.transform.rotation = Quaternion.Euler(0, 0, orientation);
                    particle.transform.localPosition = Vector3.zero;
                    particle.transform.localScale = Vector3.one;
                    particle.SetActive(true);
                }
                return;
            }
            else
            {
                //If the punched person is the player and he has correctly block, update AIFrenzy
                if (isPlayer) OnAIFeedbackPunch?.Invoke(positionPunch, false);           
            }

            OnHit?.Invoke(IsPlayer, 0, positionPunch, false);
            AudioManager.SharedInstance.PlayClip(AudioManager.Clips.BlockedPunch);

            //Particle stuff
            particle = ParticleManager.SharedInstance.GetPooledParticle("Blocked" + "(Clone)", "Particle"); 
            if (particle != null) {
                particle.transform.parent = positionBlock ? upperParticlePoint : lowerParticlePoint;
                particle.transform.rotation = Quaternion.Euler(0, 0, orientation);
                particle.transform.localPosition = Vector3.zero;
                particle.transform.localScale = Vector3.one;
                particle.SetActive(true);
            }
        }
    }
    #endregion
}