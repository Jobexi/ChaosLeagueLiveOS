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

public class GameManager : MonoBehaviour
{
    [SerializeField] private TwitchClient _twitchClient;
    [SerializeField] private TwitchApi _twitchAPI;
    [SerializeField] private TwitchPubSub _twitchPubSub;
    [SerializeField] private GoldDistributor _goldDistributor;
    [SerializeField] private InvitePromo _invitePromo;

    [SerializeField] private GameObject _playerBallPrefab;
    [SerializeField] private CLDebug _clDebug;
    [SerializeField] private TileController _tileController;
    [SerializeField] private MyHttpClient _myHttpClient;
    [SerializeField] private KingController _kingController;
    [SerializeField] private RebellionController _rebellionController;
    [SerializeField] private SQLiteServiceAsync _sqliteServiceAsync; 
    [SerializeField] private UnitTesting _unitTesting; 

    [SerializeField] public Texture DefaultPFP;
    [SerializeField] private GameObject _pbHologramPrefab;

    [SerializeField] private TextMeshPro _event1Text;

    public int EventCountdown = 35;
    public int NPC = 0;
    public bool firstNPC = true;

    private byte redhue = 150;
    private byte bluehue = 150;
    private byte greenhue = 150;
    private int huehuehue = 0;

    public Dictionary<string, PlayerHandler> PlayerHandlers = new Dictionary<string, PlayerHandler>();

    public HoldingPen HoldingPen;
    public Transform PlayerHandlersRoot;
    public Transform PlayerHandlersPoolRoot;
    public Transform PlayerBallsPoolRoot;

    private float _resourceUnloadTimer = 0;
    private float _secondsBetweenResourceUnloads = 60;

    public ObjectPool<PlayerHandler> PlayerHandlersPool;
    public ObjectPool<PlayerBall> PlayerBallsPool;

    public float BubbleFillHoldAlpha;
    public float BubbleTextHoldAlpha;

    [SerializeField] public Vector3 UsernameOffset;
    [SerializeField] public Vector3 PointsTextOffset;

    private StringBuilder _sb = new StringBuilder();

    [HideInInspector] public Sprite CommunityPointSprite;

    void Awake()
    {
        Application.targetFrameRate = 60;

        Debug.Log("Unity Main Thread: " + Thread.CurrentThread.ManagedThreadId);

        _clDebug.Initialize();

        LoadAppConfig();

        PlayerHandlersPool = new ObjectPool<PlayerHandler>(PlayerHandlerFactory, TurnOnPlayerHandler, TurnOffPlayerHandler);
        PlayerBallsPool = new ObjectPool<PlayerBall>(PlayerBallFactory, TurnOnPlayerBall, TurnOffPlayerBall);
    }


    public void Update()
    {

        _resourceUnloadTimer += Time.deltaTime;
        if (_resourceUnloadTimer > _secondsBetweenResourceUnloads)
        {
            CLDebug.Inst.Log("Unloading unused assets");
            _resourceUnloadTimer = 0;
            Resources.UnloadUnusedAssets();
        }
    }

    public IEnumerator CreateNewPlayerHandler(string twitchId)
    {
        PlayerHandler ph = PlayerHandlersPool.GetObject();
        ph.gameObject.name = twitchId;
        ph.gameObject.transform.SetParent(PlayerHandlersRoot);
        PlayerHandlers.Add(twitchId, ph);
        yield return ph.CInitPlayerHandler(this, twitchId);


    }

    public IEnumerator GetPlayerHandler(string twitchID, CoroutineResult<PlayerHandler> phResult)
    {
        //Debug.Log("at the top of get player handler");
        PlayerHandler ph = null; 
        PlayerHandlers.TryGetValue(twitchID, out ph);

        //If we were going to unload it this frame, cancel it
        if (ph != null)
            ph.LastAccess = DateTime.Now;

        //This ensures we don't create a duplicate player handler if it isn't done with the first one yet
        while (ph != null && ph.Initializing)
            yield return null; 

        //If the ph doesn't exist yet, create one
        if(ph == null)
        {
            yield return CreateNewPlayerHandler(twitchID);
            PlayerHandlers.TryGetValue(twitchID, out ph);
        }

        //Assert that we suceeded in creating a player handler
        if(ph == null)
        {
            Debug.LogError($"Failed to get player handler with id: {twitchID}");
            phResult.Complete(null); 
            yield break;
        }

        ph.LastAccess = DateTime.Now; 
        ph.SetCustomizationsFromPP();

        phResult.Complete(ph);

    }

