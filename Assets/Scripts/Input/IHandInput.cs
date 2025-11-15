public interface IHandInput
{
    // Is there an active hand pointer this frame?
    bool IsActive { get; }

    // Pixel-space position of the grasp point (e.g., pinch tip) this frame
    UnityEngine.Vector2 PositionPx { get; }

    // Pixel velocity estimate (for throws)
    UnityEngine.Vector2 VelocityPx { get; }

    // Hand “grab” state (e.g., pinch held)
    bool IsGrabbing { get; }

    // Rising/falling edges (begin/end of grab)
    bool JustBegan { get; }
    bool JustEnded { get; }
}