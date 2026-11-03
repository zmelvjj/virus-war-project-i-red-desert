using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamagePopup : MonoBehaviour
{
    [SerializeField] UnityEngine.Canvas worldCanvas;
    [SerializeField] TMPro.TMP_Text damageText;

    DamagePopupManager m_Owner;
    Camera m_Camera;
    Vector3 m_StartPosition;
    Color m_TextColor;
    float m_Lifetime;
    float m_MoveDistance;
    float m_FadeStartRatio;
    float m_Elapsed;
    bool m_IsPlaying;

    public void Initialize(DamagePopupManager owner)
    {
        m_Owner = owner;
    }

    public void Play(
        int damage,
        bool isCritical,
        Vector3 startPosition,
        Camera gameCamera,
        Color color,
        float fontSize,
        float lifetime,
        float moveDistance,
        float fadeStartRatio)
    {
        m_Camera = gameCamera;
        m_StartPosition = startPosition;
        m_TextColor = color;
        m_TextColor.a = 1f;
        m_Lifetime = lifetime;
        m_MoveDistance = moveDistance;
        m_FadeStartRatio = fadeStartRatio;
        m_Elapsed = 0f;
        m_IsPlaying = true;

        transform.position = startPosition;
        damageText.text = FormatDamage(damage, isCritical);
        damageText.fontSize = fontSize;
        damageText.color = m_TextColor;
        worldCanvas.enabled = true;
        gameObject.SetActive(true);
        FaceCamera();
    }

    public void PrepareForPool()
    {
        m_IsPlaying = false;
        m_Elapsed = 0f;
        m_Camera = null;
        transform.localPosition = Vector3.zero;
        damageText.text = string.Empty;
        damageText.color = new Color(1f, 1f, 1f, 0f);
        worldCanvas.enabled = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!m_IsPlaying)
            return;

        m_Elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(m_Elapsed / m_Lifetime);
        transform.position = m_StartPosition
            + Vector3.up * (m_MoveDistance * progress);

        var alpha = progress <= m_FadeStartRatio
            ? 1f
            : 1f - Mathf.InverseLerp(m_FadeStartRatio, 1f, progress);
        var color = m_TextColor;
        color.a = alpha;
        damageText.color = color;

        if (progress >= 1f)
            m_Owner.Release(this);
    }

    void LateUpdate()
    {
        if (m_IsPlaying)
            FaceCamera();
    }

    void FaceCamera()
    {
        if (m_Camera != null)
            transform.rotation = m_Camera.transform.rotation;
    }

    static string FormatDamage(int damage, bool isCritical)
    {
        return isCritical ? $"{damage}!" : damage.ToString();
    }
}
