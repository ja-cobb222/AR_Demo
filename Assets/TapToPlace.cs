using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlace : MonoBehaviour
{
    [Header("References")]
    [SerializeField] ARRaycastManager arRaycastManager;
    [SerializeField] Camera arCamera;
    [SerializeField] GameObject placeablePrefab;

    [Header("Editor settings")]
    [SerializeField] LayerMask editorClickableLayers; // set to EditorGround + Placeable
    [SerializeField] float placeableHeight = 0.1f;    // height of your cube in meters
    [SerializeField] float epsilonLift = 0.001f;      // tiny lift to avoid z-fighting

    static readonly List<ARRaycastHit> s_Hits = new();

    void Reset()
    {
        if (!arRaycastManager) arRaycastManager = GetComponent<ARRaycastManager>();
        if (!arCamera) arCamera = Camera.main;
    }

    void Update()
    {
#if UNITY_EDITOR
        // OPTION B: require Shift + Left Click in the Editor
        if (Input.GetMouseButtonDown(0) &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            var ray = arCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, 100f, editorClickableLayers))
            {
                var p = hit.point;

                // If we clicked a cube, place the new one on top of it
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Placeable"))
                {
                    var topY = hit.collider.bounds.max.y;
                    p.y = topY + placeableHeight * 0.5f + epsilonLift;
                }
                else
                {
                    // Ground: center the cube on the surface with tiny lift
                    p += Vector3.up * (placeableHeight * 0.5f + epsilonLift);
                }

                Instantiate(placeablePrefab, p, Quaternion.identity);
            }
        }
#else
        if (Input.touchCount == 0) return;
        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (arRaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            var pose = s_Hits[0].pose;
            // On device, AR raycasts return the plane surface; lift by half the cube height
            var p = pose.position + Vector3.up * (placeableHeight * 0.5f + epsilonLift);
            Instantiate(placeablePrefab, p, pose.rotation);
        }
#endif
    }
}