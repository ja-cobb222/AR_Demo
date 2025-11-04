using System.Collections.Generic;
using UnityEngine;

public class StubRecognizer : IObjectRecognizer
{
  readonly List<Detection> _out = new();
  float t;

  public IReadOnlyList<Detection> Detect(Texture srcTexture)
  {
    _out.Clear();
#if UNITY_EDITOR
    // simple moving box so we can test the overlay
    t += Time.deltaTime * 0.35f;
    var x = 0.1f + 0.5f * Mathf.Abs(Mathf.Sin(t));
    _out.Add(new Detection {
      label = "demo",
      score = 0.9f,
      box01 = new Rect(x, 0.3f, 0.25f, 0.25f)
    });
#endif
    return _out;
  }
}