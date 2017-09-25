﻿using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Script;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Pawn Script
/// 
/// </summary>
public class Pawn : NetworkBehaviour
{
    #region Properties
    //Progress of the Pawn on its path
    [SyncVar]
    public int Progress = 0;

    //Name of the pawn (perhaps useless)
    [SyncVar]
    public String PawnName;

    //Index of the Pawn
    [SyncVar]
    public int PawnIndex;

    //Pawn on the board ?
    [SyncVar(hook = "OnChangeOnBoard")]
    public bool OnBoard = false;

    //Owning player of this Pawn
    [SyncVar(hook = "OnChangeColor")]
    public PlayerColorEnum Player;

    public PawnTestedEnum Status = PawnTestedEnum.UNTESTED;


    //Spawn positions for the pawns
    public SpawnPositions spawnPositions;
    private GameObject outPosition;

    //Components of the pawn
    public Animator PawnAnimator;
    private MeshRenderer PawnMeshRenderer;
    private Light selectableLight;

    //Hash of Animator parameters
    private int enterHash = Animator.StringToHash("EnterBoard");
    private int exitHash = Animator.StringToHash("ExitBoard");
    private int StateHash = Animator.StringToHash("ProgressOnBoard");
    private int speedHash = Animator.StringToHash("Speed");

    public GameObject GhostPawnPrefab;

    private Color actualHaloColor;
    private Color initialHaloColor;

    public Color ActualHaloColor
    {
        get
        {
            return actualHaloColor;
        }

        set
        {
            actualHaloColor = value;
            SelectableLight.color = value;
        }
    }

    public Light SelectableLight
    {
        get
        {
            if (selectableLight == null)
            {
                selectableLight = this.GetComponentInChildren<Light>();

            }
            return selectableLight;
        }

        set
        {
            selectableLight = value;
        }
    }

    public delegate void OnPawnSelected(Pawn pawnSelected);
    public event OnPawnSelected EventOnPawnSelected;
    #endregion

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {


    }

    #region Events
    /// <summary>
    /// Event called when the pawn is created
    /// Get the component attached to the pawn
    /// </summary> 
    private void OnEnable()
    {
        PawnAnimator = GetComponent<Animator>();
        PawnMeshRenderer = this.GetComponentInChildren<MeshRenderer>();
        spawnPositions = FindObjectOfType<SpawnPositions>();
    }


    /// <summary>
    /// Event called when changing the owning Player
    /// </summary>
    /// <param name="newColor"></param>
    public void OnChangeColor(PlayerColorEnum newColor)
    {
        //Change the material color of the pawn
        PawnMeshRenderer.material.color = PlayerColorEnumToColor(newColor);

        //Pawn named after its color and index
        this.name = newColor.ToString() + PawnIndex.ToString();
        PawnName = newColor.ToString() + PawnIndex.ToString();
        //Get the out position for this pawn
        outPosition = spawnPositions.getOutPosition(newColor, PawnIndex);
    }

    private Color PlayerColorEnumToColor(PlayerColorEnum newColor)
    {
        Color color = Color.black;
        switch (newColor)
        {
            case PlayerColorEnum.Blue:
                color = Color.blue;
                break;
            case PlayerColorEnum.Green:
                color = Color.green;
                break;
            case PlayerColorEnum.Red:
                color = Color.red;
                break;
            case PlayerColorEnum.Yellow:
                color = Color.yellow;
                break;
        }
        return color;

    }

    /// <summary>
    /// Update the position of the pawn regarding if it is entering or exiting the board
    /// </summary>
    /// <param name="onBoard"></param>
    public void OnChangeOnBoard(bool onBoard)
    {
        if (!PawnAnimator.enabled)
        {
            PawnAnimator.enabled = true;
        }
        if (onBoard)
        {
            Transform startTransform = spawnPositions.getStartPosition(Player).transform;
            this.transform.position = startTransform.position;
            this.transform.rotation = startTransform.rotation;

            PawnAnimator.SetTrigger(enterHash);
        }
        else
        {
            PawnAnimator.SetTrigger(exitHash);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "PawnModel")
        {
            if (PawnAnimator.enabled && !other.GetComponentInParent<Pawn>().PawnAnimator.enabled)
            {
                (other.GetComponentInParent<Pawn>()).Exit();
            }
        }
    }

    public void CheckProgress(int animationProgress)
    {
        if (animationProgress == Progress)
        {
            PawnAnimator.SetFloat(speedHash, 0);
        }
    }
    #endregion

    #region methods
    /// <summary>
    /// Set the color, index, layer used and out position of the pawn
    /// </summary>
    /// <param name="color"></param>
    /// <param name="pawnIndex"></param>
    public void Initialize(PlayerColorEnum color, int pawnIndex)
    {
        this.PawnIndex = pawnIndex;
        Player = color;
        this.transform.position = outPosition.transform.position;

    }

    /// <summary>
    /// Get the pawn on the board
    /// </summary>
    public void Enter()
    {
        OnBoard = true;
    }


    public void Move(int nbCell)
    {
        PawnAnimator.SetFloat(speedHash, 1);

        Progress += nbCell;
        PawnAnimator.Play(StateHash);
    }
    /// <summary>
    /// Get the pawn of the board
    /// </summary>
    public void Exit()
    {
        OnBoard = false;
        this.Progress = 0;
    }

    public void PlacePawnOut()
    {
        this.transform.position = outPosition.transform.position;
    }
    #region Projection
    public void MakeProjection(int nbCell)
    {
        if (OnBoard)
        {
            List<Pawn> PawnsEncoutered = new List<Pawn>();
            GameObject ghostObject = Instantiate(GhostPawnPrefab);
            GhostPawn ghost = ghostObject.GetComponent<GhostPawn>();
            ghost.Initialize(this);
            GhostPawn.EventOnProjectionFinished += testProjection;
            ghost.Projection(nbCell);
        }
    }

    public void testProjection(List<Pawn> pawnEncoutered)
    {
        pawnEncoutered.RemoveAt(0);
        if (pawnEncoutered.Count > 0)
        {
            foreach (Pawn item in pawnEncoutered)
            {
                if (item.Progress == 0)
                {
                    this.Status = PawnTestedEnum.CANNOT_MOVE;
                }
            }
            if (pawnEncoutered[pawnEncoutered.Count - 1].Progress > 70)
            {
                this.Status = PawnTestedEnum.CANNOT_MOVE;
            }
            if (this.Status == PawnTestedEnum.UNTESTED)
            {
                this.Status = PawnTestedEnum.CAN_MOVE;
            }
        }
    }
    #endregion
    #region Selection Halo
    public void SwitchHalo(bool on, PlayerColorEnum playerColor)
    {
        ActualHaloColor = PlayerColorEnumToColor(playerColor);
        SelectableLight.enabled = on;
    }


    public void OnMouseDown()
    {
        if (SelectableLight.enabled)
        {
            EventOnPawnSelected(this);
        }
    }

    public void OnMouseOver()
    {
        if (SelectableLight.enabled)
        {
            SelectableLight.color = Color.grey;
        }
    }

    public void OnMouseExit()
    {
        if (SelectableLight.enabled)
        {
            SelectableLight.color = ActualHaloColor;
        }
    }
    #endregion
    #endregion
}
