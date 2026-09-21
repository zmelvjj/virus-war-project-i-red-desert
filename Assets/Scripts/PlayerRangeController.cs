using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRangeController : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;
    public Transform range;
    public MouthOpenTracker mouthOpenTracker;

    [Header("Range Width")]
    [Min(0.01f)] public float closedWidthMultiplier = 0.5f;
    [Min(0.01f)] public float openWidthMultiplier = 2f;

    Vector3 m_InitialRangeScale;
    float m_RangeArea;
    float fast_mouthOpen = 0f;

    void Awake()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        if (range == null || mouthOpenTracker == null)
        {
            Debug.LogError("PlayerRangeController requires Range and MouthOpenTracker references.", this);
            enabled = false;
            return;
        }

        m_InitialRangeScale = range.localScale;
        m_RangeArea = m_InitialRangeScale.x * m_InitialRangeScale.z;
    }

    void Update()
    {
        RotateTowardMouse();
        UpdateRangeScale();
    }

    void RotateTowardMouse()
    {
        if (aimCamera == null || Mouse.current == null)
            return;

        var mousePosition = Mouse.current.position.ReadValue();
        if (!aimCamera.pixelRect.Contains(mousePosition))
            return;

        var ray = aimCamera.ScreenPointToRay(mousePosition);
        var plane = new Plane(Vector3.up, transform.position);
        if (!plane.Raycast(ray, out var distance))
            return;

        var direction = ray.GetPoint(distance) - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    void UpdateRangeScale()
    {
        var mouthOpen = mouthOpenTracker.HasFace ? mouthOpenTracker.MouthOpen : 0f;
        if (Mathf.Abs(mouthOpen - fast_mouthOpen) > 0.1f)
            fast_mouthOpen = Mathf.Lerp(fast_mouthOpen, mouthOpen, 0.7f);
        
        var widthMultiplier = Mathf.Lerp(closedWidthMultiplier, openWidthMultiplier, Mathf.Clamp01(mouthOpen));
        var width = Mathf.Max(0.001f, m_InitialRangeScale.x * widthMultiplier);
        range.localScale = new Vector3(width, m_InitialRangeScale.y, m_RangeArea/width);
        range.localPosition = new Vector3(0f, range.localPosition.y, m_RangeArea/width * 0.5f);
    }
}
