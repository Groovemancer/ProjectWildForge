using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; protected set; }

    public SoundController SoundController { get; private set; }

    // If true, a modal dialog box is open, so normal inputs should be ignored.
    public bool IsModal { get; set; }

    public bool IsPaused
    {
        get
        {
            return TimeManager.Instance.IsPaused || IsModal;
        }

        set
        {
            TimeManager.Instance.IsPaused = value;
        }
    }

    // Path to the saves folder.
    public string FileSaveBasePath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "Saves");
    }

    // Each time a scene is loaded.
    private void Awake()
    {
        EnableDontDestroyOnLoad();

        SoundController = new SoundController();

        IsModal = false;
        IsPaused = true;

        KeyboardManager.Instance.RegisterInputAction("Pause", KeyboardMappedInputType.KeyUp, TogglePause);
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    // Only on first time a scene is loaded.
    private void Start()
    {
        LocaleData.LoadData();
    }

    private void Update()
    {
        TimeManager.Instance.Update(Time.deltaTime);
    }

    // Game Controller will persist between scenes.
    private void EnableDontDestroyOnLoad()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        // Ensure that the audiomanager's resources get released properly on quit. This may only be a problem in the editor.
        //AudioManager.Destroy();
    }
}
