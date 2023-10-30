using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverMenu : MonoBehaviour
{
    private bool _hasChosen;

    public void Replay()
    {
        if(_hasChosen)
        {
            return;
        }

        _hasChosen = true;

        UnityEngine.SceneManagement.SceneManager.LoadScene("02_Main");
    }

    public void Quit()
    {
        if(_hasChosen)
        {
            return;
        }

        _hasChosen = true;

        Application.Quit();
    }
}
