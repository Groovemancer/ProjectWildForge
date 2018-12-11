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

    bool init = false;

    // Use this for initialization
    void Start()
    {
        buttons.AddRange(GetComponentsInChildren<Button>());
        gameSpeedStates.AddRange(GetComponentsInChildren<GameSpeedState>());
        prevGameSpeed = GameSpeed.Normal;
        currentGameSpeed = GameSpeed.Pause;
        SetGameSpeed(currentGameSpeed);
        init = true;
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

        foreach (GameSpeedState gameSpeedState in gameSpeedStates)
        {
            if (Input.GetKeyDown(gameSpeedState.keyCode))
            {
                prevGameSpeed = GameSpeed.Pause;
                SetGameSpeed(gameSpeedState.gameSpeed);
            }
        }
    }

    public void SetGameSpeed(GameSpeedState gameSpeedState)
    {
        SetGameSpeed(gameSpeedState.gameSpeed);
    }

    private void SetGameSpeed(GameSpeed gameSpeed)
    {
        currentGameSpeed = gameSpeed;
        ToggleButtons(currentGameSpeed);
        WorldController.Instance.SetGameSpeed(currentGameSpeed);
    }

    private void ToggleButtons(GameSpeed gameSpeed)
    {
        foreach (Button button in buttons)
        {
            button.interactable = button.GetComponent<GameSpeedState>().gameSpeed != gameSpeed;
        }
    }
}
