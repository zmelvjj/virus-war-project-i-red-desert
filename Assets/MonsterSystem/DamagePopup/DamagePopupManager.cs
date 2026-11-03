using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamagePopupManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] DamagePopup popupPrefab;
    [SerializeField] Camera gameCamera;

    [Header("Pool")]
    [SerializeField, Min(0)] int initialPoolSize = 16;

    [Header("Style")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color criticalColor = Color.red;
    [SerializeField, Min(1f)] float normalFontSize = 32f;
    [SerializeField, Min(1f)] float criticalFontSize = 44f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] float lifetime = 1f;
    [SerializeField, Min(0f)] float moveDistance = 1.5f;
    [SerializeField, Range(0f, 1f)] float fadeStartRatio = 0.35f;
    [SerializeField, Min(0f)] float horizontalRandomOffset = 0.2f;

    readonly Queue<DamagePopup> m_Pool = new();

    public static DamagePopupManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (gameCamera == null)
            gameCamera = Camera.main;

        for (var i = 0; i < initialPoolSize; i++)
            m_Pool.Enqueue(CreateInstance());
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowDamage(DamageResult result)
    {
        if (result.Target == null)
            return;

        var popup = m_Pool.Count > 0
            ? m_Pool.Dequeue()
            : CreateInstance();
        var horizontalDirection = gameCamera != null
            ? gameCamera.transform.right
            : Vector3.right;
        var position = result.Target.DamagePopupPosition
            + horizontalDirection * Random.Range(
                -horizontalRandomOffset,
                horizontalRandomOffset);

        popup.Play(
            result.FinalDamage,
            result.IsCritical,
            position,
            gameCamera,
            result.IsCritical ? criticalColor : normalColor,
            result.IsCritical ? criticalFontSize : normalFontSize,
            lifetime,
            moveDistance,
            fadeStartRatio);
    }

    internal void Release(DamagePopup popup)
    {
        popup.PrepareForPool();
        m_Pool.Enqueue(popup);
    }

    DamagePopup CreateInstance()
    {
        var popup = Instantiate(popupPrefab, transform);
        popup.name = popupPrefab.name;
        
        
        popup.Initialize(this);
        popup.PrepareForPool();
        return popup;
    }
}