    public IEnumerator GetSneakyPlayerHandler(PlayerHandler ph, string twitchID)
    {
        
        //If we were going to unload it this frame, cancel it
        if (ph != null)
            ph.LastAccess = DateTime.Now;

        //This ensures we don't create a duplicate player handler if it isn't done with the first one yet
        while (ph != null && ph.Initializing)
            yield return null;

        //If the ph doesn't exist yet, create one
        if (ph == null)
        {
            yield return CreateNewPlayerHandler(twitchID);
            PlayerHandlers.TryGetValue(twitchID, out ph);
        }

        //Assert that we suceeded in creating a player handler
        if (ph == null)
        {
            Debug.LogError($"Failed to get player handler with id: {twitchID}");
            yield break;
        }

        ph.LastAccess = DateTime.Now;
        ph.SetCustomizationsFromPP();

    }

    public IEnumerator GetPlayerByUsername(string twitchUsername, CoroutineResult<PlayerHandler> coResult)
    {


        if (string.IsNullOrEmpty(twitchUsername))
        {
            Debug.Log($"Failed to get player by username: {twitchUsername}");
            coResult.Complete(null);
            yield break;
        }

        var ph = PlayerHandlers.Values.FirstOrDefault(ph => string.Equals(ph.pp.TwitchUsername, twitchUsername, StringComparison.OrdinalIgnoreCase));

        if (ph != null)
        {
            coResult.Complete(ph);
            yield break;
        }

        var t = Task.Run(async () => await TwitchApi.GetUserByUsername(twitchUsername));
        yield return new WaitUntil(() => t.IsCompleted);

        User user = t.Result;

        if(user == null)
        {
            Debug.Log($"Failed to get player by username: {twitchUsername}");
            coResult.Complete(null);
            yield break;
        }

        yield return GetPlayerHandler(user.Id, coResult);
        coResult.Complete(coResult.Result);
    }

    public void DestroyPlayerBall(PlayerBall pb)
    {
        pb.Ph.SetState(PlayerHandlerState.Idle);

        PlayerHandler ph = pb.Ph;
        ph.ReceivableTarget = null;

        GameTile gameplayTile = _tileController.GameplayTile;

        //If they're on the conveyor belt or in gameplay, eliminate them
        if(gameplayTile != null && (gameplayTile.ConveyorBelt.Contains(ph) || gameplayTile.AlivePlayers.Contains(ph)))
            gameplayTile.EliminatePlayer(pb.Ph, true);

        pb.Ph.pb = null;
        pb.gameObject.name = "pooledPB";
        pb.transform.SetParent(PlayerBallsPoolRoot);

        PlayerBallsPool.ReturnObject(pb);

 

        //After they're eliminated, if they bid while in gameplay, automatically enter the bidding Q for the next tile
        if (ph.GetBid() > 0)
        {
            _tileController.BidHandler.TryAddToBiddingQ(ph);
            return;
        }

    }

