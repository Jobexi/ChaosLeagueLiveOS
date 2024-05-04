using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client.Models;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using TMPro;

public class NPCHandler : MonoBehaviour
{
    [SerializeField] private GameManager _gm;
    [SerializeField] private TwitchClient _twitchClient;
    [SerializeField] private TwitchApi _twitchAPI;
    [SerializeField] private TwitchPubSub _twitchPubSub;
    [SerializeField] private GoldDistributor _goldDistributor;
    [SerializeField] private InvitePromo _invitePromo;

    [SerializeField] private TileController _tileController;
    [SerializeField] private MyHttpClient _myHttpClient;
    [SerializeField] private KingController _kingController;
    [SerializeField] private RebellionController _rebellionController;
    [SerializeField] private SQLiteServiceAsync _sqliteServiceAsync; 
    [SerializeField] private UnitTesting _unitTesting; 

    [SerializeField] public Texture DefaultPFP;

    public int EventCountdown = 5;
    public int RefreshCountdown = 250;
    public int NPC = 0;
    public bool firstNPC = true;    

    void Awake()
    {
        
    }

    public void Update()
    {

    }

    public void CheckCountdown()
    {
        int maxNPC = 39;
        
        CheckNPCs();
        if (firstNPC == true)
        {
            InitializeNPCs(0);
            NPC = UnityEngine.Random.Range(0, maxNPC);
            firstNPC = false;
        }

        if (RefreshCountdown < 1)
        {
            InitializeNPCs(UnityEngine.Random.Range(0, maxNPC));
            InitializeNPCs(UnityEngine.Random.Range(0, maxNPC));
            InitializeNPCs(UnityEngine.Random.Range(0, maxNPC));
            _gm._twitchAPI.AskForBotAuthorization();
            RefreshCountdown = 250;
        }

        if (EventCountdown < 1)
        {                
            InitializeNPCs(NPC);
            NPC = UnityEngine.Random.Range(1, maxNPC);
            EventCountdown = UnityEngine.Random.Range(17, 77);
        }

        //If the current tile is a shop, all NPCs should try to buy something correspondent to their rank.
        //May need a timer somehow to make sure the shop is active before they make their purchase.

        Debug.Log($"{EventCountdown}");
        EventCountdown -= 1;
        RefreshCountdown -= 1;
    }

