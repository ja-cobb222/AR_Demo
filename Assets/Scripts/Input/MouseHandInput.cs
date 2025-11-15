using UnityEngine;

public class MouseHandInput : MonoBehaviour, IHandInput
{
    [SerializeField] int velocitySamples = 6;

    public bool IsActive { get; private set; }
    public Vector2 PositionPx { get; private set; }
    public Vector2 VelocityPx { get; private set; }
    public bool IsGrabbing { get; private set; }
    public bool JustBegan { get; private set; }
    public bool JustEnded { get; private set; }

    Vector2[] bufPos;
    float[]   bufTime;
    int idx;

    void Awake()
    {
        bufPos = new Vector2[Mathf.Max(2, velocitySamples)];
        bufTime = new float[bufPos.Length];
    }

    void Update()
    {
        PositionPx = Input.mousePosition;
        IsActive = true;

        bool down = Input.GetMouseButton(0);
        JustBegan =  down && !IsGrabbing;
        JustEnded = !down &&  IsGrabbing;
        IsGrabbing = down;

        // velocity
        bufPos[idx] = PositionPx;
        bufTime[idx] = Time.unscaledTime;
        idx = (idx + 1) % bufPos.Length;

        int newest = (idx - 1 + bufPos.Length) % bufPos.Length;
        int oldest = (idx + 1) % bufPos.Length;
        float dt = Mathf.Max(0.0001f, bufTime[newest] - bufTime[oldest]);
        VelocityPx = (bufPos[newest] - bufPos[oldest]) / dt;
    }
}