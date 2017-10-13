﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Card script
/// </summary>
public class Card : NetworkBehaviour
{
    [SyncVar]
    public CardsColorsEnum Color;
    [SyncVar(hook = "OnChangeValue")]
    public CardsValuesEnum Value;

    public SelectionFilterEnum ColorFilter;

    public delegate void CardEffect(Pawn target, Pawn otherTarget = null);
    public CardEffect Effect;

    public delegate bool CardProjection(Pawn target);
    public List<CardProjection> Projections = new List<CardProjection>();

    public Material Illustration;

    //Pawn which can be played by this card after projection
    public List<Pawn> possibleTargets;

    private GameMaster gMaster;

    public GameMaster GMaster
    {
        get
        {
            if (gMaster == null)
            {
                GMaster = GameObject.Find("NetworkGameMaster").GetComponent<GameMaster>();
            }
            return gMaster;
        }

        set
        {
            gMaster = value;
        }
    }


    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Illustration == null && this.Value != 0)
        {
            OnChangeValue(this.Value);
        }
    }

    /// <summary>
    /// Set the color and value of this card
    /// </summary>
    /// <param name="color"></param>
    /// <param name="value"></param>
    public void Initialize(CardsColorsEnum color, CardsValuesEnum value)
    {
        Color = color;
        Value = value;

    }

    /// <summary>
    /// Update the card attributes according to the new value
    /// </summary>
    /// <param name="value"></param>
    public void OnChangeValue(CardsValuesEnum value)
    {
        Value = value;
        this.name = value.ToString() + "_" + Color.ToString();
        initCard(value);
        Illustration = Resources.Load<Material>("Materials/Cards/" + this.name);
        this.gameObject.transform.GetChild(1).GetComponentInChildren<MeshRenderer>().material = Illustration;

    }

    #region Card Effect
    /// <summary>
    /// Initialise the card according to the new value, Effect and Filter
    /// </summary>
    /// <param name="value"></param>
    private void initCard(CardsValuesEnum value)
    {
        switch (value)
        {
            case CardsValuesEnum.ACE:
            case CardsValuesEnum.KING:
                Effect = MoveOrEnter;
                ColorFilter = SelectionFilterEnum.OWNPAWNS;
                Projections.Add(ParteuFilter);
                break;
            case CardsValuesEnum.TWO:
            case CardsValuesEnum.THREE:
            case CardsValuesEnum.SIX:
            case CardsValuesEnum.EIGHT:
            case CardsValuesEnum.NINE:
            case CardsValuesEnum.TEN:
            case CardsValuesEnum.QUEEN:
                Effect = Move;
                ColorFilter = SelectionFilterEnum.OWNPAWNS;
                Projections.Add(MoveFilter);
                Projections.Add(OnBoardFilter);
                break;
            case CardsValuesEnum.FOUR:
                Effect = FOUR;
                ColorFilter = SelectionFilterEnum.OWNPAWNS;
                Projections.Add(OnBoardFilter);
                break;
            case CardsValuesEnum.FIVE:
                Effect = Move;
                ColorFilter = SelectionFilterEnum.OTHERPAWNS;
                Projections.Add(OnBoardFilter);
                Projections.Add(MoveFilter);
                break;
            case CardsValuesEnum.SEVEN:
                Effect = SEVEN;
                ColorFilter = SelectionFilterEnum.OWNPAWNS;
                Projections.Add(OnBoardFilter);
                break;
            case CardsValuesEnum.JACK:
                Effect = JACK;
                ColorFilter = SelectionFilterEnum.OWNPAWNS;
                Projections.Add(OnBoardFilter);
                break;
            case CardsValuesEnum.JOKER:
                Effect = JOKER;
                ColorFilter = SelectionFilterEnum.OWNPAWNS;
                Projections.Add(ParteuFilter);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Make the target enter the board or move the target according to the value of the card wiping all pawns in the way
    /// </summary>
    /// <param name="target"></param>
    /// <param name="otherTarget"></param>
    private void JOKER(Pawn target, Pawn otherTarget = null)
    {
        if (target.OnBoard)
        {
            target.Move((int)Value, true);
        }
        else
        {
            target.Enter();
        }
    }

    /// <summary>
    /// Exchange the places of the targets
    /// </summary>
    /// <param name="target"></param>
    /// <param name="otherTarget"></param>
    private void JACK(Pawn target, Pawn otherTarget)
    {
        target.Exchange(otherTarget);
    }

    /// <summary>
    /// Not used, Seven logic is done in the TockPlayer class
    /// </summary>
    /// <param name="target"></param>
    /// <param name="otherTarget"></param>
    private void SEVEN(Pawn target, Pawn otherTarget = null)
    {

    }

    /// <summary>
    /// Move the target backward
    /// </summary>
    /// <param name="target"></param>
    /// <param name="otherTarget"></param>
    private void FOUR(Pawn target, Pawn otherTarget = null)
    {
        target.Move(-(int)Value);
    }

    /// <summary>
    /// Move the target forward
    /// </summary>
    /// <param name="target"></param>
    /// <param name="otherTarget"></param>
    private void Move(Pawn target, Pawn otherTarget = null)
    {
        target.Move((int)Value);
    }

    /// <summary>
    /// Make the target enter the board or move the target according to the value of the card
    /// </summary>
    /// <param name="target"></param>
    /// <param name="otherTarget"></param>
    private void MoveOrEnter(Pawn target, Pawn otherTarget = null)
    {
        if (target.OnBoard)
        {
            target.Move((int)Value);
        }
        else
        {
            target.Enter();
        }
    }

    #endregion
    #region cardFilter
    /// <summary>
    /// Test if the pawn target can move according the value of the card
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool MoveFilter(Pawn target)
    {
        return MoveFiltering(target);
    }

    /// <summary>
    /// Test if the pawn target can move according the value of the card or the specified number of cell
    /// </summary>
    /// <param name="target"></param>
    /// <param name="nbMoves">int - if -1, test with value of the card, else test with the specified number</param>
    /// <returns></returns>
    public bool MoveFiltering(Pawn target, int nbMoves = -1)
    {
        bool Playable = true;
        //IF nbMoves == -1 THEN test with value of the card
        if (nbMoves == -1)
        {
            nbMoves = (int)Value;
        }
        //IF target has finished => not playable
        if (target.Progress + (nbMoves * (this.Value == CardsValuesEnum.FOUR ? -1 : 1)) > 74)
        {
            Playable = false;
        }
        else
        {
            //Compute the progress according to the color of the pawn and the value of the card
            int progressToCheck = target.Progress + nbMoves * (this.Value == CardsValuesEnum.FOUR ? -1 : 1) + 18 * (int)target.PlayerColor;
            //Get the list of pawn to be tested
            List<Pawn> pawnEncoutered = GMaster.progressDictionnary.GetPawnsInRange(target.Progress + 18 * (int)target.PlayerColor, progressToCheck);
            //Test if there is pawn on its starting position
            if (pawnEncoutered.Count > 0)
            {
                foreach (Pawn item in pawnEncoutered)
                {
                    if (item.Status == PawnStatusEnum.ENTRY)
                    {
                        Playable = false;
                    }
                }
            }
            //Test if there is a pawn on the destination and if it is in house
            if (GMaster.progressDictionnary.ContainsValue(progressToCheck))
            {
                if (GMaster.progressDictionnary.GetPawn(progressToCheck).Status == PawnStatusEnum.IN_HOUSE)
                {
                    Playable = false;
                }
            }
        }
        return Playable;
    }

    /// <summary>
    /// Test if the target is on the board
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool OnBoardFilter(Pawn target)
    {
        return target.OnBoard;
    }

    /// <summary>
    /// Test if the target is on the board, and if can make the move
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool ParteuFilter(Pawn target)
    {
        bool Playable = true;
        if (target.OnBoard)
        {
            Playable = MoveFilter(target);
        }
        return Playable;
    }

    /// <summary>
    /// Filter for the seven card...useless
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool SevenFilter(Pawn target)
    {
        bool Playable = true;
        return Playable;
    }
    #endregion
    #region Projection
    /// <summary>
    /// Test if every pawn in the given list can be played with the card
    /// </summary>
    /// <param name="listToTest"></param>
    /// <returns></returns>
    public bool MakeProjections(List<Pawn> listToTest)
    {
        bool playable = true;
        possibleTargets.Clear();
        //IF the card is Seven, use the projection function specific to the seven
        if (this.Value == CardsValuesEnum.SEVEN)
        {
            playable = ProjectionSeven(listToTest);
        }
        else
        {
            foreach (Pawn item in listToTest)
            {
                playable = true;
                //test the pawn with each filter of the card
                foreach (CardProjection projection in this.Projections)
                {
                    if (!projection(item))
                    {
                        playable = false;
                        break;
                    }
                }
                if (playable)
                {
                    possibleTargets.Add(item);
                }
            }
        }
        return playable;
    }

    /// <summary>
    /// Test if pawns can be switched
    /// </summary>
    /// <param name="listToTest"></param>
    /// <returns></returns>
    private bool ProjectionSeven(List<Pawn> listToTest)
    {
        bool playable = true;
        int indexPawn = 0;
        int movementAdded = 1;
        int movemenTotal = 0;

        for (indexPawn = 0; indexPawn < listToTest.Count; indexPawn++)
        {
            Pawn pawnTested = listToTest[indexPawn];
            if (pawnTested.OnBoard)
            {
                movementAdded = 1;
                //Compute movement max for the pawn
                while (!GMaster.progressDictionnary.ContainsValue(GMaster.progressDictionnary[pawnTested] + movementAdded) && (movementAdded < 8))
                {
                    movementAdded++;
                    movemenTotal++;
                }
                //IF the pawn can move for minimum 1 cell, add it to possibles argets
                if (movementAdded > 1)
                {
                    possibleTargets.Add(pawnTested);
                }
            }
        }
        //IF movement total is inferior to the card value
        if (movemenTotal < (int)Value)
        {
            playable = false;
        }
        return playable;
    }
    #endregion
    /// <summary>
    /// Apply the card effect on the targetted pawn
    /// </summary>
    /// <param name="target"></param>
    /// <param name="otherTarget"></param>
    public void Play(Pawn target, Pawn otherTarget = null)
    {
        Effect(target, otherTarget);
    }
}
