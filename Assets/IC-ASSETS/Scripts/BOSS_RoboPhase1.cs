using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net.Http.Headers;

[RequireComponent(typeof(EnemyController))]
public class BOSS_RoboPhase1 : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Patrol,
        Follow,
        ShootGun,
        ThrowGrenade,
        ShootRocket,
        Flinch,
        Dead
    }

    enum AttackTypes
    {
        Gun,
        Grenade,
        Rocket
    }

    public Animator animator;
    [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
    [Range(0f, 1f)]
    public float attackStopDistanceRatio = 0.5f;
    [Tooltip("The random hit damage effects")]
    public ParticleSystem[] randomHitSparks;
    public ParticleSystem[] onDetectVFX;
    public AudioClip onDetectSFX;

    [Header("Sound")]
    public AudioClip MovementSound;
    public MinMaxFloat PitchDistortionMovementSpeed;

    public AIState aiState { get; private set; }
    EnemyController m_EnemyController;
    AudioSource m_AudioSource;

    public float minShootingTime = 1f, maxShootingTime = 3f;
    public float attackCooldown = 2f;

    bool _attackOnCooldown;

    [Range(0, 1)]
    public float shootChance = .7f, grenadeChance = .2f, rocketChance = .1f;
    [Range(0, 1)]
    public float flinchChance = .3f;

    public float runSpeedMultipier = 1.5f;

    public float walkSpeed = 13f;

    public float shootGunRange = 15, throwGrenadeRange = 20, shootRocketRange = 32;

    bool isDead = false;

    const string _spawnAnim = "SPAWN";
    const string _walkAnim = "walk", _damagedWalkAnim = "limpingwalk";
    const string _runAnim = "run", _damagedRunAnim = "limpingrun";
    const string _shootGunAnim = "shoot", _grenadeThrowAnim = "throwgrenade", _shootRocketAnim = "shoot_rocket";
    const string _kickAttackAnim = "frontkick";
    const string _flinchAnim = "flinch";
    string[] _deathAnims = new string[] { "dieback1", "diebackwards", "dieforward", "diegutshot", "diesimple" };

    void Start()
    {
        m_EnemyController = GetComponent<EnemyController>();
        DebugUtility.HandleErrorIfNullGetComponent<EnemyController, EnemyMobile>(m_EnemyController, this, gameObject);

        m_EnemyController.onAttack += OnAttack;
        m_EnemyController.onDetectedTarget += OnDetectedTarget;
        m_EnemyController.onLostTarget += OnLostTarget;
        m_EnemyController.SetPathDestinationToClosestNode();
        m_EnemyController.onDamaged += OnDamaged;

        // Start idling
        aiState = AIState.Idle;

        // adding a audio source to play the movement sound on it
        m_AudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);
        m_AudioSource.clip = MovementSound;
        m_AudioSource.Play();

        Invoke("StartPatrol", 2f);
    }

    void StartPatrol()
    {
        aiState = AIState.Patrol;
        animator.CrossFadeInFixedTime(_spawnAnim, .1f);
    }

    void Update()
    {
        if (isDead) return;
        UpdateAIStateTransitions();
        UpdateCurrentAIState();

        m_EnemyController.m_NavMeshAgent.speed = aiState == AIState.Patrol ? walkSpeed : walkSpeed * runSpeedMultipier;


        // changing the pitch of the movement sound depending on the movement speed
        m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.min, PitchDistortionMovementSpeed.max, m_EnemyController.m_NavMeshAgent.velocity.magnitude / m_EnemyController.m_NavMeshAgent.speed);
    }

    void UpdateAIStateTransitions()
    {
        // Handle transitions 
        switch (aiState)
        {
            case AIState.Idle:
                break;
            case AIState.Follow:
                var randomAttack = Random.Range(0f, 1f);

                //default is gun
                AttackTypes pickedAttack = AttackTypes.Gun;
                AIState nextAttack = AIState.ShootGun;
                if (randomAttack < rocketChance)
                {
                    pickedAttack = AttackTypes.Rocket;
                    nextAttack = AIState.ShootRocket;
                }
                else if (randomAttack > rocketChance && randomAttack < grenadeChance)
                {
                    pickedAttack = AttackTypes.Grenade;
                    nextAttack = AIState.ThrowGrenade;
                }

                //Perdón Wain
                //Perdón Row
                //Los quiero muchísimo
                switch (pickedAttack)
                {
                    case AttackTypes.Gun:
                        m_EnemyController.m_DetectionModule.attackRange = shootGunRange;
                        break;
                    case AttackTypes.Grenade:
                        m_EnemyController.m_DetectionModule.attackRange = throwGrenadeRange;
                        break;
                    case AttackTypes.Rocket:
                        m_EnemyController.m_DetectionModule.attackRange = shootRocketRange;
                        break;
                    default:
                        m_EnemyController.m_DetectionModule.attackRange = shootGunRange;
                        break;
                }

                // Transition to attack when there is a line of sight to the target
                if (m_EnemyController.isSeeingTarget && m_EnemyController.isTargetInAttackRange)
                {
                    aiState = nextAttack;
                    m_EnemyController.SetNavDestination(transform.position);
                }
                break;
        }
    }

    IEnumerator AttackLoopCouroutine()
    {
        var randomDuration = Random.Range(minShootingTime, maxShootingTime);
        var currentDuration = 0f;
        var instruction = new WaitForEndOfFrame();

        while (true)
        {
            if (currentDuration > randomDuration || !m_EnemyController.m_DetectionModule.isSeeingTarget)
            {
                aiState = AIState.Follow;
                animator.CrossFadeInFixedTime(GetMovementAnim(aiState), .1f);
                yield break;
            }

            yield return instruction;

            currentDuration += Time.deltaTime;
        }
    }

    /// <summary>
    /// FUCKING HARDCODED TIME
    /// </summary>
    /// <returns></returns>
    IEnumerator AttackSingleCouroutine(float t)
    {
        yield return new WaitForSeconds(t);

        aiState = AIState.Follow;
        animator.CrossFadeInFixedTime(GetMovementAnim(aiState), .1f);
    }

    IEnumerator FlinchCouroutine(float t)
    {
        yield return new WaitForSeconds(t);

        aiState = AIState.Follow;
        animator.CrossFadeInFixedTime(GetMovementAnim(aiState), .1f);
    }

    string GetMovementAnim(AIState stateToChangeTo)
    {
        if (stateToChangeTo == AIState.Follow) return m_EnemyController.m_Health.isCritical() ? _damagedRunAnim : _runAnim;
        else return m_EnemyController.m_Health.isCritical() ? _damagedWalkAnim : _walkAnim;
    }

    void UpdateCurrentAIState()
    {
        // Handle logic 
        switch (aiState)
        {
            case AIState.Patrol:
                m_EnemyController.UpdatePathDestination();
                m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
                break;
            case AIState.Follow:
                m_EnemyController.SetNavDestination(m_EnemyController.knownDetectedTarget.transform.position);
                m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                m_EnemyController.OrientWeaponsTowards(m_EnemyController.knownDetectedTarget.transform.position);
                break;
            case AIState.ShootGun:
            case AIState.ShootRocket:
            case AIState.ThrowGrenade:
                if (Vector3.Distance(m_EnemyController.knownDetectedTarget.transform.position, m_EnemyController.m_DetectionModule.detectionSourcePoint.position)
                    >= (attackStopDistanceRatio * m_EnemyController.m_DetectionModule.attackRange))
                {
                    m_EnemyController.SetNavDestination(m_EnemyController.knownDetectedTarget.transform.position);
                }
                else
                {
                    m_EnemyController.SetNavDestination(transform.position);
                }
                m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                m_EnemyController.TryAttack(m_EnemyController.knownDetectedTarget.transform.position);
                break;
        }
    }

    void OnAttack()
    {
        switch (aiState)
        {
            case AIState.ThrowGrenade:
                animator.CrossFadeInFixedTime(_grenadeThrowAnim, .1f);
                StartCoroutine(AttackSingleCouroutine(2.5f));
                break;
            case AIState.ShootRocket:
                animator.CrossFadeInFixedTime(_shootRocketAnim, .1f);
                StartCoroutine(AttackSingleCouroutine(2.5f));
                break;
            case AIState.ShootGun:
            default:
                animator.CrossFadeInFixedTime(_shootGunAnim, .1f);
                StartCoroutine(AttackLoopCouroutine());
                break;
        }
    }

    void OnDetectedTarget()
    {
        if (aiState == AIState.Patrol) aiState = AIState.Follow;

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Play();
        }

        if (onDetectSFX)
        {
            AudioUtility.CreateSFX(onDetectSFX, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }

        animator.CrossFadeInFixedTime(_shootGunAnim, .1f);
    }

    void OnLostTarget()
    {
        aiState = aiState == AIState.Follow ? AIState.Patrol : AIState.Follow;

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Stop();
        }

        animator.CrossFadeInFixedTime(GetMovementAnim(aiState), .1f);
    }

    void OnDamaged()
    {
        if (randomHitSparks.Length > 0)
        {
            int n = Random.Range(0, randomHitSparks.Length - 1);
            randomHitSparks[n].Play();
        }

        var rndFlinch = Random.Range(0, 100f);
        if (rndFlinch < flinchChance)
        {
            animator.CrossFadeInFixedTime(_flinchAnim, .1f);
            StartCoroutine(FlinchCouroutine(2f));
        }
    }

    public void HandleDeath()
    {
        animator.CrossFadeInFixedTime(_deathAnims[Random.Range(0, _deathAnims.Length)], .1f);
        isDead = true;
        m_EnemyController.m_NavMeshAgent.velocity = Vector3.zero;
        m_EnemyController.m_NavMeshAgent.speed = 0f;
        m_EnemyController.m_NavMeshAgent.isStopped = true;

        StopAllCoroutines();
        CancelInvoke();
        m_EnemyController.m_DetectionModule.enabled = false;
        m_EnemyController.m_NavMeshAgent.enabled = false;
        runSpeedMultipier = 0;
        aiState = AIState.Dead;
    }
}
