using UnityEngine;

public class SceneHotkeys : MonoBehaviour
{
    [Tooltip("Layer name used by spawned proxies (e.g., Placeable)")]
    public string placeableLayerName = "Placeable";

    [Tooltip("Optional: a component that has an 'editorAutoSpawn' bool (like SpawnFromDetections)")]
    public Behaviour spawnerBehaviour;
    public string autoSpawnFieldName = "editorAutoSpawn";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearPlaceables();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleAutoSpawn();
        }
    }

    void ClearPlaceables()
    {
        int layer = LayerMask.NameToLayer(placeableLayerName);
        var all = FindObjectsOfType<Rigidbody>();
        int count = 0;
        foreach (var rb in all)
        {
            if (rb.gameObject.layer == layer)
            {
                Destroy(rb.gameObject);
                count++;
            }
        }
        Debug.Log($"[SceneHotkeys] Cleared {count} placeables (R).");
    }

    void ToggleAutoSpawn()
    {
        if (!spawnerBehaviour) return;
        var t = spawnerBehaviour.GetType();
        var field = t.GetField(autoSpawnFieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            bool val = (bool)field.GetValue(spawnerBehaviour);
            field.SetValue(spawnerBehaviour, !val);
            Debug.Log($"[SceneHotkeys] editorAutoSpawn = {!val} (T).");
        }
    }
}