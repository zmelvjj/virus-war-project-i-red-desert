using UnityEngine;
using UnityEngine.UI;

public class WebcamProvider : MonoBehaviour
{
    [SerializeField] private RawImage preview;
    [SerializeField] private bool logFrameStatusEverySecond = true;
    [SerializeField] private float logIntervalSeconds = 1f;

    public WebCamTexture Webcam { get; private set; }
    public int UpdatedFrameCount { get; private set; }
    public bool DidUpdateFrameThisUpdate { get; private set; }

    float m_NextLogTime;

    private void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("WebCam을 찾을 수 없습니다.");
            return;
        }

        

        Webcam = new WebCamTexture(
            devices[0].name,
            1280,
            1280,
            30
        );

        Webcam.Play();

        if (preview != null)
            preview.texture = Webcam;

        Debug.Log($"[WebcamProvider] Started webcam '{devices[0].name}' requested=1280x1280@30.");
        
    }

    private void Update()
    {
        if (Webcam == null)
            return;

        DidUpdateFrameThisUpdate = Webcam.didUpdateThisFrame;
        if (DidUpdateFrameThisUpdate)
            UpdatedFrameCount++;

        if (preview != null && preview.texture != Webcam)
            preview.texture = Webcam;
        
        LogFrameStatusIfDue();
    }

    private void LogFrameStatusIfDue()
    {
        if (!logFrameStatusEverySecond)
            return;

        var interval = Mathf.Max(0.1f, logIntervalSeconds);
        if (Time.unscaledTime < m_NextLogTime)
            return;

        m_NextLogTime = Time.unscaledTime + interval;
        Debug.Log($"[WebcamProvider] UnityFrame={Time.frameCount}, IsPlaying={Webcam.isPlaying}, DidUpdateThisFrame={DidUpdateFrameThisUpdate}, UpdatedFrames={UpdatedFrameCount}, Size={Webcam.width}x{Webcam.height}, Rotation={Webcam.videoRotationAngle}, Mirrored={Webcam.videoVerticallyMirrored}");
    }

    private void OnDestroy()
    {
        if (Webcam != null && Webcam.isPlaying)
            Webcam.Stop();
    }
}