    public void InitializeNPCs(int ID)
    {
        switch (ID)
        {
            case 0:
                _unitTesting.NPCReward("GameMaster", "GameMaster", "bid", 1);
                StartCoroutine(UpdateWaiter("GameMaster"));
                break;
            case 1:
                _unitTesting.NPCReward("TinyDefender", "TinyDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("TinyDefender"));
                break;
            case 2:
                _unitTesting.NPCReward("SmallDefender", "SmallDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("SmallDefender"));
                break;
            case 3:
                _unitTesting.NPCReward("LittleDefender", "LittleDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("LittleDefender"));
                break;
            case 4:
                _unitTesting.NPCReward("MinorDefender", "MinorDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("MinorDefender"));
                break;
            case 5:
                _unitTesting.NPCReward("ModerateDefender", "ModerateDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("ModerateDefender"));
                break;
            case 6:
                _unitTesting.NPCReward("AverageDefender", "AverageDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("AverageDefender"));
                break;
            case 7:
                _unitTesting.NPCReward("Defender", "Defender", "bid", 1);
                StartCoroutine(UpdateWaiter("Defender"));
                break;
            case 8:
                _unitTesting.NPCReward("LargeDefender", "LargeDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("LargeDefender"));
                break;
            case 9:
                _unitTesting.NPCReward("MajorDefender", "MajorDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("MajorDefender"));
                break;
            case 10:
                _unitTesting.NPCReward("HugeDefender", "HugeDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("HugeDefender"));
                break;
            case 11:
                _unitTesting.NPCReward("MassiveDefender", "MassiveDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("MassiveDefender"));
                break;
            case 12:
                _unitTesting.NPCReward("GiganticDefender", "GiganticDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("GiganticDefender"));
                break;
            case 13:
                _unitTesting.NPCReward("EnormousDefender", "EnormousDefender", "bid", 1);
                StartCoroutine(UpdateWaiter("EnormousDefender"));
                break;
            case 14:
                _unitTesting.NPCReward("TinyAttacker", "TinyAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("TinyAttacker"));
                break;
            case 15:
                _unitTesting.NPCReward("SmallAttacker", "SmallAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("SmallAttacker"));
                break;
            case 16:
                _unitTesting.NPCReward("LittleAttacker", "LittleAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("LittleAttacker"));
                break;
            case 17:
                _unitTesting.NPCReward("MinorAttacker", "MinorAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("MinorAttacker"));
                break;
            case 18:
                _unitTesting.NPCReward("ModerateAttacker", "ModerateAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("ModerateAttacker"));
                break;
            case 19:
                _unitTesting.NPCReward("AverageAttacker", "AverageAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("AverageAttacker"));
                break;
            case 20:
                _unitTesting.NPCReward("Attacker", "Attacker", "bid", 1);
                StartCoroutine(UpdateWaiter("Attacker"));
                break;
            case 21:
                _unitTesting.NPCReward("LargeAttacker", "LargeAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("LargeAttacker"));
                break;
            case 22:
                _unitTesting.NPCReward("MajorAttacker", "MajorAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("MajorAttacker"));
                break;
            case 23:
                _unitTesting.NPCReward("HugeAttacker", "HugeAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("HugeAttacker"));
                break;
            case 24:
                _unitTesting.NPCReward("MassiveAttacker", "MassiveAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("MassiveAttacker"));
                break;
            case 25:
                _unitTesting.NPCReward("GiganticAttacker", "GiganticAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("GiganticAttacker"));
                break;
            case 26:
                _unitTesting.NPCReward("EnormousAttacker", "EnormousAttacker", "bid", 1);
                StartCoroutine(UpdateWaiter("EnormousAttacker"));
                break;
            case 27: 
                _unitTesting.NPCReward("TinyRebel", "TinyRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("TinyRebel"));
                break;
            case 28:
                _unitTesting.NPCReward("SmallRebel", "SmallRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("SmallRebel"));
                break;
            case 29:
                _unitTesting.NPCReward("LittleRebel", "LittleRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("LittleRebel"));
                break;
            case 30:
                _unitTesting.NPCReward("ModerateRebel", "ModerateRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("ModerateRebel"));
                break;
            case 31:
                _unitTesting.NPCReward("AverageRebel", "AverageRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("AverageRebel"));
                break;
            case 32:
                _unitTesting.NPCReward("Rebel", "Rebel", "bid", 1);
                StartCoroutine(UpdateWaiter("Rebel"));
                break;
            case 33:
                _unitTesting.NPCReward("LargeRebel", "LargeRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("LargeRebel"));
                break;
            case 34:
                _unitTesting.NPCReward("HugeRebel", "HugeRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("HugeRebel"));
                break;
            case 35:
                _unitTesting.NPCReward("MassiveRebel", "MassiveRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("MassiveRebel"));
                break;
            case 36:
                _unitTesting.NPCReward("EnormousRebel", "EnormousRebel", "bid", 1);
                StartCoroutine(UpdateWaiter("EnormousRebel"));
                break;
            case 37:
                _unitTesting.NPCReward("Celebrant", "Celebrant", "bid", 1);
                StartCoroutine(UpdateWaiter("Celebrant"));
                break;
            case 38:
                _unitTesting.NPCReward("LargeCelebrant", "LargeCelebrant", "bid", 1);
                StartCoroutine(UpdateWaiter("LargeCelebrant"));
                break;
            case 39:
                _unitTesting.NPCReward("MassiveCelebrant", "MassiveCelebrant", "bid", 1);
                StartCoroutine(UpdateWaiter("MassiveCelebrant"));
                break;
            
        }
                
    }

    IEnumerator UpdateWaiter(string ID)
    {
        yield return new WaitForSeconds(1);
        
        InitializePlayerHandler(ID);
    }

    public void InitializePlayerHandler(string ID)
    {
        switch (ID)
        {
            case "GameMaster":
                SetNPCMode(0, ID);
                NPCAttack(_gm.PlayerHandlers[ID]);
                break;
            case "TinyDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1);
                break;
            case "SmallDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10);
                break;
            case "LittleDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100);
                break;
            case "MinorDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000);
                break;
            case "ModerateDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000);
                break;
            case "AverageDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100000);
                break;
            case "Defender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000000);
                break;
            case "LargeDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000);
                break;
            case "MajorDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100000000);
                break;
            case "HugeDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000000000);
                break;
            case "MassiveDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000000);
                break;
            case "GiganticDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100000000000);
                break;
            case "EnormousDefender":
                SetNPCMode(1, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000000000000);
                break;
            case "TinyAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000);
                break;
            case "SmallAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000);
                break;
            case "LittleAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100000);
                break;
            case "MinorAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000000);
                break;
            case "ModerateAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000);
                break;
            case "AverageAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100000000);
                break;
            case "Attacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000000000);
                break;
            case "LargeAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000000);
                break;
            case "MajorAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100000000000);
                break;
            case "HugeAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000000000000);
                break;
            case "MassiveAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000000000);
                break;
            case "GiganticAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 100000000000000);
                break;
            case "EnormousAttacker":
                SetNPCMode(2, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 1000000000000000);
                break;
            case "TinyRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 200);
                break;
            case "SmallRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 300);
                break;
            case "LittleRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 400);
                break;
            case "ModerateRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 500);
                break;
            case "AverageRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 1000);
                break;
            case "Rebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 1700);
                break;
            case "LargeRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 2500);
                break;
            case "HugeRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 5000);
                break;
            case "MassiveRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 10000);
                break;
            case "EnormousRebel":
                SetNPCMode(3, ID);
                _unitTesting.NPCBits(_gm.PlayerHandlers[ID].pp.TwitchID, _gm.PlayerHandlers[ID].pp.TwitchUsername, 25000);
                break;
            case "Celebrant":
                SetNPCMode(4, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000);
                break;
            case "LargeCelebrant":
                SetNPCMode(4, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000);
                break;
            case "MassiveCelebrant":
                SetNPCMode(4, ID);
                NPCGivePoints(_gm.PlayerHandlers[ID].pp.TwitchUsername, 10000000);
                break;
        }
    
    }

    public void SetNPCMode(int mode, string key)
    {
        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
        _gm.PlayerHandlers[key].pp.IsNPC = 1;
        _gm.PlayerHandlers[key].pp.ModeNPC = mode;
        _gm.PlayerHandlers[key].pp.StateNPC = 0;
    }

    public void CheckNPCs()
    {
        string[] keys = _gm.PlayerHandlers.Keys.ToArray();
        foreach (string key in keys)
        {
            if (_gm.PlayerHandlers[key].pp.IsNPC == 1) 
            {
                if (_gm.PlayerHandlers[key].pp.ModeNPC == 0) //Defenders
                {
                    NPCTradeUp(_gm.PlayerHandlers[key].pp.TwitchID);
                    _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                    _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "bid", 1);
                }
                else if (_gm.PlayerHandlers[key].pp.ModeNPC == 1)
                {
                    if (_gm.PlayerHandlers[key].pp.StateNPC == 0)
                    {
                        NPCAttack(_gm.PlayerHandlers[key]);
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                        _gm.PlayerHandlers[key].pp.StateNPC = 1;
                    }
                    else if (_gm.PlayerHandlers[key].pp.StateNPC == 1)
                    {
                        if (_gm.PlayerHandlers[key].IsKing())
                        {
                            _gm.PlayerHandlers[key].pp.StateNPC = 2;
                            NPCMessage(_gm.PlayerHandlers[key], "Haha! Come and get me!");
                            _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        }
                        else
                        {
                            _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "bid", 1);
                            _gm.PlayerHandlers[key].pp.StateNPC = 0;
                        }
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;                        
                    }
                    else if (_gm.PlayerHandlers[key].pp.StateNPC == 2)
                    {
                        if (!_gm.PlayerHandlers[key].IsKing())
                        {
                            _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                            _gm.PlayerHandlers[key].pp.StateNPC = 3;
                            NPCMessage(_gm.PlayerHandlers[key], "Oh No, My Gold!");
                            _gm.PlayerHandlers[key].pp.Gold = _gm.PlayerHandlers[key].pp.Gold / 2;
                            _goldDistributor.SpawnGoldFromEvent(_gm.PlayerHandlers[key].pp.Gold);
                            NPCTradeUp(_gm.PlayerHandlers[key].pp.TwitchID);
                        }
                    }
                }
                else if (_gm.PlayerHandlers[key].pp.ModeNPC == 2) //Attackers
                {
                    if (_gm.PlayerHandlers[key].pp.StateNPC == 0)
                    {
                        NPCAttack(_gm.PlayerHandlers[key]);
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                        _gm.PlayerHandlers[key].pp.StateNPC = 1;
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "bid", 1);

                    }
                    else if (_gm.PlayerHandlers[key].pp.StateNPC == 1)
                    {
                        if (_gm.PlayerHandlers[key].IsKing())
                        {
                            _gm.PlayerHandlers[key].pp.StateNPC = 2;
                            NPCMessage(_gm.PlayerHandlers[key], "Fear Not. I have Rescued you all.");
                            _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        }
                        else
                        {                            
                            _gm.PlayerHandlers[key].pp.StateNPC = 0;
                            NPCAttack(_gm.PlayerHandlers[key]);
                        }
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                    }
                    else if (_gm.PlayerHandlers[key].pp.StateNPC == 2)
                    {
                        if (!_gm.PlayerHandlers[key].IsKing())
                        {                            
                            _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                            _gm.PlayerHandlers[key].pp.StateNPC = 3;
                            NPCMessage(_gm.PlayerHandlers[key], "Time to Give Back to the community.");
                            _gm.PlayerHandlers[key].pp.Gold = _gm.PlayerHandlers[key].pp.Gold / 2;
                            _goldDistributor.SpawnGoldFromEvent(_gm.PlayerHandlers[key].pp.Gold);
                            _unitTesting.NPCCommand(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "!tradeup");
                        }
                    }
                }
                else if (_gm.PlayerHandlers[key].pp.ModeNPC == 3) //Rebels
                {
                    if (_gm.PlayerHandlers[key].pp.StateNPC == 0)
                    {
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid B", 175);
                        _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid B", 175);
                        NPCTradeUp(_gm.PlayerHandlers[key].pp.TwitchID);
                        _gm.PlayerHandlers[key].pp.StateNPC = 1;
                    }
                    if (_gm.PlayerHandlers[key].pp.StateNPC == 1)
                    {
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                        if (_gm.PlayerHandlers[key].pp.AutoBidRemainder > 0 && _gm.PlayerHandlers[key].pp.CurrentBid == 0)
                            _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "bid", 1);
                    }
                }
                else if (_gm.PlayerHandlers[key].pp.ModeNPC == 4) //Celebrants
                {
                    if (_gm.PlayerHandlers[key].pp.StateNPC == 0)
                    {
                        NPCAttack(_gm.PlayerHandlers[key]);
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                        _gm.PlayerHandlers[key].pp.StateNPC = 1;
                    }
                    else if (_gm.PlayerHandlers[key].pp.StateNPC == 1)
                    {
                        if (_gm.PlayerHandlers[key].IsKing())
                        {
                            _gm.PlayerHandlers[key].pp.StateNPC = 2;
                           
                            switch(key)
                            {
                                case "Celebrant":
                                    _unitTesting.NPCBits(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, 500);
                                    break;
                                case "LargeCelebrant":
                                    _unitTesting.NPCBits(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, 2500);
                                    break;
                                case "MassiveCelebrant":
                                    _unitTesting.NPCBits(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, 5000);
                                    break;
                            }
                            _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "AutoBid A", 10);
                        }
                        else
                        {
                            _unitTesting.NPCReward(_gm.PlayerHandlers[key].pp.TwitchID, _gm.PlayerHandlers[key].pp.TwitchUsername, "bid", 1);
                            _gm.PlayerHandlers[key].pp.StateNPC = 0;
                        }
                        _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                    }
                    else if (_gm.PlayerHandlers[key].pp.StateNPC == 2)
                    {
                        if (!_gm.PlayerHandlers[key].IsKing())
                        {
                            _gm.PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                            _gm.PlayerHandlers[key].pp.StateNPC = 3;
                            NPCMessage(_gm.PlayerHandlers[key], "I hope you enjoyed the celebration!");
                            _gm.PlayerHandlers[key].pp.Gold = _gm.PlayerHandlers[key].pp.Gold / 2;
                            _goldDistributor.SpawnGoldFromEvent(_gm.PlayerHandlers[key].pp.Gold);
                            NPCTradeUp(_gm.PlayerHandlers[key].pp.TwitchID);
                        }
                    }
                }
            }
        }

    }

    public void NPCMessage(PlayerHandler ph, string msg)
    {
        ph.SpeechBubble(msg);
        if (ph.IsKing())
        {
            MyTTS.inst.PlayerSpeech(msg, Amazon.Polly.VoiceId.Joey);
        }
    }

    public void NPCTradeUp(PlayerHandler ph)
    {
        ph.TradeUp();
    }

    public void NPCGivePoints(string targetUsername, long desiredPointsToGive)
    {
  
        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredPointsToGive), desiredPointsToGive, _gm.PlayerHandlers["GameMaster"], _gm.PlayerHandlers[targetUsername], 0.1f, Color.white, _gm.PlayerHandlers[targetUsername].PfpTexture, TI_Type.GivePoints);

    }

    public void NPCAttack(PlayerHandler ph)
    {
        if (ph.pb != null)
        {
            return;
        }
        if (ph.GetState() == PlayerHandlerState.BiddingQ)
        {
            return;
        }
        if (ph.GetState() == PlayerHandlerState.Gameplay)
        {
            return;
        }
        if (RebellionController.RoyalCelebration)
        {
            return;
        }

        PlayerBall pb = ph.GetPlayerBall();
        ph.SetState(PlayerHandlerState.Gameplay); //Prevent bug where players could enter bidding Q while king if timed correctly
        ph.ReceivableTarget = null; //Prevent bug where players would move to raffle after attacking and get stuck
        _twitchClient._attackPipe.ReceivePlayer(pb);

    }


}
