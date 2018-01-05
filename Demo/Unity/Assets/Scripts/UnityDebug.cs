using System.Collections;

using UnityEngine;

using Volatile;

internal class UnityDebugLogger : IVoltDebugLogger
{
  public void LogError(object message)
  {
    Debug.LogError(message);
  }

  public void LogWarning(object message)
  {
    Debug.LogWarning(message);
  }

  public void LogMessage(object message)
  {
    Debug.Log(message);
  }
}

public class UnityDebug : MonoBehaviour 
{
  void Awake()
  {
    VoltDebug.Logger = new UnityDebugLogger();
  }
}
