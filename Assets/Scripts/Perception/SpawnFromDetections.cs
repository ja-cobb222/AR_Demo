using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnFromDetections : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] ARRaycastManager raycaster;
    [SerializeField] Camera arCamera;

    [Header("Prefabs")]
    [SerializeField] GameObject headProxyPrefab;
    [SerializeField] GameObject carProxyPrefab;

    [Header("Detection")]
    [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.6f;
    [SerializeField] float cooldownSec = 1.0f;

    IObjectRecognizer recognizer;
    Texture _srcTexture; // placeholder for now (we’ll feed camera later)

    readonly List<ARRaycastHit> hits = new();
    float lastSpawnTime;

    void Awake()
    {
        if (!raycaster) raycaster = FindAnyObjectByType<ARRaycastManager>();
        if (!arCamera)  arCamera  = Camera.main;

        recognizer  = new StubRecognizer();       // swap to TFLiteRecognizer later
        _srcTexture = Texture2D.blackTexture;     // placeholder
    }

    void Update()
    {
        // Throttle spawns
        if (Time.time - lastSpawnTime < cooldownSec) return;

        var dets = recognizer.Detect(_srcTexture);
        foreach (var d in dets)
        {
            if (d.score < scoreThreshold) continue;

            // Screen point from detection center (0..1 → pixels)
            var center01 = new Vector2(
                d.box01.x + d.box01.width  * 0.5f,
                d.box01.y + d.box01.height * 0.5f
            );
            var screenPt = new Vector2(center01.x * Screen.width, center01.y * Screen.height);

            // Raycast to AR plane (or Editor ground if you’re using the Editor raycast fallback)
            if (!raycaster || !raycaster.Raycast(screenPt, hits, TrackableType.PlaneWithinPolygon))
                continue;

            var pose = hits[0].pose;

            if (IsCar(d.label))
            {
                Spawn(carProxyPrefab, pose.position);
                lastSpawnTime = Time.time;
                break;
            }
            if (IsFace(d.label) || d.label == "person")
            {
                Spawn(headProxyPrefab, pose.position + Vector3.up * 0.05f);
                lastSpawnTime = Time.time;
                break;
            }
        }
    }

    GameObject Spawn(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
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
        label == "face" || label == "head" || label == "person"; // we’ll refine later with a face model
}