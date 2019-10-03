﻿using AssemblyCSharp;
using com.shephertz.app42.gaming.multiplayer.client;
using com.shephertz.app42.gaming.multiplayer.client.events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Tile[] _tilesTypes;
    [SerializeField] Character[] _characterTypes;
    [SerializeField] Surface _gameBoard;
    [SerializeField] Button _restartButton;
    [SerializeField] AudioSource _gameMusic;

    [Header("Variables")]
    [SerializeField] int _charactersPerPlayer;

    #region Surface
    public Dictionary<Character,Vector2Int> _characterDictionary { get; private set; }
    public Surface GetBoard { get { return _gameBoard; } }
    #endregion

    #region Turns Management
    public Character.CharacterColors WhosTurn { get; private set; }
    public Character.CharacterColors PlayerOneColor { get; set; }
    public Character.CharacterColors PlayerTwoColor { get; set; }
    public bool GameStarted { get; private set; }
    public bool GameOver { get; private set; }
    public string UserId { get; set; }
    public bool IsSingleplayer { get; set; }

    public bool ChoosingColor { get; private set; }
    public Dictionary<string, Character.CharacterColors> _playersDictionary;
    public bool InvalidCommand { get; set; }
    private int _leftThisTurn;
    private int _playerOneLeft;
    private int _playerTwoLeft;
    #endregion

    #region Click Detectors
    public Vector2Int _whereClicked { get; private set; }
    public Character _characterClicked { get; set; }
    public Character _characterEnemyClicked { get; set; }
    #endregion

    public static GameManager Instance { get; private set; }

    protected void Awake()
    {
        Instance = this;
        ChoosingColor = true;

        _characterDictionary = new Dictionary<Character,Vector2Int>();
        _playersDictionary = new Dictionary<string, Character.CharacterColors>();

        PlayerOneColor = Character.CharacterColors.None;
        PlayerTwoColor = Character.CharacterColors.None;

        _restartButton.gameObject.SetActive(false);
        GameInit();
    }
    protected void Start()
    {
        _gameBoard.SurfaceInit(transform);
        SpawnCharacter();
        GUI.menuInstance.MoveCharacterEvent += OnPlayerMoveCharacter;
        GUI.menuInstance.OverwatchEvent += OnCharacterOverwatchingTile;
        GUI.menuInstance.AttackPressedEvent += () => _characterClicked.myAnimator.SetTrigger("isAttacking");
        StartCoroutine(PlayersChoosingColor());
        SetMusicVolume(20);
    }
    protected void Update()
    {
        if(GameStarted)
        {
            if (Input.GetKeyDown(KeyCode.E))
                WarpClient.GetInstance().stopGame();

            if (!GameOver)
            {
                if(IsMyTurn() || IsSingleplayer)
                {
                    GUI.menuInstance.GameController();
                    TurnManagement();
                }
            }
            else if (GameOver)
            {
                _restartButton.gameObject.SetActive(true);
            }
        }

    }

    private void GameInit()
    {
        WhosTurn = Character.CharacterColors.None;
        GameOver = false;
        _leftThisTurn = _charactersPerPlayer;
        _playerOneLeft = _charactersPerPlayer;
        _playerTwoLeft = _charactersPerPlayer;
    }
    private void SpawnCharacter()
    {
        for(int i = 0; i < _charactersPerPlayer * 2; i++)
        {
            var newCharacter = Instantiate(_characterTypes[i % _charactersPerPlayer]);
            if (i < _charactersPerPlayer)
            {
                newCharacter.IsPlayerOne = true;
                _gameBoard.SetCharacterOnBoard(i, 0, newCharacter);
                _characterDictionary.Add(newCharacter, new Vector2Int(i, 0));
            }
            else
            {
                newCharacter.IsPlayerOne = false;
                newCharacter.transform.rotation = Quaternion.Euler(0, 180, 0);
                _gameBoard.SetCharacterOnBoard(_gameBoard.GetWidth - (i % _charactersPerPlayer) - 1, _gameBoard.GetHeight - 1, newCharacter);
                _characterDictionary.Add(newCharacter, new Vector2Int(_gameBoard.GetWidth - (i % _charactersPerPlayer) - 1, _gameBoard.GetHeight - 1));
            }
        }
    }
    private void SingleplayerUserID()
    {
        UserId = WhosTurn == PlayerOneColor ? "PlayerOne" : "PlayerTwo";
    }
    private IEnumerator PlayersChoosingColor()
    {
        while (ChoosingColor)
        {
            if (PlayerOneColor != Character.CharacterColors.None)
                if(IsSingleplayer)
                    _playersDictionary.Add("PlayerOne", PlayerOneColor);

            if (PlayerTwoColor != Character.CharacterColors.None)
                if (IsSingleplayer)
                    _playersDictionary.Add("PlayerTwo", PlayerTwoColor);

            if (PlayerOneColor != Character.CharacterColors.None && PlayerTwoColor != Character.CharacterColors.None)
                ChoosingColor = false;

            yield return null;
        }

        foreach (var alive in _characterDictionary.ToList())
        {
            if (alive.Key.IsPlayerOne)
                alive.Key.SetColor(PlayerOneColor);
            else
                alive.Key.SetColor(PlayerTwoColor);
        }

        if(IsSingleplayer)
        {
            WhosTurn = UnityEngine.Random.value > 0.5f ? PlayerOneColor : PlayerTwoColor;
            SingleplayerUserID();
        }

        GameStarted = true;
    }
    public bool IsMyTurn()
    {
        foreach (var player in _playersDictionary)
        {
            if (player.Value == WhosTurn && player.Key == UserId && _characterClicked == null)
                return true;
            if (player.Value == WhosTurn && player.Key == UserId && _characterClicked.MyColor == WhosTurn)
                return true;
        }
        return false;
    }
    public List<Vector2Int> GetTargetsInRange()
    {
        List<Vector2Int> tmpList = new List<Vector2Int>();
        foreach (var alive in _characterDictionary.ToList())
        {
            if (alive.Key.MyColor != WhosTurn)
            {
                if(_whereClicked.x <= alive.Value.x && alive.Value.x <= (_whereClicked.x + _characterClicked.getRange) &&
                   _whereClicked.y <= alive.Value.y && alive.Value.y <= (_whereClicked.y + _characterClicked.getRange) ||
                   _whereClicked.x >= alive.Value.x && alive.Value.x >= (_whereClicked.x - _characterClicked.getRange) &&
                   _whereClicked.y >= alive.Value.y && alive.Value.y >= (_whereClicked.y - _characterClicked.getRange))
                tmpList.Add(alive.Value);
            }
        }

        return tmpList;
    }
    private void TurnManagement()
    {
        if (GUI.stateChanged)
        {
            if (_leftThisTurn > 0)
            {
                foreach (var alive in _characterDictionary.ToList())
                {
                    alive.Key.UpdateStatus();
                    if (alive.Key.isDead)
                    {
                        if (!CheckGameIsOver(alive.Key.IsPlayerOne))
                        {
                            _characterDictionary.Remove(alive.Key);
                            Destroy(alive.Key.gameObject);
                        }
                        else
                        {
                            WarpClient.GetInstance().stopGame();
                            return;
                        }
                    }
                }

                _leftThisTurn--;
                Debug.Log("left - " + _leftThisTurn);
            }

            if (_leftThisTurn <= 0)
            {

                _leftThisTurn = WhosTurn == PlayerTwoColor ? _playerOneLeft : _playerTwoLeft;
                if (IsSingleplayer)
                {
                    WhosTurn = WhosTurn == PlayerTwoColor ? PlayerOneColor : PlayerTwoColor;
                    SingleplayerUserID();
                }
                else
                {
                    if (IsMyTurn())
                        SendingJSONToServer();
                }

                foreach (var alive in _characterDictionary.ToList())
                {
                    if (alive.Key.MyColor == WhosTurn || alive.Key.MyColor != WhosTurn)
                        Cursor.cursorInstance.MoveCursor(alive.Value.x, alive.Value.y);

                    alive.Key.ResetState();
                }

            }
        }

        GUI.stateChanged = false;
    } 
    private bool CheckGameIsOver(bool player)
    {
        if (player)
            _playerOneLeft--;
        else
            _playerTwoLeft--;

        if (_playerTwoLeft <= 0 || _playerOneLeft <= 0)
            return GameOver = true;

        return false;
    }
    public void RestartGame()
    {
        GameInit();
        if(_characterDictionary != null)
        {
            foreach (var alive in _characterDictionary.ToList())
                Destroy(alive.Key.gameObject);
        }

        _characterDictionary = new Dictionary<Character,Vector2Int>();
        SpawnCharacter();
        _restartButton.gameObject.SetActive(false); 
    }
    public bool IsCharacterHere()
    {
        _whereClicked = Cursor.cursorInstance.GetCoords;
        foreach (var alive in _characterDictionary)
        {
            if (alive.Value == _whereClicked)
                return true;
        }
        return false;
    }
    private void OnPlayerMoveCharacter()
    {
        _gameBoard.SetCharacterOnBoard(_whereClicked.x, _whereClicked.y, _characterClicked);

        foreach (var alive in _characterDictionary.ToList())
        {
            if (alive.Key.getCharacterID == _characterClicked.getCharacterID)
            {
                _characterDictionary[alive.Key] = new Vector2Int(_whereClicked.x, _whereClicked.y);
            }
        }
    }
    private void OnCharacterOverwatchingTile()
    {
        _gameBoard.SetTextureOnTiles(_whereClicked.x, _whereClicked.y, _tilesTypes[3]);

        //foreach (KeyValuePair<Character, Vector2Int> alive in _characterDictionary)
        //{
        //    if (alive.Value.getCharacterID == _characterClicked.getCharacterID)
        //    {
        //        _characterDictionary.Remove(alive.Key);
        //        _characterDictionary.Add(new Vector2Int(_whereClicked.x, _whereClicked.y), _characterClicked);
        //    }
        //}
    }
    public void SetMusicVolume(int volume)
    {
        _gameMusic.volume = volume / 100f;
    }
    public void EndTurn()
    {
        GUI.stateChanged = true;

        if (GameStarted)
            _leftThisTurn = 0;
    }

    #region Multiplayer

    private void OnGameStarted(string _Sender, string _RoomId, string _NextTurn)
    {
        GameStarted = true;
        WhosTurn = PlayerOneColor;
    }

    private void SendingJSONToServer()
    {
        Dictionary<string, object> _toSend = new Dictionary<string, object>();
        foreach (KeyValuePair<Character, Vector2Int> alive in _characterDictionary)
        {
            _toSend.Add(alive.Key.getCharacterID.ToString(), alive.Value.ToString());
        }

        string _send = MiniJSON.Json.Serialize(_toSend);
        WarpClient.GetInstance().sendMove(_send);
    }

    private void OnMoveCompleted(MoveEvent _Move)
    {
        if (_Move.getSender() != UserId)
        {
            Dictionary<string, object> _characterDictionaryTmp = (Dictionary<string,object>)MiniJSON.Json.Deserialize(_Move.getMoveData());
            if (_characterDictionaryTmp != null)
            {
                foreach (var check in _characterDictionary)
                {
                    var isExist = false;
                    foreach (var alive in _characterDictionaryTmp)
                    {
                        if (alive.Key == check.Key.getCharacterID.ToString())
                            isExist = true;
                    }
                    if (!isExist)
                        check.Key.isDead = true;
                }
                //_characterDictionary = _characterDictionaryTmp;
            }
            else
            {
                Debug.Log("Data not received");
            }
        }

        WhosTurn = _playersDictionary[_Move.getNextTurn()];
        GUI.stateChanged = true;
    }

    private void OnGameStopped(string _Sender, string _RoomId)
    {
        Debug.Log("Game Over");
    }

    private void OnEnable()
    {
        Listener.OnGameStarted += OnGameStarted;
        Listener.OnMoveCompleted += OnMoveCompleted;
        Listener.OnGameStopped += OnGameStopped;
    }

    private void OnDisable()
    {
        Listener.OnGameStarted -= OnGameStarted;
        Listener.OnMoveCompleted -= OnMoveCompleted;
        Listener.OnGameStopped -= OnGameStopped;
    }
    #endregion
}

