using System.Collections.Generic;
using UnityEngine;

public class DetectionOverlay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] RectTransform canvasRT;   // set to this Canvas' RectTransform
    [SerializeField] RectTransform boxPrefab;  // set to Box.prefab (RectTransform)

    IObjectRecognizer recognizer;
    readonly List<RectTransform> pool = new();

    // Placeholder texture for now (we'll feed camera later)
    Texture _srcTexture;

    void Awake()
    {
        if (!canvasRT) canvasRT = GetComponent<RectTransform>();
        recognizer = new StubRecognizer(); // swap to TFLiteRecognizer later
        _srcTexture = Texture2D.blackTexture;
    }

    void Update()
    {
        var dets = recognizer.Detect(_srcTexture);
        EnsurePool(dets.Count);

        // Canvas pixel size
        var size = canvasRT.rect.size;

        int i = 0;
        foreach (var d in dets)
        {
            var rt = pool[i++];
            rt.gameObject.SetActive(true);

            // Convert normalized 0..1 rect to pixel rect on the canvas
            float pxX = d.box01.x * size.x;
            float pxY = d.box01.y * size.y;
            float pxW = d.box01.width  * size.x;
            float pxH = d.box01.height * size.y;

            // Anchor to bottom-left to avoid any center-offset math
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot     = new Vector2(0f, 0f);

            // Place and size directly in pixels
            rt.anchoredPosition = new Vector2(pxX, pxY);
            rt.sizeDelta        = new Vector2(pxW, pxH);
            rt.localScale       = Vector3.one;
        }

        // Hide any unused pooled boxes
        for (; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    void EnsurePool(int n)
    {
        while (pool.Count < n)
        {
            var inst = Instantiate(boxPrefab, canvasRT);
            inst.localScale = Vector3.one;
            inst.gameObject.SetActive(false);
            pool.Add(inst);
        }
    }
}