using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class EditorOnly : MonoBehaviour
{
    void OnEnable()
    {
#if !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }

}
