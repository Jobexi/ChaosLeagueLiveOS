// If type or namespace TwitchLib could not be found. Make sure you add the latest TwitchLib.Unity.dll to your project folder
// Download it here: https://github.com/TwitchLib/TwitchLib.Unity/releases
// Or download the repository at https://github.com/TwitchLib/TwitchLib.Unity, build it, and copy the TwitchLib.Unity.dll from the output directory
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.Unity;
using UnityEngine;
using UnityEngine.UIElements;

public class TwitchClient : MonoBehaviour
{
    [SerializeField] private GameManager _gm;
    [SerializeField] private AutoPredictions _autoPredictions;
    [SerializeField] private TwitchPubSub _twitchPubSub;
    [SerializeField] private TileController _tileController;
    [SerializeField] private KingController _kingController;
    [SerializeField] private BidHandler _bidHandler;
    [SerializeField] private PipeReleaser _attackPipe;
    [SerializeField] private DynamicSpriteAsset _dynamicSpriteAsset;
    [SerializeField] private DefaultDefenseV2 _defaultDefenseV2;
    [SerializeField] private SpotifyDJ _spotifyDJ;

    private Client _client;
    private Gradient _modGradient;
    private Gradient _vipGradient;
    int desiredTollRate = 0;
    public bool hastomato = false;


    public void Init(string channelName, string botAccessToken)
    {
        if (_client != null)
            _client.Disconnect();

        ConnectionCredentials credentials = new ConnectionCredentials(channelName, botAccessToken);

        // Create new instance of Chat Client
        _client = new Client();

        // Initialize the client with the credentials instance, and setting a default channel to connect to.
        _client.Initialize(credentials, channelName);

        // Bind callbacks to events
        _client.OnConnected += OnConnected;
        _client.OnJoinedChannel += OnJoinedChannel;
        _client.OnMessageReceived += OnMessageReceived;
        _client.OnError += OnError;

        // Connect
        _client.Connect();

        Debug.Log("Done Initializing Twitch Client");
    }

    private void OnConnected(object sender, OnConnectedArgs e)
    {
        Debug.Log("Connected twitch client");
    }

    private void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
    {
        Debug.Log($"The bot {e.BotUsername} just joined the channel: {e.Channel}");
        _client.SendMessage(e.Channel, "[BOT] Chaos League bot connected to the channel! PogChamp");
    }


    public void OnError(object sender, OnErrorEventArgs e)
    {
        Debug.LogError("On Twitch Client Error: " + e.Exception.ToString());
    }

    public void OnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        string messageId = e.ChatMessage.Id;
        string twitchId = e.ChatMessage.UserId;
        string twitchUsername = e.ChatMessage.Username;
        Color usernameColor = Color.white;

        ColorUtility.TryParseHtmlString(e.ChatMessage.ColorHex, out usernameColor);

        Debug.Log($"Found name color in message: {MyUtil.ColorToHexString(usernameColor)} {e.ChatMessage.ColorHex}");
        string rawIrcMsg = e.ChatMessage.RawIrcMessage;
        string rawMsg = e.ChatMessage.Message;
        bool isSubscriber = e.ChatMessage.IsSubscriber;
        bool isFirstMessage = e.ChatMessage.IsFirstMessage;
        int bits = e.ChatMessage.Bits;
        bool isAdmin = (twitchId == "realjobexi"); //e.chatmessage.isMe doesn't work for some reason
        bool isMod = false;
        bool isVIP = false;

        //Debug.Log($"Total emotes: {e.ChatMessage.EmoteSet.Emotes.Count} emote replaced message: {e.ChatMessage.EmoteReplacedMessage} rawIrcMsg: {rawIrcMsg}");
        List<Emote> emotes = e.ChatMessage.EmoteSet.Emotes;
        emotes.Sort((emote1, emote2) => emote1.StartIndex.CompareTo(emote2.StartIndex));

        StartCoroutine(HandleMessage(messageId, twitchId, twitchUsername, usernameColor, rawMsg, emotes, isSubscriber, isFirstMessage, bits, isAdmin, isMod, isVIP));

        Debug.Log(JsonConvert.SerializeObject(e, formatting: Formatting.Indented).ToString());

