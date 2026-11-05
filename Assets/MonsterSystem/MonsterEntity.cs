using System.Collections;
using UnityEngine;

public class MonsterEntity : MonoBehaviour
{
    [SerializeField] Transform damagePopupPoint;
    [SerializeField, Min(0f)] float fallbackDamagePopupHeight = 2f;

    public MonsterDefinition Definition { get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public MonsterMovementType MovementType { get; private set; }
    public Vector3 DamagePopupPosition => damagePopupPoint != null
        ? damagePopupPoint.position
        : transform.position + Vector3.up * fallbackDamagePopupHeight;

    Renderer[] m_Renderers;
    Material[][] m_OriginalMaterials;
    MaterialPropertyBlock m_HitPropertyBlock;
    Coroutine m_HitFlash;
    Animator m_Animator;
    MonsterNavMeshMovement m_NavMeshMovement;
    bool m_IsDead;

    public void Initialize(MonsterDefinition definition, int hp, MonsterMovementType movementType)
    {
        Definition = definition;
        MaxHp = hp;
        CurrentHp = hp;
        m_IsDead = false;
        MovementType = movementType;
        CacheMaterials();
        m_Animator = GetComponent<Animator>();
        m_NavMeshMovement = GetComponent<MonsterNavMeshMovement>();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null);
        if (m_Animator != null)
            m_Animator.SetInteger("HP_Ratio", (int)((float)CurrentHp / MaxHp * 100));
    }

    public void TakeDamage(int damage, Material hitMaterial)
    {
        TakeDamage(damage, hitMaterial, null);
    }

    public void TakeDamage(
        int damage,
        Material hitMaterial,
        Color? hitColor)
    {
        if (m_IsDead)
            return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        if (m_Animator != null)
            m_Animator.SetInteger("HP_Ratio", (int)((float)CurrentHp / MaxHp * 100));
        if (CurrentHp <= 0)
        {
            m_IsDead = true;
            if (m_NavMeshMovement != null)
                m_NavMeshMovement.enabled = false;

            if (DNADropSystem.HasInstance)
                DNADropSystem.Instance.Drop(Definition.RewardDNA, transform.position);

            // Destroy(gameObject);
            return;
        }

        if (hitMaterial != null)
        {
            if (m_HitFlash != null)
                StopCoroutine(m_HitFlash);

            m_HitFlash = StartCoroutine(FlashMaterial(hitMaterial, hitColor));
        }
    }

    void CacheMaterials()
    {
        m_Renderers = GetComponentsInChildren<Renderer>();
        m_OriginalMaterials = new Material[m_Renderers.Length][];

        for (var i = 0; i < m_Renderers.Length; i++)
            m_OriginalMaterials[i] = m_Renderers[i].sharedMaterials;
    }

    IEnumerator FlashMaterial(Material hitMaterial, Color? hitColor)
    {
        if (hitColor.HasValue)
        {
            m_HitPropertyBlock ??= new MaterialPropertyBlock();
            m_HitPropertyBlock.Clear();
            m_HitPropertyBlock.SetColor("_BaseColor", hitColor.Value);
            m_HitPropertyBlock.SetColor("_Color", hitColor.Value);
        }

        for (var i = 0; i < m_Renderers.Length; i++)
        {
            var materials = new Material[m_OriginalMaterials[i].Length];
            for (var j = 0; j < materials.Length; j++)
                materials[j] = hitMaterial;

            m_Renderers[i].sharedMaterials = materials;
            m_Renderers[i].SetPropertyBlock(null);

            if (hitColor.HasValue)
                m_Renderers[i].SetPropertyBlock(m_HitPropertyBlock);
        }

        yield return new WaitForSeconds(0.2f);

        for (var i = 0; i < m_Renderers.Length; i++)
        {
            m_Renderers[i].sharedMaterials = m_OriginalMaterials[i];
            m_Renderers[i].SetPropertyBlock(null);
        }

        m_HitFlash = null;
    }

    void OnDestroy()
    {
        if (MonsterManager.HasInstance)
            MonsterManager.Instance.Unregister(this);
    }
}
