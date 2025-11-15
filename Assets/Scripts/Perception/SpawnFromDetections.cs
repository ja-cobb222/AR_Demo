using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnFromDetections : MonoBehaviour
{
    [Header("AR (device)")]
    [SerializeField] ARRaycastManager raycaster;
    [SerializeField] Camera arCamera;

    [Header("Prefabs")]
    [SerializeField] GameObject headProxyPrefab;
    [SerializeField] GameObject carProxyPrefab;

    [Header("Detection")]
    [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.6f;
    [SerializeField] float cooldownSec = 0.6f;

#if UNITY_EDITOR
    [Header("Editor fallback (no XR Simulation required)")]
    [Tooltip("Layer(s) your EditorGround plane uses (must have a Collider).")]
    [SerializeField] LayerMask editorGroundMask;
    [Tooltip("If ON, spawns alternate car/head every cooldown at a moving screen point.")]
    [SerializeField] bool editorAutoSpawn = true;
#endif

    IObjectRecognizer recognizer;
    Texture _srcTexture;

    readonly List<ARRaycastHit> hits = new();
    float lastSpawnTime;
    bool spawnCarNext = true; // alternate car/head in Editor demo

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
#if !UNITY_EDITOR
        if (!raycaster) raycaster = FindAnyObjectByType<ARRaycastManager>();
#endif
        recognizer  = new StubRecognizer();      // swap to TFLiteRecognizer later
        _srcTexture = Texture2D.blackTexture;    // replace with camera texture later
    }

    void Update()
    {
        if (Time.time - lastSpawnTime < cooldownSec) return;

#if UNITY_EDITOR
        // ----- EDITOR PATH: no AR planes; use physics raycast to EditorGround -----
        if (editorAutoSpawn)
        {
            var screenPt = GetEditorDemoScreenPoint();
            if (PhysicsRayToEditorGround(screenPt, out var pos))
            {
                SpawnAlternating(pos + Vector3.up * 0.05f);
                lastSpawnTime = Time.time;
            }
            return; // skip device logic while in Editor
        }
#endif

        // ----- DEVICE PATH: use real detections + AR raycasts -----
        var dets = recognizer.Detect(_srcTexture);
        foreach (var d in dets)
        {
            if (d.score < scoreThreshold) continue;

            // center of box (0..1) -> screen pixels
            var center01 = new Vector2(
                d.box01.x + d.box01.width  * 0.5f,
                d.box01.y + d.box01.height * 0.5f
            );
            var screenPt = new Vector2(center01.x * Screen.width, center01.y * Screen.height);

            if (raycaster != null && raycaster.Raycast(screenPt, hits, TrackableType.PlaneWithinPolygon))
            {
                var pose = hits[0].pose;

                if (IsCar(d.label))
                    Spawn(carProxyPrefab, pose.position);
                else if (IsFace(d.label) || d.label == "person")
                    Spawn(headProxyPrefab, pose.position + Vector3.up * 0.05f);
                else
                    continue;

                lastSpawnTime = Time.time;
                break;
            }
        }
    }

#if UNITY_EDITOR
    // Smoothly moving point across the screen so you see spawns without clicking
    Vector2 GetEditorDemoScreenPoint()
    {
        float t = Time.time * 0.6f;
        float x01 = 0.5f + 0.35f * Mathf.Sin(t);
        float y01 = 0.5f + 0.25f * Mathf.Cos(t * 1.2f);
        return new Vector2(x01 * Screen.width, y01 * Screen.height);
    }

    bool PhysicsRayToEditorGround(Vector2 screenPt, out Vector3 hitPos)
    {
        hitPos = default;
        if (arCamera == null) return false;

        var ray = arCamera.ScreenPointToRay(screenPt);
        if (Physics.Raycast(ray, out var hit, 30f, editorGroundMask))
        {
            hitPos = hit.point;
            return true;
        }
        return false;
    }

    void SpawnAlternating(Vector3 pos)
    {
        if (spawnCarNext) Spawn(carProxyPrefab, pos);
        else              Spawn(headProxyPrefab, pos);
        spawnCarNext = !spawnCarNext;
    }
#endif

    GameObject Spawn(GameObject prefab, Vector3 pos)
    {
        if (!prefab) return null;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        return go;
    }

    static bool IsCar(string label) =>
        label == "car" || label == "truck" || label == "bus";

    static bool IsFace(string label) =>
        label == "face" || label == "head" || label == "person";
}