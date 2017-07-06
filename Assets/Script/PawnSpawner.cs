﻿using Assets.Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// GameObject handling the pawns creation and spawning
/// </summary>
public class PawnSpawner : NetworkBehaviour
{
    //Event triggered when all the pawns are created
    public delegate void OnAllPawnsCreation();
    [SyncEvent]
    public static event OnAllPawnsCreation EventAllPawnsCreated;

    //The prefab used for the Pawn GameObject
    public GameObject PawnPrefab;
    //List of all the players in the Game
    public TockPlayer[] playerList;
    //Used for debugging
    public Text text;


    // Use this for initialization
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Create and spawn a new Pawn
    /// </summary>
    /// <param name="player">Owner of the pawn</param>
    /// <param name="pawnIndex"></param>
    /// <returns></returns>
    private Pawn CreatePawn(PlayerColorEnum player, int pawnIndex)
    {

        GameObject newPawn = Instantiate(PawnPrefab);
        Pawn retour = newPawn.GetComponent<Pawn>();

        retour.Initialise(player, pawnIndex);

        NetworkServer.Spawn(newPawn);

        return retour;
    }

    /// <summary>
    /// Create all pawns for all players
    /// </summary>
    public void PopulatePawns()
    {
        //For debugging
        if (text == null)
        {
            text = GameObject.Find("TextPawnSpawner").GetComponent<Text>();
        }


        playerList = FindObjectsOfType<TockPlayer>();
        //FOR EACH player, create 4 pawns
        foreach (TockPlayer player in playerList)
        {
            text.text += "Populating " + player.PlayerColor.ToString() + " Pawn : ";
            for (int i = 1; i <= 4; i++)
            {
                text.text += CreatePawn(player.PlayerColor, i).Player.ToString() + " ";
            }

        }
        //Trigger Event, used to tell the GameMaster to build his dictionnary of Pawns
        EventAllPawnsCreated();

        //Build the pawn list of each player
        foreach (TockPlayer player in playerList)
        {
            player.RpcBuildPawnList();

        }
    }


    [Command]
    public void CmdTestEnter(string player)
    {
        TockPlayer blop = GameObject.FindGameObjectWithTag(player +"_Player").GetComponent<TockPlayer>();
        blop.CmdEnterPawn(1);
    }

    [Command]
    public void CmdTestMove(string player)
    {
        TockPlayer blop = GameObject.FindGameObjectWithTag(player + "_Player").GetComponent<TockPlayer>();
        blop.CmdMovePawn(1,3);

    }

}
