using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class GrabDrag : MonoBehaviour
{
    [Header("Input source")]
    [Tooltip("Assign a component that implements IHandInput (e.g., MouseHandInput).")]
    [SerializeField] MonoBehaviour handInputBehaviour; // must implement IHandInput
    IHandInput handInput;

    [Header("Common")]
    [SerializeField] Camera arCamera;
    [SerializeField] LayerMask grabbableLayers; // set to Placeable
    [SerializeField] float followStrength = 20f;

    [Header("Throw tuning")]
    [SerializeField, Tooltip("Pixels → world units scale for throw velocity")]
    float pixelToWorld = 0.002f;
    [SerializeField, Tooltip("Extra forward push along camera forward on release")]
    float forwardBoost = 1.0f;
    [SerializeField, Tooltip("Small upward lift on release")]
    float upwardBoost = 0.0f;
    [SerializeField, Tooltip("Clamp the final throw speed (m/s)")]
    float maxThrowSpeed = 12f;

    [Header("Rotate / Scale while holding (Editor & Device)")]
    [SerializeField, Tooltip("Yaw speed when right-dragging (deg/sec)")]
    float rotateSpeed = 120f;
    [SerializeField, Tooltip("Roll speed with Q/E (deg/sec)")]
    float rollSpeed = 120f;
    [SerializeField] bool  allowScaling = true;
    [SerializeField, Tooltip("Min/Max scale relative to original size")]
    Vector2 scaleRange = new Vector2(0.5f, 2.0f);
    [SerializeField, Tooltip("Scale change per mouse wheel tick (fraction)")]
    float scalePerScroll = 0.08f;

    [Header("Device (AR)")]
    [SerializeField] ARRaycastManager raycaster;

#if UNITY_EDITOR
    [Header("Editor fallback (no XR Simulation)")]
    [Tooltip("Layer(s) used by your EditorGround plane with a collider")]
    [SerializeField] LayerMask editorGroundMask;
    [Tooltip("If you have no EditorGround, drag along a flat Y=0 plane")]
    [SerializeField] bool useFlatY0WhenNoGround = true;
#endif

    Rigidbody grabbed;
    Vector3 grabOffsetLocal;
    float grabbedBaseScale = 1f;
    readonly List<ARRaycastHit> hits = new();

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
#if !UNITY_EDITOR
        if (!raycaster) raycaster = FindAnyObjectByType<ARRaycastManager>();
#endif
        // Validate/auto-find IHandInput
        if (handInputBehaviour is IHandInput hi) handInput = hi;
        else handInput = FindFirstHandInputInScene();
    }

    void Update()
    {
        if (handInput == null || !handInput.IsActive) return;

        if (handInput.JustBegan) TryGrab(handInput.PositionPx);

        if (handInput.IsGrabbing)
        {
#if UNITY_EDITOR
            MoveGrabbed_Editor(handInput.PositionPx);
#else
            MoveGrabbed_AR(handInput.PositionPx);
#endif
            UpdateHoldRotScale(); // yaw/roll/scale while holding
        }

        if (handInput.JustEnded)
        {
            // Convert 2D swipe to world velocity
            Vector2 velPx = handInput.VelocityPx;
            var cam = arCamera.transform;
            var v = cam.right * (velPx.x * pixelToWorld)
                  + cam.up    * (velPx.y * pixelToWorld)
                  + cam.forward * forwardBoost
                  + Vector3.up * upwardBoost;
            ApplyRelease(v);
        }
    }

    // ---------- Core ops ----------
    void TryGrab(Vector2 screenPos)
    {
        var ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, 30f, grabbableLayers))
        {
            grabbed = hit.rigidbody ? hit.rigidbody : hit.collider.attachedRigidbody;
            if (grabbed != null)
            {
                grabbed.useGravity = false;
                grabbed.linearVelocity = Vector3.zero;
                grabOffsetLocal = grabbed.transform.InverseTransformPoint(hit.point);

                grabbed.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                grabbed.interpolation = RigidbodyInterpolation.Interpolate;

                grabbedBaseScale = grabbed.transform.localScale.x;
            }
        }
    }

#if UNITY_EDITOR
    void MoveGrabbed_Editor(Vector2 screenPos)
    {
        if (!grabbed) return;

        var ray = arCamera.ScreenPointToRay(screenPos);

        // Prefer real EditorGround hits
        if (Physics.Raycast(ray, out var hit, 50f, editorGroundMask))
        {
            MoveToward(hit.point);
            return;
        }

        // Fallback: Y=0 plane
        if (useFlatY0WhenNoGround)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
            {
                MoveToward(ray.GetPoint(dist));
            }
        }
    }
#endif

    void MoveGrabbed_AR(Vector2 screenPos)
    {
        if (!grabbed || raycaster == null) return;
        if (raycaster.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            MoveToward(hits[0].pose.position);
        }
    }

    void MoveToward(Vector3 surfacePoint)
    {
        var worldOffset = grabbed.transform.TransformVector(grabOffsetLocal);
        var desired = surfacePoint - worldOffset;
        var to = desired - grabbed.position;
        grabbed.linearVelocity = to * followStrength;
    }

    void ApplyRelease(Vector3 worldVelocity)
    {
        if (!grabbed) return;

        if (worldVelocity.magnitude > maxThrowSpeed)
            worldVelocity = worldVelocity.normalized * maxThrowSpeed;

        grabbed.useGravity = true;
        grabbed.linearVelocity = worldVelocity; // keep throw velocity!
        grabbed = null;
    }

    // ---------- Rotate / scale while holding ----------
    void UpdateHoldRotScale()
    {
        if (!grabbed) return;

        // Right mouse drag -> yaw (Editor & mouse; trackpads also work)
        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");               // mouse delta X per frame
            float yaw = dx * rotateSpeed * Time.deltaTime;     // degrees
            var q = Quaternion.AngleAxis(yaw, Vector3.up) * grabbed.rotation;
            grabbed.MoveRotation(q);
        }

        // Q/E roll around camera forward
        float roll = 0f;
        if (Input.GetKey(KeyCode.Q)) roll += rollSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) roll -= rollSpeed * Time.deltaTime;
        if (Mathf.Abs(roll) > 0f)
        {
            var q = Quaternion.AngleAxis(roll, arCamera.transform.forward) * grabbed.rotation;
            grabbed.MoveRotation(q);
        }

        // Mouse wheel scale (uniform)
        if (allowScaling)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float s = grabbed.transform.localScale.x;
                s = Mathf.Clamp(
                    s * (1f + scroll * scalePerScroll),
                    scaleRange.x * grabbedBaseScale,
                    scaleRange.y * grabbedBaseScale
                );
                grabbed.transform.localScale = new Vector3(s, s, s);
            }
        }
    }

    // ---------- Utility ----------
    IHandInput FindFirstHandInputInScene()
    {
        // Try to locate any behaviour that implements IHandInput (incl. inactive)
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<MonoBehaviour>(true);
#endif
        foreach (var mb in all)
        {
            if (mb is IHandInput hi) return hi;
        }
        return null;
    }
}