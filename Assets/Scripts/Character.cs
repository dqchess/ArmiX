﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
    public enum CharacterTypes { MachineGun, Sniper, Agent };
    public enum CharacterColors { Blue, Red, Black, Brown, Green, Grey, Olive, White, Yellow };

    [Header("References")]
    [SerializeField] Material[] _colorMaterials;
    [SerializeField] GameObject _characterPrafab;
    [SerializeField] Slider _healthSlider;

    #region Properties
    [Header("Attributes")]
    [SerializeField] CharacterTypes _characterType;
    [SerializeField] int _strength;
    [SerializeField] int _health;
    [SerializeField] int _movement;
    [SerializeField] int _range;

    public int getStrength { get { return _strength; } }
    public int getMovement { get { return _movement; } }
    public int getRange { get { return _range; } }
    #endregion

    #region State
    public bool movedThisTurn { get; set; }
    public bool attackedThisTurn { get; set; }
    public bool overwatchedThisTurn { get; set; }
    public int remainingHealth { get; set; }
    #endregion

    #region ID
    public CharacterColors myColor { get; private set; }
    private static int _characterID = 0;
    public int getCharacterID { get; private set; }
    public bool isDead { get; private set; }
    #endregion

    #region Graphics
    public Animator myAnimator { get; private set; }
    #endregion

    public void SetColor(CharacterColors newColor)
    {
        var mrList = _characterPrafab.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var mr in mrList)
            mr.material = _colorMaterials[(int)newColor];

        myColor = newColor;
    }

    protected void Awake()
    {
        myAnimator = _characterPrafab.GetComponent<Animator>();
        movedThisTurn = false;
        ++_characterID;
        getCharacterID = _characterID;
        remainingHealth = _health;
        UpdateHUD();
    }

    public void UpdateHUD()
    {
        _healthSlider.value = remainingHealth;
    }

    public void UpdateStatus()
    {
        if (remainingHealth <= 0)
        {
            isDead = true;
        }
    }
}