using System.Collections;
using UnityEngine;

public class MonsterEntity : MonoBehaviour
{
    public MonsterDefinition Definition { get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public MonsterMovementType MovementType { get; private set; }

    Renderer[] m_Renderers;
    Material[][] m_OriginalMaterials;
    Coroutine m_HitFlash;
    Animator m_Animator;
    MonsterNavMeshMovement m_NavMeshMovement;

    public void Initialize(MonsterDefinition definition, int hp, MonsterMovementType movementType)
    {
        Definition = definition;
        MaxHp = hp;
        CurrentHp = hp;
        MovementType = movementType;
        CacheMaterials();
        m_Animator = GetComponent<Animator>();
        m_NavMeshMovement = GetComponent<MonsterNavMeshMovement>();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null);
        //m_Animator.SetInteger("HP_Ratio", (int)CurrentHp/MaxHp*100);

    }

    public void TakeDamage(int damage, Material hitMaterial)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        //m_Animator.SetInteger("HP_Ratio", (int)CurrentHp/MaxHp*100);
        if (CurrentHp <= 0)
        {
            if (m_NavMeshMovement != null)
                m_NavMeshMovement.enabled = false;

            // Destroy(gameObject);
            return;
        }

        if (hitMaterial != null)
        {
            if (m_HitFlash != null)
                StopCoroutine(m_HitFlash);

            m_HitFlash = StartCoroutine(FlashMaterial(hitMaterial));
        }
    }

    void CacheMaterials()
    {
        m_Renderers = GetComponentsInChildren<Renderer>();
        m_OriginalMaterials = new Material[m_Renderers.Length][];

        for (var i = 0; i < m_Renderers.Length; i++)
            m_OriginalMaterials[i] = m_Renderers[i].sharedMaterials;
    }

    IEnumerator FlashMaterial(Material hitMaterial)
    {
        for (var i = 0; i < m_Renderers.Length; i++)
        {
            var materials = new Material[m_OriginalMaterials[i].Length];
            for (var j = 0; j < materials.Length; j++)
                materials[j] = hitMaterial;

            m_Renderers[i].sharedMaterials = materials;
        }

        yield return new WaitForSeconds(0.2f);

        for (var i = 0; i < m_Renderers.Length; i++)
            m_Renderers[i].sharedMaterials = m_OriginalMaterials[i];

        m_HitFlash = null;
    }

    void OnDestroy()
    {
        if (MonsterManager.HasInstance)
            MonsterManager.Instance.Unregister(this);
    }
}
