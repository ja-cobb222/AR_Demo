using System.Collections.Generic;
using UnityEngine;

public struct Detection {
  public string label;   // e.g., "chair"
  public float score;    // 0..1
  public Rect box01;     // x,y,w,h in 0..1 space (normalized)
}

public interface IObjectRecognizer {
  IReadOnlyList<Detection> Detect(Texture srcTexture);
}