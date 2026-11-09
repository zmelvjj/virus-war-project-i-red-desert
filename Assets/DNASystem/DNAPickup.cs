using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class DNAPickup : MonoBehaviour
{
    enum PickupState
    {
        Inactive,
        Scatter,
        Magnet,
        Collected
    }

    [SerializeField, Min(0f)] float magnetDelay = 0.5f;
    [FormerlySerializedAs("maxMagnetSpeed")]
    [SerializeField, Min(0.01f)] float magnetSpeed = 8f;

    Rigidbody m_Rigidbody;
    DNADropSystem m_Owner;
    Transform m_Target;
    PickupState m_State;
    float m_StateElapsed;
    int m_DNAValue;
    float m_CollectRadius;

    public void Initialize(DNADropSystem owner)
    {
        m_Owner = owner;
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    public void Spawn(
        Vector3 position,
        Transform target,
        int dnaValue,
        float collectRadius,
        Vector3 impulse)
    {
        m_Target = target;
        m_DNAValue = dnaValue;
        m_CollectRadius = collectRadius;
        m_State = PickupState.Scatter;
        m_StateElapsed = 0f;

        transform.SetPositionAndRotation(position, Random.rotationUniform);
        gameObject.SetActive(true);
        m_Rigidbody.isKinematic = false;
        m_Rigidbody.useGravity = true;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Rigidbody.AddForce(impulse, ForceMode.Impulse);
    }

    public void PrepareForPool()
    {
        m_State = PickupState.Inactive;
        m_StateElapsed = 0f;
        m_DNAValue = 0;
        m_CollectRadius = 0f;
        m_Target = null;

        if (m_Rigidbody != null)
        {
            if (!m_Rigidbody.isKinematic)
            {
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            m_Rigidbody.useGravity = false;
            m_Rigidbody.isKinematic = true;
        }

        gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        if (m_State == PickupState.Inactive || m_State == PickupState.Collected)
            return;

        m_StateElapsed += Time.fixedDeltaTime;
        if (m_State == PickupState.Scatter)
        {
            if (m_StateElapsed < magnetDelay)
                return;

            m_State = PickupState.Magnet;
            m_StateElapsed = 0f;
            m_Rigidbody.linearVelocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.useGravity = false;
            m_Rigidbody.isKinematic = true;
        }

        UpdateMagnet();
    }

    void UpdateMagnet()
    {
        if (m_Target == null)
            return;

        var toTarget = m_Target.position - m_Rigidbody.position;
        if (toTarget.sqrMagnitude <= m_CollectRadius * m_CollectRadius)
        {
            Collect();
            return;
        }

        var nextPosition = Vector3.MoveTowards(
            m_Rigidbody.position,
            m_Target.position,
            magnetSpeed * Time.fixedDeltaTime / (Vector3.Distance(m_Rigidbody.position, m_Target.position) / 3f + 0.4f));
        m_Rigidbody.MovePosition(nextPosition);
    }

    internal void CollectImmediately()
    {
        if (m_State == PickupState.Inactive || m_State == PickupState.Collected)
            return;

        m_Rigidbody.position = m_Target.position;
        Collect();
    }

    void Collect()
    {
        m_State = PickupState.Collected;
        DNAWallet.Instance.AddDNA(m_DNAValue);
        m_Owner.Release(this);
    }
}