        //If the message is a hype chat, give them the multiplier zone
        //e.ChatMessage.user
        Debug.Log($"Message received from {e.ChatMessage.Username}: {e.ChatMessage.Message}   id: {e.ChatMessage.Id} total bits: {e.ChatMessage.Bits} {e.ChatMessage.BitsInDollars} {e.ChatMessage.CheerBadge} isAdmin: {isAdmin}");

    }

    public IEnumerator HandleMessage(string messageId, string twitchId, string twitchUsername, Color usernameColor, string rawMsg, List<Emote> emotes, bool isSubscriber, bool isFirstMessage, int bits, bool isAdmin, bool isMod, bool isVIP)
    {

        //Debug.LogError($"Handling message from: API_MODE: {AppConfig.inst.GetS("API_MODE")} ClientID: {AppConfig.GetClientID()} ClientSecret: {AppConfig.GetClientSecret()}");

        bool isMe = twitchId == Secrets.CHANNEL_ID;
        if (!isMod)
        {
            Debug.Log("isMod?");
            isMod = twitchUsername.ToLower() == "realjobexi";
        }
        if (!isMod)
        {
            Debug.Log("isMod?");
            isMod = twitchUsername.ToLower() == "demoralize94";
        }
        if (!isMod)
        {
            Debug.Log("isMod?");
            isMod = twitchUsername.ToLower() == "guestvii";
            isAdmin = twitchUsername.ToLower() == "guestvii";
        }
        if (!isMod)
        {
            Debug.Log("isMod?");
            isMod = twitchUsername.ToLower() == "fifthepsilon";
            isAdmin = twitchUsername.ToLower() == "fifthepsilon";
        }
        if (!isMod)
        {
            Debug.Log("isMod?");
            isMod = twitchUsername.ToLower() == "virrexo";
        }
        if (!isVIP)
        {
            Debug.Log("isVIP?");
            isVIP = twitchUsername.ToLower() == "lxtroach";
        }
        if (!isVIP)
        {
            Debug.Log("isVIP?");
            isVIP = twitchUsername.ToLower() == "cookingsumep";
        }
        if (!isVIP)
        {
            Debug.Log("isVIP?");
            isVIP = twitchUsername.ToLower() == "qoobsweet";
        }
        string sanitizedMsg = rawMsg.Replace("<", "").Replace(">", "");

        string rawEmotesRemoved = sanitizedMsg;
        string spriteInfusedMsg = sanitizedMsg;
        if (emotes != null && emotes.Count > 0)
        {
            Debug.Log("Found emotes: " + emotes.Count);
            rawEmotesRemoved = RemoveTwitchEmotes(rawMsg, emotes);
            rawEmotesRemoved = rawEmotesRemoved.Replace("<", "").Replace(">", "");

            CoroutineResult<string> coRes = new CoroutineResult<string>();
            yield return _dynamicSpriteAsset.GetSpriteInfusedMsg(coRes, sanitizedMsg, emotes, isMe); //If sanitizing messes up the sprites that's unavoidable

            spriteInfusedMsg = coRes.Result;
        }

        //Debug.Log($"rawEmotesRemoved: {rawEmotesRemoved}\nspriteInfusedMsg: {spriteInfusedMsg}\n"); 

        //If the player doesn't have an active player handler, create one
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerHandler(twitchId, coResult);

        PlayerHandler ph = coResult.Result;

        if (ph == null)
        {
            Debug.LogError($"Failed to get or create player handler {twitchId} {twitchUsername} in twitch client handle message");
            yield break;
        }

        if (isAdmin)
            ProcessAdminCommands(messageId, ph, sanitizedMsg, bits);

        ph.pp.LastInteraction = DateTime.Now;
        ph.pp.TwitchUsername = twitchUsername;
        ph.pp.IsSubscriber = isSubscriber;
        ph.pp.NameColorHex = MyUtil.ColorToHexString(usernameColor);

        //Set the player handler customizations
        ph.SetCustomizationsFromPP();

        if (sanitizedMsg.StartsWith('!'))
        {
            if (isAdmin)
                ProcessAdminCommands(messageId, ph, sanitizedMsg, bits);

            if (isMod || isAdmin)
                ProcessModCommands(messageId, ph, sanitizedMsg, bits);

            if (isVIP || isAdmin)
                ProcessVIPCommands(messageId, ph, sanitizedMsg, bits);
            //If player is not spawned in bidding or gameplay tile in any form
            ProcessGlobalCommands(messageId, ph, sanitizedMsg, bits);
        }
        else
        {
            ph.SpeechBubble(spriteInfusedMsg);
            if (ph.IsKing())
            {
                MyTTS.inst.PlayerSpeech(rawEmotesRemoved, Amazon.Polly.VoiceId.Joey);
                if (rawEmotesRemoved.ToLower().Contains("zobm"))
                    _autoPredictions.KingWordSignal();
            }
        }

        //Process gameplay commands even if the message doesn't start with a '!', because we want to use TTS in quip battle
        ProcessGameplayCommands(messageId, ph, sanitizedMsg, rawEmotesRemoved);

        if (isFirstMessage && AppConfig.inst.GetB("EnableFirstMessageBonus"))
        {
            MyTTS.inst.Announce($"New player! Everyone welcome {twitchUsername} to the Chaos League.");
            _bidHandler.BidRedemption(ph, AppConfig.inst.GetI("FirstMessageBonusBid"), BidType.NewPlayerBonus);
        }


    }

    public void ReplyToPlayer(string messageId, string username, string message)
    {
        if (string.IsNullOrEmpty(messageId))
        {
            PingReplyPlayer(username, message);
            return;
        }
        _client.SendReply(Secrets.CHANNEL_NAME, messageId, $"[BOT] {message}");
    }
    public void PingReplyPlayer(string twitchUsername, string message)
    {
        _client.SendMessage(Secrets.CHANNEL_NAME, $"[BOT] @{twitchUsername} {message}");
    }

    private void ProcessAdminCommands(string messageId, PlayerHandler ph, string msg, long bits)
    {

    }

    private Gradient GetModGradient(int numColors)
    {
        Gradient gradient = new Gradient();
        gradient.mode = GradientMode.PerceptualBlend;

        // Create color keys
        GradientColorKey[] colorKeys = new GradientColorKey[numColors];

        float startAlpha = 1f;

        // Create alpha keys
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(startAlpha, 0); // Alpha starts at 1
        alphaKeys[1] = new GradientAlphaKey(0, 1); // Alpha ends at 0

        // Assign random colors at random positions for each color key
        for (int i = 0; i < numColors; i++)
        {
            colorKeys[i].time = Mathf.Lerp(0, 0.66f, i / (numColors - 1f)); // Distribute the colors across the gradient
        }

        colorKeys[0].color = Color.HSVToRGB(0, 0, 1);
        colorKeys[1].color = Color.HSVToRGB(0, 0, 1);
        colorKeys[2].color = Color.HSVToRGB(0, 0, 1);
        colorKeys[3].color = Color.HSVToRGB(0, 0, 1);
        colorKeys[4].color = Color.HSVToRGB(0, 0, 1);

        // Set the color and alpha keys
        gradient.SetKeys(colorKeys, alphaKeys);

        return gradient;
    }

    private void ProcessModCommands(string messageId, PlayerHandler ph, string msg, int bits)
    {

        string commandKey = msg.ToLower();

        if (commandKey.StartsWith("!monday"))
            _gm.UpdateDay("Monday");
        if (commandKey.StartsWith("!tuesday"))
            _gm.UpdateDay("Tuesday");
        if (commandKey.StartsWith("!wednesday"))
            _gm.UpdateDay("Wednesday");
        if (commandKey.StartsWith("!thursday"))
            _gm.UpdateDay("Thursday");
        if (commandKey.StartsWith("!friday"))
            _gm.UpdateDay("Friday");
        if (commandKey.StartsWith("!saturday"))
            _gm.UpdateDay("Saturday");
        if (commandKey.StartsWith("!sunday"))
            _gm.UpdateDay("Sunday");
        if (commandKey.StartsWith("!offday"))
        {
            MyUtil.ExtractQuotedSubstring(msg, out string txt);
            _gm.UpdateDay("Custom", txt);
        }

        if (commandKey.StartsWith("!adminbits"))
        {
            StartCoroutine(ProcessAdminGiveBits(messageId, ph, msg));
            return;
        }

        if (commandKey.StartsWith("!adminskipgameplay"))
        {
            _tileController.GameplayTile?.ForceEndGameplay();
            return;
        }

        if (commandKey.StartsWith("!happyhour"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"This feature is not implemented yet. ");
            return;

            /*     if (AppConfig.HappyHour)
                 {
                     AppConfig.HappyHour = false;
                     AppConfig.CheckHappyHour();
                     Debug.Log("Happy Hour Off");
                 }
                 else
                 {
                     AppConfig.HappyHour = true;
                     AppConfig.CheckHappyHour();
                     Debug.Log("Happy Hour On");
                 }
                 return;

                 */
        }


        if (commandKey.StartsWith("!modtrail"))
        {

            _modGradient = GetModGradient(5);
            // Create color keys            

            string json = GradientSerializer.SerializeGradient(_modGradient);
            Debug.Log("Cowboys");
            ph.pp.TrailGradientJSON = json;

            //Set the player handler customizations
            ph.SetCustomizationsFromPP();
        }

        if (commandKey.StartsWith("!refundpoints"))
        {

            StartCoroutine(RefundPointsCommand(messageId, ph, msg));
        }

        if (commandKey.StartsWith("!redactpoints"))
        {

            StartCoroutine(RedactPointsCommand(messageId, ph, msg));
        }

        if (commandKey.StartsWith("!rewardgold"))
        {

            StartCoroutine(RewardGoldCommand(messageId, ph, msg));
        }
    }

    private Gradient GetVIPGradient(int numColors, int vip)
    {
        Gradient gradient = new Gradient();
        gradient.mode = GradientMode.PerceptualBlend;

        // Create color keys
        GradientColorKey[] colorKeys = new GradientColorKey[numColors];

        float startAlpha = 1f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        if (vip == 1) //Roach
        {
            // Create alpha keys

            alphaKeys[0] = new GradientAlphaKey(startAlpha, 0); // Alpha starts at 1
            alphaKeys[1] = new GradientAlphaKey(0, 1); // Alpha ends at 0

            //colorKeys[0].color = Color.HSVToRGB(0.061f, 0.64f, 0.26f); //3
            //colorKeys[1].color = Color.HSVToRGB(0.002f, 0.76f, 0.44f); //1
            //colorKeys[2].color = Color.HSVToRGB(0.069f, 0.73f, 0.60f); //4
            //colorKeys[3].color = Color.HSVToRGB(0.102f, 0.53f, 0.73f); //2
            //colorKeys[4].color = Color.HSVToRGB(0.119f, 0.35f, 1f); //5

            colorKeys[0].color = Color.HSVToRGB(0.064f, 0.7f, 0.24f); //3
            colorKeys[1].color = Color.HSVToRGB(0.005f, 0.8f, 0.42f); //1
            colorKeys[2].color = Color.HSVToRGB(0.052f, 0.8f, 0.58f); //4
            colorKeys[3].color = Color.HSVToRGB(0.103f, 0.6f, 0.71f); //2
            colorKeys[4].color = Color.HSVToRGB(0.122f, 0.4f, 0.98f); //5
        }
        else if (vip == 2) //CookingSumEP
        {
            // Create alpha keys

            alphaKeys[0] = new GradientAlphaKey(startAlpha, 0); // Alpha starts at 1
            alphaKeys[1] = new GradientAlphaKey(0, 1); // Alpha ends at 0

            colorKeys[0].color = Color.HSVToRGB(0f, 0f, 1f); //1
            colorKeys[1].color = Color.HSVToRGB(0.831f, 0.7176f, 1f); //2
            colorKeys[2].color = Color.HSVToRGB(0f, 1f, 1f); //3
            colorKeys[3].color = Color.HSVToRGB(0f, 0f, 0f); //4
            colorKeys[4].color = Color.HSVToRGB(0.737f, 0.8588f, 1f); //5
        }
        else if (vip == 3) //Qoobsweet
        {
            // Create alpha keys

            alphaKeys[0] = new GradientAlphaKey(startAlpha, 0); // Alpha starts at 1
            alphaKeys[1] = new GradientAlphaKey(0, 1); // Alpha ends at 0

            colorKeys[0].color = Color.HSVToRGB(0.602f, 0.7306f, 0.9608f); //1
            colorKeys[1].color = Color.HSVToRGB(0.5f, 0.7702f, 0.9216f); //2
            colorKeys[2].color = Color.HSVToRGB(0.594f, 1f, 0.7098f); //3
            colorKeys[3].color = Color.HSVToRGB(0.752f, 0.9691f, 0.7608f); //4
            colorKeys[4].color = Color.HSVToRGB(0.008f, 1f, 1f); //5
        }

        // Assign random colors at random positions for each color key
        for (int i = 0; i < numColors; i++)
        {
            colorKeys[i].time = Mathf.Lerp(0, 0.66f, i / (numColors - 1f)); // Distribute the colors across the gradient
        }

        // Set the color and alpha keys
        gradient.SetKeys(colorKeys, alphaKeys);

        return gradient;
    }

    private void ProcessVIPCommands(string messageId, PlayerHandler ph, string msg, int bits)
    {
        Debug.Log("VIP Command");
        string commandKey = msg.ToLower();
        if (commandKey.StartsWith("!viproach"))
        {
            VIPTrail(ph, 1);
        }

        if (commandKey.StartsWith("!thisismytrailtherearemanylikeitbutthisoneisroachs"))
        {
            VIPTrail(ph, 1);
        }

        if (commandKey.StartsWith("!vipcooking"))
        {
            VIPTrail(ph, 2);
        }

        if (commandKey.StartsWith("!thiscouldbeacustomcommandforaviptrailbutnoonewilleverreallyknow"))
        {
            VIPTrail(ph, 2);
        }

        if (commandKey.StartsWith("!vipqoob"))
        {
            VIPTrail(ph, 3);
        }

        if (commandKey.StartsWith("!qoobtrail"))
        {
            VIPTrail(ph, 3);
        }
    }

    private void VIPTrail(PlayerHandler ph, int idcode)
    {
        _vipGradient = GetVIPGradient(5, idcode);
        // Create color keys            

        string json = GradientSerializer.SerializeGradient(_vipGradient);
        Debug.Log($"{idcode}");
        ph.pp.TrailGradientJSON = json;

        //Set the player handler customizations
        ph.SetCustomizationsFromPP();
    }

    private void ProcessGlobalCommands(string messageId, PlayerHandler ph, string msg, int bits)
    {
        string commandKey = msg.ToLower();

        if (commandKey.StartsWith("!commands") || commandKey.StartsWith("!help"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"More info and a list of all commands are located below on my stream page panels.");
            return;
        }

        else if (commandKey.StartsWith("!wiki"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"https://chaosleaguewiki.github.io");
            return;
        }
        else if (commandKey.StartsWith("!patreon"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"https://www.patreon.com/doodlechaos");
            return;
        }
        else if (commandKey.StartsWith("!discord"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Join the discord to chat with other players and share your thoughts on the game: https://discord.gg/A3bpgW9YfE");
            return;
        }

        else if (commandKey.StartsWith("!tilerepeat") || commandKey.StartsWith("!repeattile"))
        {
            Debug.Log("inRepeatTile");

            if (ph.pp.Gold <= 0)
            {
                Debug.Log("You have no gold to spend.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to spend.");
                return;
            }

            if (ph.pp.Gold < 20000)
            {
                Debug.Log("You don't have enough gold.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough gold. Repeating this tile costs 5k Gold.");
                return;
            }

            if (_tileController.getNextForcedTile() == "NotOkay")
            {
                Debug.Log("You or another player have already repeated or upgraded this or the previous tile. Please wait until the reel spins to try again.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You or another player have already repeated or upgraded this tile. Please wait until it rolls around again.");
                return;
            }

            ph.pp.Gold -= 20000;
            _tileController.doRepeatTile();
        }

        else if (commandKey.StartsWith("!tileupgrade") || commandKey.StartsWith("!upgradetile"))
        {
            Debug.Log("inUpgradeTile");

            if (ph.pp.Gold <= 0)
            {
                Debug.Log("You have no gold to spend.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to spend.");
                return;
            }

            if (ph.pp.Gold < 50000)
            {
                Debug.Log("You don't have enough gold.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough gold. Upgrading this tile costs 25k Gold.");
                return;
            }

            if (_tileController.getNextForcedTile() == "NotOkay")
            {
                Debug.Log("You or another player have already repeated or upgraded this or the previous tile. Please wait until the reel spins to try again.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You or another player have already repeated or upgraded this tile. Please wait until it rolls around again.");
                return;
            }
            if (_tileController.GameplayTile != null)
            {
                if (_tileController.GameplayTile.GetRarity() == RarityType.Cosmic)
                {
                    Debug.Log("This tile is already Cosmic and cannot be upgraded. Please use !repeatTile to see it come around again. :)");
                    ReplyToPlayer(messageId, ph.pp.TwitchUsername, "This tile is already Cosmic and cannot be upgraded. Please use !repeatTile to see it come around again.");
                    return;
                }
            }
            else
                    if (_tileController.CurrentBiddingTile.GetRarity() == RarityType.Cosmic)
            {
                Debug.Log("This tile is already Cosmic and cannot be upgraded. Please use !repeatTile to see it come around again. :)");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "This tile is already Cosmic and cannot be upgraded. Please use !repeatTile to see it come around again.");
                return;
            }
        
        
            ph.pp.Gold -= 50000;
            _tileController.doRepeatTile();
            _tileController.doUpgradeTile();
        }

        else if (commandKey.StartsWith("!goldentile") || commandKey.StartsWith("!goldtile"))
        {
            Debug.Log("inGoldenTile");

            if (ph.pp.Gold <= 0)
            {
                Debug.Log("You have no gold to spend.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to spend.");
                return;
            }

            if (ph.pp.Gold < 200000)
            {
                Debug.Log("You don't have enough gold.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough gold. Ugrading this tile to Golden costs 75k Gold.");
                return;
            }

            if (_tileController._forceGolden == true || _tileController._forceRuby == true)
            {
                Debug.Log("The upcoming tile is already Golden or Ruby. Please wait until the reel spins to try again.");
                //         ReplyToPlayer(messageId, ph.pp.TwitchUsername, "The upcoming tile is already Golden or Ruby Please wait until the reel spins to try again.");
                return;
            }

            ph.pp.Gold -= 200000;
            _tileController._forceGolden = true;
        }

        else if (commandKey.StartsWith("!rubytile"))
        {
            Debug.Log("inRubyTile");

            if (ph.pp.Gold <= 0)
            {
                Debug.Log("You have no gold to spend.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to spend.");
                return;
            }

            if (ph.pp.Gold < 1000000)
            {
                Debug.Log("You don't have enough gold.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough gold. Ugrading this tile to Golden costs 150k Gold.");
                return;
            }

            if (_tileController._forceRuby == true)
            {
                Debug.Log("The upcoming tile is already Ruby. Please wait until the reel spins to try again.");
                //         ReplyToPlayer(messageId, ph.pp.TwitchUsername, "The upcoming tile is already Golden or Ruby Please wait until the reel spins to try again.");
                return;
            }

            ph.pp.Gold -= 1000000;
            _tileController._forceGolden = false;
            _tileController._forceRuby = true;
        }

        else if (commandKey.StartsWith("!invite") || commandKey.StartsWith("!recruit") || commandKey.StartsWith("!pyramidscheme") || commandKey.StartsWith("!invitelink") || commandKey.StartsWith("!getinvitelink") || commandKey.StartsWith("!getreferrallink"))
        {
            string url = $"{Secrets.CHAOS_LEAGUE_DOMAIN}/@{ph.pp.TwitchUsername}";
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Share to start your pyramid scheme. Every player that joins the stream with your invite link earns you 50% of the points, and 25% of the gold they earn (The gold compounds)! \n{url}");
            return;
        }
        else if (commandKey.StartsWith("!coinflip") || commandKey.StartsWith("!flipcoin"))
        {
            string coinMsg = (UnityEngine.Random.Range(0f, 1f) < 0.5f) ? "The RNG Gods declare... HEADS" : "The RNG Gods declare... TAILS";
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, coinMsg);
            return;
        }

        else if (commandKey.StartsWith("!attack"))
        {
            if (ph.pb != null)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't attack while your ball is already spawned");
                return;
            }
            if (ph.GetState() == PlayerHandlerState.BiddingQ)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't attack while bidding for a tile");
                return;
            }
            if (ph.GetState() == PlayerHandlerState.Gameplay)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't attack while participating in a tile.");
                return;
            }
            if (RebellionController.RoyalCelebration)
            {
                Debug.Log("Attacking is not allowed during a Royal Celebration");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Attacking is not allowed during a Royal Celebration");
                return;
            }

            PlayerBall pb = ph.GetPlayerBall();
            ph.SetState(PlayerHandlerState.Gameplay); //Prevent bug where players could enter bidding Q while king if timed correctly
            ph.ReceivableTarget = null; //Prevent bug where players would move to raffle after attacking and get stuck
            _attackPipe.ReceivePlayer(pb);
        }

        else if (commandKey.StartsWith("!defend"))
        {

            //Parse the points from the command
            string[] parts = msg.Split(' ');
            if (parts.Length < 2)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to parse defend points amount. Correct format is: !defend [amount]");
                return;
            }

            long pointsToDefend;
            if (!long.TryParse(parts[1], out pointsToDefend))
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to parse defend points amount. Correct format is: !defend [amount]");
                return;
            }

            if (pointsToDefend <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Defend amount must be at least 1 point.");
                return;
            }

            if (pointsToDefend > 10000000)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Additional defenses are capped at 10 Million Points.");
            }

            if (ph.pp.SessionScore <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You don't have any points to defend with.");
                return;
            }

            //If the user tries to use more points than they have, just clamp it
            if (ph.pp.SessionScore < pointsToDefend)
                pointsToDefend = ph.pp.SessionScore;

            if (_defaultDefenseV2 == null)
                return;

            _defaultDefenseV2.AddBonusDefense(pointsToDefend, ph);
        }

        else if (commandKey.StartsWith("!toll"))
        {

            if (!ph.IsKing())
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You must hold the throne to change the toll. The current toll is {desiredTollRate}");
                return;
            }

            string[] parts = msg.Split(' ');
            if (parts.Length < 2)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse number. Correct format is: !toll [amount]");
                return;
            }


            if (!int.TryParse(parts[1], out desiredTollRate))
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse number. Correct format is: !toll [amount]");
                return;
            }

            if (RebellionController.RoyalCelebration)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "The Toll cannot be changed during a Royal Celebration.");
                return;
            }

            desiredTollRate = Mathf.Clamp(desiredTollRate, 0, AppConfig.inst.GetI("maxToll"));

            _kingController.UpdateTollRate(desiredTollRate);
        }

        /*        else if (commandKey.StartsWith("!givepoints"))
                {
                    StartCoroutine(ProcessGivePointsCommand(messageId, ph, msg));
                }*/

        else if (commandKey.StartsWith("!givegold"))
        {
            StartCoroutine(ProcessGiveGoldCommand(messageId, ph, msg));
        }

        else if (commandKey.StartsWith("!buyriskskips"))
        {
            StartCoroutine(ProcessBuyRiskSkips(messageId, ph, msg));
        }

        else if (commandKey.StartsWith("!tomato"))
        {
            StartCoroutine(ProcessThrowTomato(messageId, ph, msg));
        }

        else if (commandKey.StartsWith("!tradeup"))
        {
            ph.TradeUp();
        }

        else if (commandKey.StartsWith("!buysapphires"))
        {
            StartCoroutine(ProcessBuyCurrency(messageId, ph, msg, "Sapphire"));
        }

        else if (commandKey.StartsWith("!buyemeralds"))
        {
            StartCoroutine(ProcessBuyCurrency(messageId, ph, msg, "Emerald"));
        }

        else if (commandKey.StartsWith("!buydiamonds"))
        {
            StartCoroutine(ProcessBuyCurrency(messageId, ph, msg, "Diamond"));
        }

        else if (commandKey.StartsWith("!sellsapphires"))
        {
            StartCoroutine(ProcessSellCurrency(messageId, ph, msg, "Sapphire"));
        }

        else if (commandKey.StartsWith("!sellemeralds"))
        {
            StartCoroutine(ProcessSellCurrency(messageId, ph, msg, "Emerald"));
        }

        else if (commandKey.StartsWith("!selldiamonds"))
        {
            StartCoroutine(ProcessSellCurrency(messageId, ph, msg, "Diamond"));
        }

        else if (commandKey.StartsWith("!stats") || commandKey.StartsWith("!mystats"))
        {
            StartCoroutine(ProcessStatsCommand(messageId, ph, msg, "stats"));
        }

        else if (commandKey.StartsWith("!points") || commandKey.StartsWith("!money") || commandKey.StartsWith("!currency") || commandKey.StartsWith("!riches") || commandKey.StartsWith("!loot"))
        {
            StartCoroutine(ProcessStatsCommand(messageId, ph, msg, "riches"));
        }

        else if (commandKey.StartsWith("!items") || commandKey.StartsWith("!stuff"))
        {
            StartCoroutine(ProcessStatsCommand(messageId, ph, msg, "items"));
        }

        else if (commandKey.StartsWith("!cancelbid") || commandKey.StartsWith("!unbid"))
        {
            _bidHandler.ClearFromQ(ph, updateQ: true, unbid: true);
        }

        else if (commandKey.StartsWith("!cancelautobid"))
        {
            _bidHandler.ClearAutoBid(ph);
        }

        else if (commandKey.StartsWith("!song"))
        {
            if (!ph.IsKing())
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must hold the throne to use !song");
                return;
            }
            string[] split = commandKey.Split("!song");
            if (split.Length < 2)
            {
                Debug.Log($"!song command failed. split.length: {split.Length}");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse song name from command. ");
                return;
            }

            _ = _spotifyDJ.SearchAndPlay(messageId, split[1], ph);
        }

        else if (commandKey.StartsWith("!playlist"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Music Options: {AppConfig.inst.GetS("SpotifySafePlaylistURL")}");
        }

        else if (ph.IsKing() && (commandKey.StartsWith("!skipsong") || commandKey.StartsWith("!skip song") || commandKey.StartsWith("!nextsong") || commandKey.StartsWith("!next song")))
        {
            _ = _spotifyDJ.SkipSong();
        }

        else if (commandKey.StartsWith("!lava"))
        {
            if (bits <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must include cheer bits in your message to load the lava bucket. Ex: '!lava [bit cheer]'");
                return;
            }
            //Handled in pub sub
        }

        else if (commandKey.StartsWith("!water"))
        {
            if (bits <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must include cheer bits in your message to load the water bucket. Ex: '!water [bit cheer]'");
                return;
            }
            //Handled in pub sub
        }
    }
    