    private void LoadAppConfig()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "config.json");
        string fileContents = File.ReadAllText(filePath);

        AppConfig.LoadFromJson(fileContents);



        //QuipBattlePrompts.LoadPrompts(promptsJSON);

        string pathToEnv = Path.Combine(Application.streamingAssetsPath, "secrets.env");
        AppConfig.LoadEnvironmentVariables(pathToEnv); 

        string communityPointSprite = Path.Combine(Application.streamingAssetsPath, "communityPointSprite.png");

        // Check if custom sprite exists
        if (File.Exists(communityPointSprite))
        {
            // Load the custom sprite
            byte[] fileData = File.ReadAllBytes(communityPointSprite);
            Texture2D texture = new Texture2D(1, 1, TextureFormat.Alpha8, false); // Create new "empty" texture
            //texture.filterMode = FilterMode.Bilinear;
            //texture.anisoLevel = 0; 
            texture.LoadImage(fileData); // Load the image data into the texture (this will also resize the texture to the correct dimensions)
            texture.wrapMode = TextureWrapMode.Clamp;
            //texture.alphaIsTransparency = true;

            // Create a sprite from the texture
            CommunityPointSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError("Failed to find community point sprite in streaming assets folder (communityPointSprite.png)"); 
        }
    }


    public PlayerBall CreatePlayerBall(PlayerHandler ph)
    {
        PlayerBall pb = PlayerBallsPool.GetObject();
        pb.gameObject.name = ph.pp.TwitchUsername;
        ph.pb = pb;
        pb.transform.SetParent(ph.transform);

        pb.InitPB(this, ph);

        //Move the text off the screen the first frame so it doesn't pop in
        if (pb._usernameText != null)
            pb._usernameText.transform.position = HoldingPen.GetReceivePosition();
        if (pb._pointsText != null)
            pb._pointsText.transform.position = HoldingPen.GetReceivePosition();

 

        return pb; 
    }

    private void TurnOnPlayerHandler(PlayerHandler ph)
    {
        ph.gameObject.SetActive(true);

    }
    private void TurnOffPlayerHandler(PlayerHandler ph)
    {
        ph.gameObject.SetActive(false);

    }

    private PlayerHandler PlayerHandlerFactory()
    {
        GameObject playerHandler = new GameObject();
        PlayerHandler ph = playerHandler.AddComponent<PlayerHandler>();
        ph.pbh = Instantiate(_pbHologramPrefab, ph.transform).GetComponent<PBHologram>();
        ph.pbh.InitPBHologram(ph);
        ph.DisableHologram();
        ph.pbh.transform.position = HoldingPen.transform.position; //Have to do this here because the ph isn't instantiated yet so it can't find the holding pen 



        return ph;
    }

    private void TurnOnPlayerBall(PlayerBall pb)
    {
        pb.gameObject.SetActive(true);
        pb._rb2D.transform.position = HoldingPen.Get_TI_IO_Position();

    }

    private void TurnOffPlayerBall(PlayerBall pb)
    {
        pb.gameObject.SetActive(false);
        pb._rb2D.transform.position = HoldingPen.Get_TI_IO_Position();

    }

    private PlayerBall PlayerBallFactory()
    {
        GameObject newObj = Instantiate(_playerBallPrefab);
        newObj.SetActive(false);
        PlayerBall pb = newObj.GetComponent<PlayerBall>();

        return pb;
    }

    public void ChangeHue()
    {
        if (huehuehue == 0)
        {
            redhue += 1;
            if (redhue == 255)
                huehuehue = 1;
        }
        else if (huehuehue == 1)
        {
            greenhue += 1;
            if (greenhue == 255)
                huehuehue = 2;
        }
        else if (huehuehue == 2)
        {
            redhue -= 1;
            if (redhue == 25)
                huehuehue = 3;
        }
        else if (huehuehue == 3)
        {
            bluehue += 1;
            if (bluehue == 255)
                huehuehue = 4;
        }
        else if (huehuehue == 4)
        {
            greenhue -= 1;
            if (greenhue == 25)
                huehuehue = 5;
        }
        else if (huehuehue == 5)
        {
            redhue += 1;
            if (redhue == 255)
                huehuehue = 6;
        }
        else if (huehuehue == 6)
        {
            bluehue -= 1;
            if (bluehue == 25)
                huehuehue = 1;
        }

        _event1Text.color = new Color32(redhue, greenhue, bluehue, 255);
            }

    public void SaveAndQuitButtonClick()
    {
        StartCoroutine(SaveAndQuit());
    }


    public IEnumerator HandleInviteSignal(User invitedUser, User invitorUser)
    {
        Debug.Log($"handling invite signal in game manager {invitedUser.Id} {invitorUser.Id}");

        //Get the player handler for the invited ph
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return GetPlayerHandler(invitedUser.Id, coResult);
        PlayerHandler invitedPh = coResult.Result;

        if(invitedPh == null)
        {
            Debug.LogError($"Failed to handle invite signal. invitedPh is null for invited: {invitedUser.Login} and invitor: {invitorUser.Login}");
            yield break;
        }
        invitedPh.pp.TwitchUsername = invitedUser.Login;
        yield return invitedPh.LoadBallPfp();  //Important, this depends on the twitch username being set before it can load the pfp




        coResult.Reset();
        yield return GetPlayerHandler(invitorUser.Id, coResult);
        PlayerHandler invitorPh = coResult.Result;

        if (invitorPh == null)
        {
            Debug.LogError($"Failed to handle invite signal. invitorPh is null for invited: {invitedUser.Login} and invitor: {invitorUser.Login}");
            yield break;
        }
        invitorPh.pp.TwitchUsername = invitorUser.Login;
        yield return invitorPh.LoadBallPfp();  //Important, this depends on the twitch username being set before it can load the pfp

        yield return invitedPh.SetInvitor(invitorPh, _twitchClient, _invitePromo);

        invitedPh.pp.InviteCount++;
    }

    public KingController GetKingController()
    {
        return _kingController; 
    }

    public TileController GetTileController()
    {
        return _tileController;
    }
    public RebellionController GetRebellionController() 
    {
        return _rebellionController; 
    }


    private IEnumerator SaveAndQuit()
    {
        yield return SaveAllPlayerProfilesToDB(); 

        //Save the app config
        string filePath = Path.Combine(Application.streamingAssetsPath, "config.json");
        AppConfig.SaveConfigFile(filePath);

        SQLiteServiceAsync.CloseConnection(); 

#if !UNITY_EDITOR
        SavePreviousLog(); 
        Application.Quit();
#endif

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        yield return new WaitForSeconds(1f); //Wait for SQLiteService to close connections
    }

    public IEnumerator SaveAllPlayerProfilesToDB()
    {
        //Save all player profiles to db
        string[] keys = PlayerHandlers.Keys.ToArray();
        foreach (string key in keys)
        {
            PlayerHandlers.TryGetValue(key, out PlayerHandler ph);
            if (ph == null)
                continue;

            var t = Task.Run(async () => await SQLiteServiceAsync.UpdatePlayer(ph.pp));
            yield return new WaitUntil(() => t.IsCompleted);
        }
    }

    public void ClearAllInvitesData()
    {
        string[] keys = PlayerHandlers.Keys.ToArray();
        foreach (string key in keys)
        {
            PlayerHandlers[key].pp.InvitedByID = "";
            PlayerHandlers[key].pp.InvitesJSON = "";
        }

        _sqliteServiceAsync.ClearInvitesData(); 
    }

    public void RebidChecker()
    {
        string[] keys = PlayerHandlers.Keys.ToArray();
        foreach (string key in keys)
        {
            if (PlayerHandlers[key].pp.AutoBidRemainder > 0 && PlayerHandlers[key].pp.RiskSkips > 0  && PlayerHandlers[key].pp.CurrentBid == 0)
            {
                PlayerHandlers[key].pp.LastInteraction = DateTime.Now;

                if (_tileController.CurrentBiddingTile.IsRisk)
                    PlayerHandlers[key].pp.RiskSkips -= 1;
                    PlayerHandlers[key].pp.AutoBidRemainder -= 1;
                
                if (!_tileController.NextBiddingTile.IsRisk && PlayerHandlers[key].pp.RiskSkips > 0)
                {
                    GetSneakyPlayerHandler(PlayerHandlers[key], PlayerHandlers[key].pp.TwitchID);
                    _tileController.BidHandler.BidRedemption((PlayerHandlers[key]), 1, BidType.ChannelPoints, "1", "1");
                }
            }
            
        }

    }

    public void CheckCountdown()
    {        
   /*     CheckNPCs();
        if (firstNPC == true)
        {
            _unitTesting.incrementUserId = false;
            _unitTesting.randomizeNameColor = false;
            _unitTesting.testUsername = "";
            _unitTesting.testUserId = "";
            _unitTesting.rewardTitle = "";
            _unitTesting.userInput = "";
            _unitTesting.rewardCost = 1;

    //        InitializeNPCs(0);
            EventCountdown = 1;
            firstNPC = false;
        }

        if (EventCountdown < 1)
        {
            RandomizeCountdown();
        }

        Debug.Log($"{EventCountdown}");
        EventCountdown -= 1;*/
    }

    public void RandomizeCountdown()
    {
            EventCountdown = 50;
    }

    public void InitializeNPCs(int ID)
    {
        if (ID == 0)
        {
            _unitTesting.incrementUserId = false;
            _unitTesting.testUsername = "GameMaster";
            _unitTesting.randomizeNameColor = true;
            _unitTesting.rewardCost = 1;
            _unitTesting.rewardTitle = "bid";
            _unitTesting.NPCReward();

            StartCoroutine(UpdateWaiter(ID));
        }
        else if (ID == 1)
        {
            _unitTesting.incrementUserId = false;
            _unitTesting.testUsername = "TinyDefender";
            _unitTesting.nameColor = Color.white;
            _unitTesting.randomizeNameColor = false;
            _unitTesting.rewardCost = 1;
            _unitTesting.rewardTitle = "bid";
            _unitTesting.NPCReward();
            _unitTesting.testUsername = "GameMaster";
            _unitTesting.userInput = $"!refundpoints @TinyDefender 1";
            _unitTesting.NPCMessage();

            StartCoroutine(UpdateWaiter(ID));            
        }

        _unitTesting.randomizeNameColor = false;
    }

    IEnumerator UpdateWaiter(int ID)
    {
        yield return new WaitForSeconds(1);
        
        UpdatePlayerHandler(ID);
    }

    IEnumerator GeneralWaiter(int duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public void UpdatePlayerHandler(int ID)
    {        
        string[] keys = PlayerHandlers.Keys.ToArray();
        foreach (string key in keys)
        {
            if (ID == 1)
            {
                PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                PlayerHandlers[key].pp.IsNPC = 1;
                PlayerHandlers[key].pp.ModeNPC = 1;
                PlayerHandlers[key].pp.StateNPC = 0;
            }
            else if (ID == 0)
            {
                PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                PlayerHandlers[key].pp.IsNPC = 1;
                PlayerHandlers[key].pp.ModeNPC = 2;
                PlayerHandlers[key].pp.StateNPC = 0;
            }
        }
    }

    public void CheckNPCs()
    {
        string[] keys = PlayerHandlers.Keys.ToArray();
        foreach (string key in keys)
        {
            if (PlayerHandlers[key].pp.IsNPC == 1)
            {
                if (PlayerHandlers[key].pp.ModeNPC == 1)
                {
                    Debug.Log($"{PlayerHandlers[key].pp.TwitchUsername} {PlayerHandlers[key].pp.StateNPC}");
                    if (PlayerHandlers[key].pp.StateNPC == 0)
                    {
                        PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                        _unitTesting.testUsername = PlayerHandlers[key].pp.TwitchUsername;
                        _unitTesting.rewardCost = 1;
                        _unitTesting.rewardTitle = "bid";
                        _unitTesting.RedeemReward();
                    }
                }
                else if (PlayerHandlers[key].pp.ModeNPC == 2)
                {
                    Debug.Log($"{PlayerHandlers[key].pp.TwitchUsername} {PlayerHandlers[key].pp.StateNPC}");
                    if (PlayerHandlers[key].pp.StateNPC == 0)
                    {
                        PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                        PlayerHandlers[key].pp.StateNPC = 1;
                        _unitTesting.testUsername = PlayerHandlers[key].pp.TwitchUsername;
                        _unitTesting.userInput = "!attack";
                        _unitTesting.NPCMessage();
                    }
                    else if (PlayerHandlers[key].pp.StateNPC == 1)
                    {
                        if (PlayerHandlers[key].IsKing())
                        {
                            PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                            PlayerHandlers[key].pp.StateNPC = 2;
                            _unitTesting.testUsername = PlayerHandlers[key].pp.TwitchUsername;
                            _unitTesting.userInput = "Haha! Come and get me!";
                            _unitTesting.NPCMessage();
                        }
                        else
                        {
                            PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                            PlayerHandlers[key].pp.StateNPC = 0;
                            _unitTesting.testUsername = PlayerHandlers[key].pp.TwitchUsername;
                            _unitTesting.rewardCost = 1;
                            _unitTesting.rewardTitle = "bid";
                            _unitTesting.NPCReward();
                        }

                    }
                    else if (PlayerHandlers[key].pp.StateNPC == 2)
                    {
                        if (!PlayerHandlers[key].IsKing())
                        {
                            PlayerHandlers[key].pp.LastInteraction = DateTime.Now;
                            PlayerHandlers[key].pp.StateNPC = 3;
                            _unitTesting.testUsername = PlayerHandlers[key].pp.TwitchUsername;
                            _unitTesting.rewardCost = 1;
                            _unitTesting.rewardTitle = "bid";
                            _unitTesting.NPCReward();
                            _unitTesting.userInput = "Oh No, My Gold!";
                            _unitTesting.NPCMessage();
                            _goldDistributor.SpawnGoldFromEvent(PlayerHandlers[key].pp.Gold);
                            PlayerHandlers[key].pp.Gold = 0;
                        }
                    }
                }
            }




            /*
            Tiny
            Small
            Little
            Minor
            Moderate
            Average
            Medium
            Large
            Major
            Huge
            Massive
            Gigantic
            Enormous
             */
        }

    }


    private void SavePreviousLog()
    {
        // Get the path to the Player.log file
        string logFilePath = Path.Combine(Application.persistentDataPath, "Player.log");

        // Generate a new file name with a timestamp
        string newFileName = "PlayerLog_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";

        string oldLogsFolderPath = Path.Combine(Application.persistentDataPath, "OldLogs");
        if (!Directory.Exists(oldLogsFolderPath))
        {
            Directory.CreateDirectory(oldLogsFolderPath);
        }

        // Generate the full path for the new log file
        string newFilePath = Path.Combine(oldLogsFolderPath, newFileName);

        try
        {
            // Check if the Player.log file exists
            if (File.Exists(logFilePath))
            {
                // Copy the Player.log file to the new file name
                File.Copy(logFilePath, newFilePath);

                CLDebug.Inst.Log($"Player.log copied to: {newFilePath}");
            }
            else
            {
                Debug.LogWarning("Player.log file not found.");
            }
        }
        catch (IOException ex)
        {
            Debug.LogError($"Error copying the log file: {ex.Message}");
        }
    }

    public void UpdateDay(string day, string customday = "Hoopty!")
    {
        Debug.Log($"{day}");
        
        if (day == "Tuesday")
        {
            AppConfig.Monday = false;
            AppConfig.Tuesday = true;
            TileController.CheckRarityEvent();
            _event1Text.SetText("New Build Tuesday");
        }
        else if (day == "Wednesday")
        {
            AppConfig.Tuesday = false;
            AppConfig.Wednesday = true;
            _event1Text.SetText("Wildcard Wednesday");

        }
        else if (day == "Thursday")
        {
            AppConfig.Wednesday = false;
            AppConfig.Thursday = true;
            _event1Text.SetText("Throwback Thursday");

        }
        else if (day == "Friday")
        {
            AppConfig.Thursday = false;
            AppConfig.Friday = true;
            Debug.Log($"Friday = {AppConfig.Friday}");
            _event1Text.SetText("Friday Fortune");

        }
        else if (day == "Saturday")
        {
            AppConfig.Friday = false;
            AppConfig.Saturday = true;
            Debug.Log($"Friday = {AppConfig.Friday}");
            _event1Text.SetText("Standard Saturday");

        }
        else if (day == "Sunday")
        {
            _event1Text.SetText("Super Sunday");

            AppConfig.Saturday = false;
            AppConfig.Sunday = true;
            AppConfig.CheckHappyHour();
            Debug.Log($"Sunday = {AppConfig.Sunday}");
        }
        else if (day == "Monday")
        {
            _event1Text.SetText("Monday Mania");

            AppConfig.Sunday = false;
            AppConfig.Monday = true;
            AppConfig.CheckHappyHour();
            TileController.CheckRarityEvent();
            Debug.Log($"Sunday = {AppConfig.Sunday}");
            Debug.Log($"Monday = {AppConfig.Monday}");
        }
        else if (day == "Custom")
        {
            _event1Text.SetText(customday);
        }
    }

}
