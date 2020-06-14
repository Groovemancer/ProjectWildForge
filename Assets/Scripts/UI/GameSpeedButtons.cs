using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSpeedButtons : MonoBehaviour
{
    List<Button> buttons = new List<Button>();
    List<GameSpeedState> gameSpeedStates = new List<GameSpeedState>();

    GameSpeed prevGameSpeed;
    GameSpeed currentGameSpeed;

    // Use this for initialization
    void Start()
    {
        buttons.AddRange(GetComponentsInChildren<Button>());
        gameSpeedStates.AddRange(GetComponentsInChildren<GameSpeedState>());
        prevGameSpeed = GameSpeed.Normal;
        currentGameSpeed = GameSpeed.Pause;
        SetGameSpeed(currentGameSpeed);

        KeyboardManager.Instance.RegisterInputAction("SetSpeed1", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.Normal));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed2", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.Fast));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed3", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.VeryFast));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed4", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.SuperFast));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed5", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.SuperDuperFast));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed6", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.MegaFast));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed7", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.UberFast));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed8", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.UltraFast));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed9", KeyboardMappedInputType.KeyUp, () => SetGameSpeed(GameSpeed.OmegaFast));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentGameSpeed != GameSpeed.Pause)
            {
                prevGameSpeed = currentGameSpeed;
                SetGameSpeed(GameSpeed.Pause);
            }
            else
            {
                SetGameSpeed(prevGameSpeed);
            }
        }
    }

    public void SetGameSpeed(GameSpeedState gameSpeedState)
    {
        switch (gameSpeedState.gameSpeed)
        {
            case GameSpeed.Pause:
                GameController.Instance.TogglePause();
                break;
            case GameSpeed.Normal:
                TimeManager.Instance.SetTimeScalePosition(1);
                break;
            case GameSpeed.Fast:
                TimeManager.Instance.SetTimeScalePosition(2);
                break;
            case GameSpeed.VeryFast:
                TimeManager.Instance.SetTimeScalePosition(3);
                break;
            case GameSpeed.SuperFast:
                TimeManager.Instance.SetTimeScalePosition(4);
                break;
            case GameSpeed.SuperDuperFast:
                TimeManager.Instance.SetTimeScalePosition(5);
                break;
            case GameSpeed.MegaFast:
                TimeManager.Instance.SetTimeScalePosition(6);
                break;
            case GameSpeed.UberFast:
                TimeManager.Instance.SetTimeScalePosition(7);
                break;
            case GameSpeed.UltraFast:
                TimeManager.Instance.SetTimeScalePosition(8);
                break;
            case GameSpeed.OmegaFast:
                TimeManager.Instance.SetTimeScalePosition(9);
                break;
        }

        SetGameSpeed(gameSpeedState.gameSpeed);
    }

    private void SetGameSpeed(GameSpeed gameSpeed)
    {
        currentGameSpeed = gameSpeed;
        ToggleButtons(currentGameSpeed);

        // FIXME Update UI
        /*
        switch (currentGameSpeed)
        {
            case GameSpeed.Pause:
                GameController.Instance.TogglePause();
                break;
            case GameSpeed.Normal:
                TimeManager.Instance.SetTimeScalePosition(1);
                break;
            case GameSpeed.Fast:
                TimeManager.Instance.SetTimeScalePosition(2);
                break;
            case GameSpeed.VeryFast:
                TimeManager.Instance.SetTimeScalePosition(3);
                break;
            case GameSpeed.SuperFast:
                TimeManager.Instance.SetTimeScalePosition(4);
                break;
        }
        */
        //WorldController.Instance.SetGameSpeed(currentGameSpeed);
    }

    private void ToggleButtons(GameSpeed gameSpeed)
    {
        foreach (Button button in buttons)
        {
            button.interactable = button.GetComponent<GameSpeedState>().gameSpeed != gameSpeed;
        }
    }
}