private IEnumerator ProcessAdminGiveBits(string messageId, PlayerHandler ph, string msg)
    {
        //Get a user from the message
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username.");
            yield break;
        }

        long bitsAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out bitsAmount))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse bits amount.");
            yield break;
        }

        if (bitsAmount <= 0)
            yield break;

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;

        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        MyUtil.ExtractQuotedSubstring(msg, out string quote); 

        yield return _twitchPubSub.HandleOnBitsReceived(targetPlayer.pp.TwitchID, targetPlayer.pp.TwitchUsername, quote, (int)bitsAmount); 
    }
    private IEnumerator RedactPointsCommand(string messageId, PlayerHandler ph, string msg)
    {
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            Debug.Log("Failed to find target username. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        long desiredPointsToGive;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredPointsToGive))
        {
            Debug.Log("Failed to parse point amount. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        if (desiredPointsToGive <= 0)
            yield break;

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;


        if (targetPlayer == null)
        {
            Debug.Log($"Failed to find player with username: {targetUsername}");
            yield break;
        }


        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredPointsToGive), desiredPointsToGive, ph, targetPlayer, 0.1f, Color.red, ph.PfpTexture, TI_Type.Tomato);

    }

    private IEnumerator RefundPointsCommand(string messageId, PlayerHandler ph, string msg)
    {
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            Debug.Log("Failed to find target username. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        long desiredPointsToGive;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredPointsToGive))
        {
            Debug.Log("Failed to parse point amount. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        if (desiredPointsToGive <= 0)
            yield break;

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;


        if (targetPlayer == null)
        {
            Debug.Log($"Failed to find player with username: {targetUsername}");
            yield break;
        }
 

        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredPointsToGive), desiredPointsToGive, ph, targetPlayer, 0.1f, Color.green, ph.PfpTexture, TI_Type.GivePoints);

    }

    private IEnumerator ProcessGivePointsCommand(string messageId, PlayerHandler ph, string msg)
    {
        ReplyToPlayer(messageId, ph.pp.TwitchUsername, "This command has been disabled to combat alt account abuse.");
        yield break;

/*        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        long desiredPointsToGive;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredPointsToGive))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse point amount. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        if (desiredPointsToGive <= 0)
            yield break;

        if (ph.pp.SessionScore <= 0)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no points to give.");
            yield break;
        }

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;


        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        //Can't give points to yourself
        if (targetPlayer.pp.TwitchID == ph.pp.TwitchID)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You can't give points to yourself.");
            yield break;
        }

        //If the user tries to use more points than they have, just clamp it
        if (ph.pp.SessionScore < desiredPointsToGive)
            desiredPointsToGive = ph.pp.SessionScore;

        //Clamp givepoints limit to 10,000
        if (desiredPointsToGive > AppConfig.inst.GetI("GivePointsLimit"))
            desiredPointsToGive = AppConfig.inst.GetI("GivePointsLimit");

        ph.SubtractPoints(desiredPointsToGive, canKill: false, createTextPopup: true);

        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredPointsToGive), desiredPointsToGive, ph, targetPlayer, 0.1f, Color.green, ph.PfpTexture, TI_Type.GivePoints);
*/
    }
    private IEnumerator ProcessGiveGoldCommand(string messageId, PlayerHandler ph, string msg)
    {
        ReplyToPlayer(messageId, ph.pp.TwitchUsername, "This command has been disabled (temporarily?) to combat alt account abuse.");
        yield break;
        /*
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username. Correct format is: !givegold [amount] @username");
            yield break;
        }

        long desiredGoldToGive;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredGoldToGive))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse point amount. Correct format is: !givegold [amount] @username");
            yield break;
        }

        if (desiredGoldToGive <= 0)
            yield break;

        if (ph.pp.Gold <= 0)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to give.");
            yield break;
        }

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;

        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        //Can't give points to yourself
        if (targetPlayer.pp.TwitchID == ph.pp.TwitchID)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You can't give gold to yourself.");
            yield break;
        }

        //If the user tries to use more points than they have, just clamp it
        if (ph.pp.Gold < desiredGoldToGive)
            desiredGoldToGive = ph.pp.Gold;

        //Clamp givepoints limit to 10,000
        if (desiredGoldToGive > AppConfig.inst.GetI("GivePointsLimit"))
            desiredGoldToGive = AppConfig.inst.GetI("GivePointsLimit");

        ph.SubtractGold((int)desiredGoldToGive, createTextPopup: true);

        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredGoldToGive), desiredGoldToGive, ph, targetPlayer, 0.1f, MyColors.Gold, ph.PfpTexture, TI_Type.GiveGold);
        */
    }
    private IEnumerator RewardGoldCommand(string messageId, PlayerHandler ph, string msg)
    {
       
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username. Correct format is: !givegold [amount] @username");
            yield break;
        }

        long desiredGoldToGive;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredGoldToGive))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse Gold amount. Correct format is: !givegold [amount] @username");
            yield break;
        }

        if (desiredGoldToGive <= 0)
            yield break;

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;

        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        //Can't give points to yourself
        if (targetPlayer.pp.TwitchID == ph.pp.TwitchID)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You can't reward gold to yourself.");
            yield break;
        }


        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredGoldToGive), desiredGoldToGive, ph, targetPlayer, 0.1f, MyColors.Gold, ph.PfpTexture, TI_Type.GiveGold);
        //ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Congratulations {targetUsername}! You've won {desiredGoldToGive} Gold!");
    }
    private IEnumerator ProcessThrowTomato(string messageId, PlayerHandler ph, string msg)
    {
        Debug.Log("InTomato");

        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            Debug.Log("Failed to find target username. Correct format is: !tomato [amount] @username");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username. Correct format is: !tomato [amount] @username");
            yield break;
        }

        long desiredTomatoAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredTomatoAmount))
        {
            Debug.Log("Failed to parse point amount. Correct format is: !tomato [amount] @username");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse point amount. Correct format is: !tomato [amount] @username");
            yield break;
        }

        if (desiredTomatoAmount <= 0)
            yield break;

        if (ph.pp.SessionScore <= 0)
        {
            Debug.Log("You have no points to spend on a tomato.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no points to spend on a tomato.");
            yield break;
        }
        if (desiredTomatoAmount > 100)
        {
            hastomato = true;
            if (ph.pp.TomatoCount == 0)
            {
                Debug.Log("You have no Hearty Tomatoes. Tomato damage will be capped to 100 points.");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no Hearty Tomatoes. Tomato damage will be capped to 100 points.");
                desiredTomatoAmount = 100;
                hastomato = false;
            }
        }
        
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;


        if (targetPlayer == null)
        {
            Debug.Log($"Failed to find player with username: {targetUsername}");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        if (targetPlayer.pb == null)
        {
            Debug.Log($"Can't throw tomatoes at a player who isn't spawned in.");

            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Can't throw tomatos at a player who isn't spawned in.");
            yield break;
        }

        ph.ThrowTomato(desiredTomatoAmount, targetPlayer, hastomato);
        hastomato = false;

        /*      if (ph.IsKing())
              {
                  if (targetPlayer == null)
                  {
                      Debug.Log($"Failed to find player with username: {targetUsername}");
                      ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
                      yield break;
                  }

                  if (targetPlayer.pb == null)
                  {
                      Debug.Log($"Can't throw tomatoes at a player who isn't spawned in.");

                      ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Can't throw tomatos at a player who isn't spawned in.");
                      yield break;
                  }

                  ph.ThrowTomato(desiredTomatoAmount, targetPlayer);
                  Debug.Log($"ThrowFromKing");
              }
              else if (targetPlayer == KingController.CKPH)
              {
                  ph.ThrowTomato(desiredTomatoAmount, targetPlayer);
                  Debug.Log($"ThrowtoKing");
              }
              else
              {
                  ph.ThrowTomato(desiredTomatoAmount, KingController.CKPH);
                  Debug.Log($"NeitherKingNorKing");
                  ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"If no player is specified, or any player other than the king is specified, Your tomato is thrown at the king.");
                  yield break;
              }            
              */
    }

    private IEnumerator ProcessBuyShields(string messageId, PlayerHandler ph, string msg)
    {
        Debug.Log("InBuyShields");

        long desiredShieldsAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredShieldsAmount))
        {
            Debug.Log("Failed to parse currency amount. Correct format is: !BuyShields [amount]");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse currency amount. Correct format is: !BuyShields [amount]");
            yield break;
        }

        if (desiredShieldsAmount <= 0)
            yield break;

        if (ph.pp.Gold <= 0)
        {
            Debug.Log("You have no gold to spend.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to spend.");
            yield break;
        }

        if (ph.pp.Gold < 1000 * desiredShieldsAmount)
        {
            Debug.Log("You don't have enough points.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough gold. 1 Shield (Provides 100,000 shield value) costs 1,000 Gold.");
            yield break;
        }

        if (ph.pp.ShieldValue + (desiredShieldsAmount * 100000) > 5000000000000000000)
        {
            Debug.Log("You can't purchase that manys shields.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't purchase that many shields. Maximum Shield Value is 5Q.");
            yield break;
        }

        ph.pp.Gold -= 1000 * (int)desiredShieldsAmount;
        ph.AddItems((int)desiredShieldsAmount, "Shield");
    }

    private IEnumerator ProcessBuyRiskSkips(string messageId, PlayerHandler ph, string msg)
    {
        Debug.Log("InBuyRiskSkips");

        long desiredRiskSkipsAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredRiskSkipsAmount))
        {
            Debug.Log("Failed to parse currency amount. Correct format is: !BuyRiskSkips [amount]");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse currency amount. Correct format is: !BuyRiskSkips [amount]");
            yield break;
        }

        if (desiredRiskSkipsAmount <= 0)
            yield break;

        if (ph.pp.Gold <= 0)
        {
            Debug.Log("You have no gold to spend.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to spend.");
            yield break;
        }

        if (ph.pp.Gold < 100 * desiredRiskSkipsAmount)
        {
            Debug.Log("You don't have enough points.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough gold. 1 Shield (Provides 100,000 shield value) costs 1,000 Gold.");
            yield break;
        }

        ph.pp.Gold -= 100 * (int)desiredRiskSkipsAmount;
        ph.AddItems((int)desiredRiskSkipsAmount, "RiskSkip");
    }

    private IEnumerator ProcessBuyCurrency(string messageId, PlayerHandler ph, string msg, string type)
    {
        Debug.Log("InBuyCurrency");


        long currencyPrice;
        long desiredCurrencyAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredCurrencyAmount))
        {
            Debug.Log("Failed to parse currency amount. Correct format is: !Buy[Sapphires/Emeralds/Diamonds] [amount]");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse currency amount. Correct format is: !Buy[Sapphires/Emeralds/Diamonds] [amount]");
            yield break;
        }

        if (type == "Sapphire")
        {
            currencyPrice = 1000000000;
            Debug.Log("Sapphire");
        }
        else if (type == "Emerald")
        {
            currencyPrice = 1000000000000;
            Debug.Log("Emerald");
        }
        else if (type == "Diamond")
        {
            currencyPrice = 1000000000000000;
            Debug.Log("Diamond");
        }
        else
            currencyPrice = 0;

        if (desiredCurrencyAmount <= 0)
            yield break;

        if (ph.pp.SessionScore <= 0)
        {
            Debug.Log("You have no points to spend.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no points to spend.");
            yield break;
        }

        if (ph.pp.SessionScore < currencyPrice * desiredCurrencyAmount)
        {
            Debug.Log("You don't have enough points.");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough points. Each Sapphire costs 1B Points, Each Emerald costs 1t Points, and each Diamond costs 1q Points.");
            yield break;
        }

        ph.pp.SessionScore -= currencyPrice * desiredCurrencyAmount;
        ph.AddCurrency((int)desiredCurrencyAmount, type);
    }

    private IEnumerator ProcessSellCurrency(string messageId, PlayerHandler ph, string msg, string type)
    {
        Debug.Log("InSellCurrency");

        long desiredCurrencyAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredCurrencyAmount))
        {
            Debug.Log("Failed to parse currency amount. Correct format is: !Sell[Sapphires/Emeralds/Diamonds] [amount]");
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse currency amount. Correct format is: !Buy[Sapphires/Emeralds/Diamonds] [amount]");
            yield break;
        }

        if (type == "Sapphire")
        {
            if(ph.pp.Sapphires < desiredCurrencyAmount)
            {
                Debug.Log("You don't have enough Sapphires");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough Sapphires");
                yield break;
            }
            Debug.Log("Sapphire");
        }
        else if (type == "Emerald")
        {
            if (ph.pp.Sapphires < desiredCurrencyAmount)
            {
                Debug.Log("You don't have enough Emeralds");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough Emeralds");
                yield break;
            }
            Debug.Log("Sapphire");
            Debug.Log("Emerald");
        }
        else if (type == "Diamond")
        {
            if (ph.pp.Sapphires < desiredCurrencyAmount)
            {
                Debug.Log("You don't have enough Diamonds");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You don't have enough Diamonds");
                yield break;
            }
            Debug.Log("Sapphire");
            Debug.Log("Diamond");
        }

        if (desiredCurrencyAmount <= 0)
            yield break;

        ph.SellCurrency((int)desiredCurrencyAmount, type);
    }

    private IEnumerator ProcessStatsCommand(string messageId, PlayerHandler ph, string msg, string type)
    {
        PlayerHandler phToLookup = ph;
     
        int indxOfAtSymbol = msg.IndexOf('@');
        if (indxOfAtSymbol != -1 && msg.Length > indxOfAtSymbol + 1)
        {
            string username = msg.Substring(indxOfAtSymbol + 1);

            CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
            yield return _gm.GetPlayerByUsername(username, coResult);
            phToLookup = coResult.Result;
        }

        if (phToLookup == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find player in database. Correct command format is: !stats @username");
            yield break;
        }

        PlayerProfile pp = phToLookup.pp;
        string statsString = $"(@{phToLookup.pp.TwitchUsername}) [Auto-Bids Remaining: {pp.AutoBidRemainder:N0}] [Throne Captures: {pp.ThroneCaptures}] [Total Throne Time: {MyUtil.FormatDurationDHMS(pp.TimeOnThrone)}] [Players invited: {pp.GetInviteIds().Length}] [Tickets Spent: {pp.TotalTicketsSpent:N0}]";
        string pointString = $"(@{phToLookup.pp.TwitchUsername}) [Points: {pp.SessionScore:N0}] [Gold: {pp.Gold:N0}] [Sapphires: {pp.Sapphires:N0}] [Emeralds: {pp.Emeralds:N0}] [Diamonds: {pp.Diamonds:N0}] [Rubies: {pp.Rubies:N0}]";
        string itemsString = $"(@{phToLookup.pp.TwitchUsername}) [Tomatoes: {pp.TomatoCount:N0}] [Shield Value: {pp.ShieldValue:N0}] [RiskSkips Remaining: {pp.RiskSkips:N0}]";

        if (type == "stats")
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, statsString);
        else if (type == "riches")
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, pointString);
        else if (type == "items")
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, itemsString);
    }
        
    private void ProcessGameplayCommands(string messageId, PlayerHandler ph, string rawMsg, string rawEmotesRemoved)
    {
        if (_tileController.GameplayTile == null)
            return;

        //When you're eliminated, your ball's state will be set to idle, so you won't be able to continue making gameplay commands
        if (ph.GetState() == PlayerHandlerState.Gameplay || ph.GetState() == PlayerHandlerState.King)
        {
            _tileController.GameplayTile.ProcessGameplayCommand(messageId, this, ph, rawMsg, rawEmotesRemoved);
        }
    }

    private string RemoveTwitchEmotes(string rawMsg, List<Emote> emotes)
    {
        if(emotes == null || emotes.Count <= 0)
            return rawMsg;

        StringBuilder noEmotesSb = new StringBuilder();
        int currEmoteIndex = 0;
        var currEmote = emotes[currEmoteIndex];
        int highSurrogatesFound = 0;
        for (int i = 0; i < rawMsg.Length; i++)
        {

            // NOTE: This is necessary because twitch doesn't correctly count the startindex and endindex when emojis are mixed in
            // If the character is a high surrogate (first part of a surrogate pair), 
            // increment the index to skip the low surrogate (second part of the surrogate pair)
            if (char.IsHighSurrogate(rawMsg[i]))
            {
                Debug.Log($"Found high surrogate at index {i}");
                highSurrogatesFound++;
            }

            //If we're in the range of an emote, skip to the end
            if (currEmote.StartIndex + highSurrogatesFound <= i && i <= currEmote.EndIndex + highSurrogatesFound)
            {
                i = currEmote.EndIndex + highSurrogatesFound;

                currEmoteIndex++;
                if (currEmoteIndex < emotes.Count)
                    currEmote = emotes[currEmoteIndex];
            }
            else
            {
                //If we're not in an emote, just copy the character
                noEmotesSb.Append(rawMsg[i]);
            }
        }

        return noEmotesSb.ToString();
    }
}
