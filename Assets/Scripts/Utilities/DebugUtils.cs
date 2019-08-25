using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class DebugUtils : MonoBehaviour
{
    public Canvas canvasObject;
    public GameObject ErrorScreenObject;
    public GameObject ErrorTextObject;
    public GameObject ErrorCountObject;
    public bool Visible = false;

    public static Queue<string> ErrorText = new Queue<string>();
    private bool initialized = false;
    public int Count = 0;

    private static DebugUtils s_Instance = null;   // Our instance to allow this script to be called without a direct connection.
    public static DebugUtils instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType(typeof(DebugUtils)) as DebugUtils;
                if (s_Instance == null)
                {
                    GameObject console = new GameObject();
                    console.AddComponent<DebugUtils>();
                    console.name = "ErrorScreenController";
                    s_Instance = FindObjectOfType(typeof(DebugUtils)) as DebugUtils;
                    DebugUtils.instance.Initialize();
                }
            }

            return s_Instance;
        }
    }

    void Awake()
    {
        s_Instance = this;
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
            return;

        canvasObject = GameObject.FindObjectOfType<Canvas>();
        GameObject bgObj = (GameObject)Resources.Load("Prefabs/ErrorScreen", typeof(GameObject));
        ErrorScreenObject = Instantiate<GameObject>(bgObj);
        ErrorScreenObject.transform.SetParent(canvasObject.transform, false);
        ErrorScreenObject.SetActive(true);

        GameObject textObj = (GameObject)Resources.Load("Prefabs/ErrorText", typeof(GameObject));
        ErrorTextObject = Instantiate<GameObject>(textObj);
        ErrorTextObject.transform.SetParent(canvasObject.transform, false);
        ErrorTextObject.SetActive(true);

        GameObject countObj = (GameObject)Resources.Load("Prefabs/ErrorCount", typeof(GameObject));
        ErrorCountObject = Instantiate<GameObject>(countObj);
        ErrorCountObject.transform.SetParent(canvasObject.transform, false);
        ErrorCountObject.SetActive(true);

        initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (ErrorText.Count > 0 && !Visible)
        {
            SetVisible(true);
            UpdateText();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HideError();
        }
    }

    public void SetVisible(bool visible)
    {
        ErrorScreenObject.SetActive(visible);
        ErrorTextObject.SetActive(visible);
        ErrorCountObject.SetActive(visible);
        Visible = visible;
    }

    public void UpdateText()
    {
        ErrorTextObject.GetComponent<Text>().text = ErrorText.Peek();
        int current = ErrorText.Count;
        ErrorCountObject.GetComponent<Text>().text = string.Format("({0}/{1})", current, Count);
    }

    public void AddMessage(string message)
    {
        ErrorText.Enqueue(message);
        Count += 1;
        UpdateText();
    }

    public static bool IsVisible
    {
        get
        {
            if (s_Instance == null)
                return false;
            else
                return DebugUtils.instance.Visible;
        }
    }

    public static void DisplayError(string message, bool addStackTrace = true)
    {
        if (!Application.isPlaying)
            return;

        if (addStackTrace)
        {
            StackTrace stackTrace = new StackTrace();
            message += "\n" + stackTrace.ToString();
        }
        DebugUtils.instance.AddMessage(message);
    }

    public static void HideError()
    {
        if (!IsVisible)
            return;

        ErrorText.Dequeue();

        if (ErrorText.Count > 0)
        {
            DebugUtils.instance.UpdateText();
            return;
        }

        DebugUtils.instance.SetVisible(false);
        DebugUtils.instance.Count = 0;
    }

    #region Unity Debug Wrapper
    public static void Log(object message)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.Log(message);
        }
    }

    public static void Log(object message, Object context)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.Log(message, context);
        }
    }

    public static void LogChannel(string channel, object message)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.Log(string.Format("[{0}]", channel) + message);
        }
    }

    public static void LogChannel(string channel, object message, Object context)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.Log(string.Format("[{0}]", channel) + message, context);
        }
    }

    public static void LogException(System.Exception exception)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogException(exception);
        }
    }

    public static void LogException(System.Exception exception, Object context)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogException(exception, context);
        }
    }

    public static void LogWarning(object message)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogWarning(message);
        }
    }

    public static void LogWarning(object message, Object context)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }
    }

    public static void LogWarningChannel(string channel, object message)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogWarning(string.Format("[{0}]", channel) + message);
        }
    }

    public static void LogWarningChannel(string channel, object message, Object context)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogWarning(string.Format("[{0}]", channel) + message, context);
        }
    }

    public static void LogError(object message)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogError(message);
        }
    }

    public static void LogError(object message, Object context)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogError(message, context);
        }
    }

    public static void LogErrorChannel(string channel, object message)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogError(string.Format("[{0}]", channel) + message);
        }
    }

    public static void LogErrorChannel(string channel, object message, Object context)
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            UnityEngine.Debug.LogError(string.Format("[{0}]", channel) + message, context);
        }
    }

    public static bool IsDebugBuild()
    {
        return UnityEngine.Debug.isDebugBuild;
    }
    #endregion
}