using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(EnemyController))]
public class BOSS_RoboPhase2 : MonoBehaviour
{
    public enum AIState
    {
        Follow,
        ShootGun,
        Flinch,
        Dead
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

    public float minShootingTime = 3f, maxShootingTime = 5f;
    public float attackCooldown = 2f;

    bool _attackOnCooldown;

    [Range(0, 1)]
    public float flinchChance = .3f;

    public float runSpeedMultipier = 1.5f;

    public float walkSpeed = 1.5f;

    public float shootGunRange = 15;

    public bool isDead = false;

    const string _walkAnim = "walk";
    const string _runAnim = "run";
    const string _shootGunAnim = "spinup";
    const string _flinchAnim = "flinch";
    readonly string[] _deathAnims = new string[] { "die_violent" };

    const string _runAnParam = "run", _walkAnParam = "walk";

    public AudioClip deathSFX;

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
        aiState = AIState.Follow;
        animator.SetBool(_walkAnParam, true);
        animator.SetBool(_runAnParam, false);

        // adding a audio source to play the movement sound on it
        m_AudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);
        m_AudioSource.clip = MovementSound;
        m_AudioSource.Play();
    }

    void Update()
    {
        if (isDead) return;

        UpdateAIStateTransitions();
        UpdateCurrentAIState();

        // changing the pitch of the movement sound depending on the movement speed
        m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.min, PitchDistortionMovementSpeed.max, m_EnemyController.m_NavMeshAgent.velocity.magnitude / m_EnemyController.m_NavMeshAgent.speed);
    }

    void UpdateAIStateTransitions()
    {
        // Handle transitions 
        switch (aiState)
        {
            case AIState.Follow:
                if (!_attackOnCooldown)
                {
                    // Transition to attack when there is a line of sight to the target
                    if (m_EnemyController.isSeeingTarget && m_EnemyController.isTargetInAttackRange)
                    {
                        aiState = AIState.ShootGun;
                        m_EnemyController.SetNavDestination(transform.position);
                        animator.SetBool(_walkAnParam, false);
                        animator.SetBool(_runAnParam, false);
                    }
                }
                break;
        }
    }

    IEnumerator AttackLoopCouroutine()
    {
        var randomDuration = Random.Range(minShootingTime, maxShootingTime);
        var currentDuration = 0f;
        var instruction = new WaitForEndOfFrame();

        _attackOnCooldown = true;

        while (true)
        {
            if (currentDuration > randomDuration)
            {
                StartCoroutine(AttackCooldownCoroutine(attackCooldown));

                animator.CrossFadeInFixedTime("idle", .1f);
                aiState = AIState.Follow;
                yield break;
            }

            yield return instruction;

            currentDuration += Time.deltaTime;
        }
    }

    IEnumerator FlinchCouroutine(float t)
    {
        _attackOnCooldown = true;
        yield return new WaitForSeconds(t);

        //half attack cooldown because balance
        StartCoroutine(AttackCooldownCoroutine(attackCooldown));

        animator.CrossFadeInFixedTime("idle", .1f);
        aiState = AIState.Follow;
    }

    IEnumerator AttackCooldownCoroutine(float t)
    {
        _attackOnCooldown = true;

        yield return new WaitForSeconds(t);

        _attackOnCooldown = false;
    }

    void UpdateCurrentAIState()
    {
        // Handle logic 
        switch (aiState)
        {
            case AIState.Follow:
                var closeDistanceToEnemy = Vector3.Distance(m_EnemyController.knownDetectedTarget.transform.position, transform.position) > (m_EnemyController.m_DetectionModule.attackRange * 1.3f);

                if (closeDistanceToEnemy)
                {
                    m_EnemyController.m_NavMeshAgent.speed = walkSpeed * runSpeedMultipier;
                    animator.SetBool(_walkAnParam, false);
                    animator.SetBool(_runAnParam, true);
                }
                else
                {
                    m_EnemyController.m_NavMeshAgent.speed = walkSpeed;
                    animator.SetBool(_walkAnParam, true);
                    animator.SetBool(_runAnParam, false);
                }

                if (m_EnemyController.knownDetectedTarget != null)
                {
                    m_EnemyController.SetNavDestination(m_EnemyController.knownDetectedTarget.transform.position);
                    m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                    m_EnemyController.OrientWeaponsTowards(m_EnemyController.knownDetectedTarget.transform.position);
                }
                break;
            case AIState.ShootGun:
                m_EnemyController.SetNavDestination(transform.position);
                m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                m_EnemyController.TryAttack(m_EnemyController.knownDetectedTarget.transform.position);
                break;
        }
    }

    void OnAttack()
    {
        animator.CrossFadeInFixedTime(_shootGunAnim, .1f);
        StartCoroutine(AttackLoopCouroutine());
    }

    void OnDetectedTarget()
    {
        if (aiState == AIState.Flinch) aiState = AIState.Follow;

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Play();
        }

        if (onDetectSFX)
        {
            AudioUtility.CreateSFX(onDetectSFX, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }
    }

    void OnLostTarget()
    {
        aiState = AIState.Follow;

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Stop();
        }
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
        var coll = transform.Find("HitBox").GetComponent<Collider>();
        coll.isTrigger = true;

        animator.CrossFadeInFixedTime(_deathAnims[Random.Range(0, _deathAnims.Length)], .1f);
        isDead = true;

        GetComponent<AudioSource>().PlayOneShot(deathSFX);  

        m_EnemyController.m_NavMeshAgent.velocity = Vector3.zero;
        m_EnemyController.m_NavMeshAgent.speed = 0f;
        m_EnemyController.m_NavMeshAgent.isStopped = true;

        StopAllCoroutines();
        CancelInvoke();
        m_EnemyController.m_DetectionModule.enabled = false;
        m_EnemyController.m_NavMeshAgent.enabled = false;
        runSpeedMultipier = 0;
        aiState = AIState.Dead;
        animator.SetBool(_runAnParam, false);
        animator.SetBool(_walkAnParam, false);

        this.enabled = false;
    }
}
