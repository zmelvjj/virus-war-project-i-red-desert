using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Renderer))]
public class RangeDamageDealer : MonoBehaviour
{
    [SerializeField] Material hitMaterial;
    [SerializeField, Min(0.01f)] float projectileSpeed = 10f;
    [SerializeField] Color normalRangeColor = Color.red;
    [SerializeField] Color comboRangeColor = Color.yellow;
    [SerializeField] SneezeParticleController sneezeParticleController;

    BoxCollider m_RangeCollider;
    Renderer m_RangeRenderer;
    MaterialPropertyBlock m_RangePropertyBlock;
    readonly List<AttackPulse> m_Pulses = new();
    readonly Collider[] m_OverlapResults = new Collider[32];
    float m_AttackElapsed;
    bool m_IsComboColor;

    void Awake()
    {
        m_RangeCollider = GetComponent<BoxCollider>();
        m_RangeRenderer = GetComponent<Renderer>();
        m_RangePropertyBlock = new MaterialPropertyBlock();
        SetRangeColor(false, true);
    }

    void Update()
    {
        if (!GameFlowManager.IsMainGame)
        {
            m_AttackElapsed = 0f;
            m_Pulses.Clear();
            SetRangeColor(false);
            return;
        }

        m_AttackElapsed += Time.deltaTime;
        var attackInterval = 1f / PlayerStats.Instance.AttackSpeed;
        while (m_AttackElapsed >= attackInterval)
        {
            m_AttackElapsed -= attackInterval;
            FirePulse();
            sneezeParticleController.Fire();
        }

        AdvancePulses(Time.deltaTime);
    }

    void FirePulse()
    {
        var colliderCenter = m_RangeCollider.center;
        var halfSize = m_RangeCollider.size * 0.5f;
        var start = transform.TransformPoint(new Vector3(
            colliderCenter.x,
            colliderCenter.y,
            colliderCenter.z - halfSize.z));
        var end = transform.TransformPoint(new Vector3(
            colliderCenter.x,
            colliderCenter.y,
            colliderCenter.z + halfSize.z));
        var direction = end - start;
        var distance = direction.magnitude;
        var baseDamage = AbilityManager.Instance.ResolveBasicAttackBaseDamage(
            PlayerStats.Instance.Damage,
            out var comboTriggered);
        SetRangeColor(comboTriggered);

        m_Pulses.Add(new AttackPulse(
            start,
            direction / distance,
            transform.rotation,
            transform.TransformVector(Vector3.right * m_RangeCollider.size.x).magnitude,
            transform.TransformVector(Vector3.up * m_RangeCollider.size.y).magnitude,
            distance,
            projectileSpeed,
            baseDamage));
    }

    void AdvancePulses(float deltaTime)
    {
        for (var i = m_Pulses.Count - 1; i >= 0; i--)
        {
            var pulse = m_Pulses[i];
            var previousDistance = pulse.TravelledDistance;
            pulse.TravelledDistance = Mathf.Min(
                pulse.Distance,
                pulse.TravelledDistance + pulse.Speed * deltaTime);
            HitPulseOverlap(pulse, previousDistance);

            if (pulse.TravelledDistance >= pulse.Distance)
                m_Pulses.RemoveAt(i);
        }
    }

    void HitPulseOverlap(AttackPulse pulse, float previousDistance)
    {
        var sweepDistance = pulse.TravelledDistance - previousDistance;
        var center = pulse.Start
            + pulse.Direction * (previousDistance + sweepDistance * 0.5f);
        var hitCount = Physics.OverlapBoxNonAlloc(
            center,
            new Vector3(
                pulse.Width * 0.5f,
                pulse.Height * 0.5f,
                sweepDistance * 0.5f),
            m_OverlapResults,
            pulse.Rotation);

        for (var i = 0; i < hitCount; i++)
        {
            var monster = m_OverlapResults[i].GetComponentInParent<MonsterEntity>();
            if (monster == null
                || monster.CurrentHp <= 0
                || !pulse.HitTargets.Add(monster))
            {
                continue;
            }

            var hitPosition = monster.transform.position;
            var damageResult = AbilityManager.Instance.ResolveBasicAttackDamage(
                pulse.BaseDamage,
                monster);
            monster.TakeDamage(
                damageResult.FinalDamage,
                hitMaterial);
            DamagePopupManager.Instance.ShowDamage(damageResult);
            BasicAttackEvents.Raise(new BasicAttackHit(
                monster,
                hitPosition,
                pulse.Start,
                damageResult.FinalDamage));
        }
    }

    void SetRangeColor(bool comboTriggered, bool force = false)
    {
        if (!force && m_IsComboColor == comboTriggered)
            return;

        m_IsComboColor = comboTriggered;
        var color = comboTriggered ? comboRangeColor : normalRangeColor;
        m_RangeRenderer.GetPropertyBlock(m_RangePropertyBlock);
        m_RangePropertyBlock.SetColor("_BaseColor", color);
        m_RangePropertyBlock.SetColor("_Color", color);
        m_RangeRenderer.SetPropertyBlock(m_RangePropertyBlock);
    }

    sealed class AttackPulse
    {
        public readonly Vector3 Start;
        public readonly Vector3 Direction;
        public readonly Quaternion Rotation;
        public readonly float Width;
        public readonly float Height;
        public readonly float Distance;
        public readonly float Speed;
        public readonly float BaseDamage;
        public readonly HashSet<MonsterEntity> HitTargets = new();

        public float TravelledDistance;

        public AttackPulse(
            Vector3 start,
            Vector3 direction,
            Quaternion rotation,
            float width,
            float height,
            float distance,
            float speed,
            float baseDamage)
        {
            Start = start;
            Direction = direction;
            Rotation = rotation;
            Width = width;
            Height = height;
            Distance = distance;
            Speed = speed;
            BaseDamage = baseDamage;
        }
    }
}
