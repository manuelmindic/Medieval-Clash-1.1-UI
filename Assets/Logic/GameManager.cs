using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameManager _gameManager;

    public GameManager(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public GameManager GetGameManager() { return _gameManager; }
}
