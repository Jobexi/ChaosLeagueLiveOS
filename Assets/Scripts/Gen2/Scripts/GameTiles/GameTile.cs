using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public enum RarityType { Common, Rare, Epic, Legendary, Mythic, Ethereal, Cosmic, CommonPlus, RarePlus, EpicPlus, LegendaryPlus, MythicPlus, EtherealPlus, CosmicPlus }
public enum DurationTYpe { Timer, Manual }
public enum Side { Left, Center, Right}
public enum TileState { Inactive, LockedInPos, Bidding, Gameplay}

public class GameTile : MonoBehaviour
{
    private TileController _tc;
    public int TileIDNum;
    [SerializeField] private Game _game;
    [SerializeField] private SuddenDeath _suddenDeath;
    [SerializeField] private CleaningBarController _cleaningBarController;
    [SerializeField] private Transform _releaseBar;
    [SerializeField] private TextMeshPro _tileNameText;
    [SerializeField] public TextMeshPro _rarityText;
    [SerializeField] private TextMeshPro _timerText;
    [SerializeField] public TextMeshPro _ticketBonusAmountText;
    [SerializeField] public TextMeshPro _indicator1;
    [SerializeField] public TextMeshPro _indicator2;
    public int TicketBonusAmount;

    public TileState TileState = TileState.Inactive;
    public RarityType RarityType;
    [SerializeField] public DurationTYpe DurationTYpe;
    [SerializeField] public bool IsGolden; //1% chance
    [SerializeField] public bool IsRuby; //0.01% chance
    [SerializeField] public bool IsCurse; //Special
    [SerializeField] public bool IsMystery; //Special
    [SerializeField] public bool IsShop;
    [SerializeField] public bool IsRisk;
    [SerializeField] public bool IsKing;
    [SerializeField] public bool HasBackground;
    [SerializeField] public bool BuyingActive;
    [SerializeField] private CycleMode _cycleMode;
    [SerializeField] private bool _updateCycleModeButton;
    [SerializeField] private ContactWarp _contactWarp;

    [SerializeField] private Transform _resetablesRoot;

    [SerializeField] public MeshRenderer _background;
    [SerializeField] private List<Material> _bgOptions;
    [SerializeField] private List<Material> _bgOptionsPlus;

    [SerializeField] private int _tileDurationS;

    [Range(0, 8)]
    [SerializeField] public int MinAuctionSlots;
    [Range(0, 8)]
    [SerializeField] public int MaxAuctionSlots;
    [Range(0, 8)]
    [SerializeField] public int RaffleSlots;
    [SerializeField] public int AuctionDuration = 60;

    [SerializeField] private bool finishTileButton;
    [SerializeField] public PipeReleaser EntrancePipe;
    [SerializeField] private GoldenVisuals _goldenVisuals;
    [SerializeField] private List<MeshRenderer> _colorTrimsByRarity;

    public List<PlayerHandler> ConveyorBelt = new List<PlayerHandler>();

    private bool _forceEndGameplay = false;
    private float _tileGameplayTimeElapsed = 0;
    private float _timer;

    static public int GoldenSpids;
    //public bool TileActive;


    private MaterialPropertyBlock _mpb;
    private MaterialPropertyBlock _trimMpb;

    private int MysteriousAnnouncer;

    [SerializeField] [HideInInspector] private Color _backgroundStartColor;
    [SerializeField] [HideInInspector] private Color _backgroundEndColor;
    [SerializeField] [HideInInspector] private Color _trimColor;

    public PBEffector[] Effectors;

    public Side CurrentSide;

    //public int TotalPlayers;
    public List<PlayerHandler> Players = new List<PlayerHandler>();
    public List<PlayerHandler> AlivePlayers = new List<PlayerHandler>();
    public List<PlayerHandler> EliminatedPlayers = new List<PlayerHandler>();

    [Header("Wait for All Players Released Before Starting Game")]
    [SerializeField] private bool _waitForAll = true;
    [SerializeField] private bool _waitForAllDead = false;

    //private int _playersReleased = 0;

    private DateTime _tileStartTime;
    private long _playerPointsSumStart;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _trimMpb = new MaterialPropertyBlock();

        //SetBackgroundShader(1);

        _background.GetPropertyBlock(_mpb);
        _mpb.SetColor("_StartColor", _backgroundStartColor);
        _mpb.SetColor("_EndColor", _backgroundEndColor);
        _background.SetPropertyBlock(_mpb);

        _trimMpb.SetColor("_BaseColor", _trimColor);
        foreach (var meshRenderer in _colorTrimsByRarity)
            meshRenderer.SetPropertyBlock(_trimMpb);

        SetBackground();

        /*        foreach (var resetable in _resetablesRoot.GetComponentsInChildren<IResetable>())
                    resetable.MyReset();*/

        Effectors = GetComponentsInChildren<PBEffector>();

        if(!IsKing)
        _tileNameText.SetText(gameObject.name.Replace("(Clone)", ""));
    }

    public void SetRarity(RarityType rarityType, Color backgroundStartColor, Color backgroundEndColor, Color trimColor)
    {
        RarityType = rarityType;

        _rarityText.SetText(System.Enum.GetName(typeof(RarityType), rarityType));

        _backgroundStartColor = backgroundStartColor;
        _backgroundEndColor = backgroundEndColor;
        _trimColor = trimColor;

    }

    public void SetBackground()
    {
        var BaseMaterials = _background.materials;
        var DesiredMaterials = _bgOptions;
        var MaterialsPlus = _bgOptionsPlus;

        if (!IsKing)
        {
            switch (GetRarity())
            {
                case RarityType.Common:
                    BaseMaterials[0] = DesiredMaterials[0];
                    break;
                case RarityType.Rare:
                    BaseMaterials[0] = DesiredMaterials[1];
                    break;
                case RarityType.Epic:
                    BaseMaterials[0] = DesiredMaterials[2];
                    break;
                case RarityType.Legendary:
                    BaseMaterials[0] = DesiredMaterials[3];
                    break;
                case RarityType.Mythic:
                    BaseMaterials[0] = DesiredMaterials[4];
                    break;
                case RarityType.Ethereal:
                    BaseMaterials[0] = DesiredMaterials[5];
                    break;
                case RarityType.Cosmic:
                    BaseMaterials[0] = DesiredMaterials[6];
                    break;
                case RarityType.CommonPlus:
                    BaseMaterials[0] = MaterialsPlus[0];
                    break;
                case RarityType.RarePlus:
                    BaseMaterials[0] = MaterialsPlus[1];
                    break;
                case RarityType.EpicPlus:
                    BaseMaterials[0] = MaterialsPlus[2];
                    break;
                case RarityType.LegendaryPlus:
                    BaseMaterials[0] = MaterialsPlus[3];
                    break;
                case RarityType.MythicPlus:
                    BaseMaterials[0] = MaterialsPlus[4];
                    break;
                case RarityType.EtherealPlus:
                    BaseMaterials[0] = MaterialsPlus[5];
                    break;
                case RarityType.CosmicPlus:
                    BaseMaterials[0] = MaterialsPlus[6];
                    break;
            }
        }
        _background.materials = BaseMaterials;
    }

    public RarityType GetRarity()
    {
        return RarityType; 
    }

    private void OnValidate()
    {
        if (finishTileButton) 
        {
            finishTileButton = false;
            FinishTile();
        }
        if (_updateCycleModeButton)
        {
            _updateCycleModeButton = false;
            InitCycleMode(); 
        }
    }

    private void InitCycleMode()
    {
        _contactWarp.targetMode = _cycleMode;

        if (_cycleMode == CycleMode.receiver)
        {
            EntrancePipe.gameObject.SetActive(true); 
            _contactWarp.playerReceiver = EntrancePipe;
            EntrancePipe.transform.localPosition = new Vector3(0, EntrancePipe.transform.localPosition.y, EntrancePipe.transform.localPosition.z);
            EntrancePipe.transform.localEulerAngles = new Vector3(0, 0, 180); 
        }
        else if(_cycleMode == CycleMode.verticalWrap)
        {
            EntrancePipe.gameObject.SetActive(false);
            int side = (CurrentSide == Side.Left) ? 1 : -1;
            EntrancePipe.transform.localPosition = new Vector3((_background.transform.localScale.x / 2.1f) * side, EntrancePipe.transform.localPosition.y, EntrancePipe.transform.localPosition.z); //Extra .1 to get past wall barrier
            EntrancePipe.transform.localEulerAngles = new Vector3(0, 0, 90 * side); 
        }
    }

    public void TogglePhysics(bool toggle)
    {
        foreach (var rb in GetComponentsInChildren<Rigidbody2D>())
            rb.simulated = toggle;

        foreach(var col in GetComponentsInChildren<Collider2D>())
            col.enabled = toggle;

        foreach (var oscillators in GetComponentsInChildren<OscillatorV2>())
            oscillators.ToggleOnOff(toggle); 
    }
    public void PreInitTile(TileController tc, bool insideGolden, bool insideRuby, bool insideCurse, bool insideMystery)
    {
        _goldenVisuals._coverObj.gameObject.SetActive(false);

        IsCurse = false;
        IsRuby = false;
        IsGolden = false;
        IsMystery = false;

        int Mysteries = UnityEngine.Random.Range(1, 5);
        MysteriousAnnouncer = Mysteries;
        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();
        
        _tc = tc;

        InitCycleMode(); 

        Effectors = GetComponentsInChildren<PBEffector>();

        _timer = 0;
        UpdateTileTimer();
        ResetTicketBonus();

        EntrancePipe.LockIcon.enabled = true;   

        foreach (var resetable in _resetablesRoot.GetComponentsInChildren<IResetable>())
            resetable.MyReset();

        foreach (var effector in Effectors)
        {
            effector.ResetEffector();

            effector.MultiplyCurrValue(AppConfig.GetMult(RarityType));
            bool do100Effect = false;
            bool doZeroEffect = false;

            if (insideMystery)
            {
                switch (Mysteries)
                {
                    case 1:
                        insideCurse = true;
                        IsCurse = true;
                        break;
                    case 2:
                        insideRuby = true;
                        IsRuby = true;
                        break;
                    case 3:
                        insideGolden = true;
                        IsGolden = true;
                        break;
                    case 4:
                        do100Effect = true;
                        IsMystery = true;
                        break;
                    case 5:
                        doZeroEffect = true;
                        IsMystery = true;
                        break;
                }
            }
            if (insideCurse)
                effector.MultiplyCurrValue(-1);
            else if (insideRuby)
                effector.MultiplyCurrValue(50);
            else if (insideGolden)
                effector.MultiplyCurrValue(10);
            else if (do100Effect)
                effector.MultiplyCurrValue(100);
            else if (doZeroEffect)
                effector.MultiplyCurrValue(0);
        }
        
        EntrancePipe.SetTollCost(tc.GetGameManager().GetKingController().TollRate * AppConfig.GetMult(RarityType));

        if (_game != null)
        {
            _game.DoneWithGameplay = false;
            _game.IsGameStarted = false;
            _game.OnTilePreInit();
        }

        if (_suddenDeath.gameObject.activeSelf)
            _suddenDeath.OnTilePreInit();

        if (IsShop)
        {
            insideCurse = false;
            insideRuby = false;
            insideGolden = false;
            insideMystery = false;
        }

        if (insideMystery)
        {
            _goldenVisuals.gameObject.SetActive(true);
            _goldenVisuals._coverObj.gameObject.SetActive(true);
            IsMystery = true;
            GoldenSpids = 4;
            _goldenVisuals.UpdateSettings(4);
        }
        else if (insideCurse)
        {            
            _goldenVisuals.gameObject.SetActive(true);
            IsCurse = true;
            GoldenSpids = 3;
            _goldenVisuals.UpdateSettings(3);
        }
        else if (insideRuby)
        {
            _goldenVisuals.gameObject.SetActive(true);
            IsRuby = true;
            GoldenSpids = 2;
            _goldenVisuals.UpdateSettings(2);
        }
        else if (insideGolden)
        {
            _goldenVisuals.gameObject.SetActive(true);
            IsGolden = true;
            GoldenSpids = 1;
            _goldenVisuals.UpdateSettings(1);
        }        
        else
        {
            _goldenVisuals.UpdateSettings(0);
            _goldenVisuals.gameObject.SetActive(false);
            _goldenVisuals._coverObj.gameObject.SetActive(false);
            GoldenSpids = 0;
        }



        _mpb.SetColor("_StartColor", _backgroundStartColor);
        _mpb.SetColor("_EndColor", _backgroundEndColor);
        _background.SetPropertyBlock(_mpb);

        _trimMpb.SetColor("_BaseColor", _trimColor);
        foreach (var meshRenderer in _colorTrimsByRarity)
            meshRenderer.SetPropertyBlock(_trimMpb);

        SetBackground();
        //SetBackgroundShader(0);
        UpdateTileTimer();

    }

    public void InitTileInPos()
    {

        TileState = TileState.LockedInPos;
        //TileActive = true;
        _timer = 0;
        //_playersReleased = 0;

        Players.Clear();
        AlivePlayers.Clear();
        EliminatedPlayers.Clear();

        if (_releaseBar != null)
            _releaseBar.gameObject.SetActive(true);

        if (_game != null)
            _game.OnTileInitInPos();

        if (IsMystery)
        {
            StartCoroutine(PlaySoundSequence("Mystery"));
        }
        else if (IsRuby)
        {
            AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.4f, 0.5f);
        }
        else if (IsGolden)
        {
            AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.2f, 0.3f);
        }
        else if (IsCurse)
        {
            StartCoroutine(PlaySoundSequence("Curse"));
        }
        else if (IsShop)
        {
            AudioController.inst.PlaySound(AudioController.inst.BattlePerchEarn, 0.4f, 0.5f);
        }

        if(!IsMystery)
        switch (GetRarity())
        {
            case RarityType.Legendary:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.2f, 0.3f);
                break;
            case RarityType.Mythic:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.3f, 0.4f);
                break;
            case RarityType.Ethereal:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.4f, 0.5f);
                break;
            case RarityType.Cosmic:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.5f, 0.6f);
                break;
            case RarityType.CommonPlus:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.6f, 0.7f);
                break;
            case RarityType.RarePlus:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.6f, 0.7f);
                break;
            case RarityType.EpicPlus:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.6f, 0.7f);
                break;
            case RarityType.LegendaryPlus:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.6f, 0.7f);
                break;
            case RarityType.MythicPlus:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.6f, 0.7f);
                break;
            case RarityType.EtherealPlus:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.6f, 0.7f);
                break;
            case RarityType.CosmicPlus:
                AudioController.inst.PlaySound(AudioController.inst.TileRarity, 0.6f, 0.7f);
                break;
        }

        _tc._forceGolden = false;
        _tc._forceRuby = false;
        _tc._forceCurse = false;
        _tc._forceMystery = false;

        TogglePhysics(true);

        UpdateTileTimer();

    }

    public void OnPipeReleasePlayer(PlayerBall pb)
    {
        //_playersReleased++; 
    }

    public IEnumerator PlaySoundSequence(string Which)
    {
        switch (Which)
        {
            case "Mystery":
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.4f, 0.8f);
                yield return new WaitForSeconds(0.1f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.35f, 0.7f);
                yield return new WaitForSeconds(0.1f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.3f, 0.6f);
                yield return new WaitForSeconds(0.1f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.25f, 0.5f);
                yield return new WaitForSeconds(0.1f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.2f, 0.4f);
                yield return new WaitForSeconds(0.1f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.15f, 0.3f);
                break;
            case "Curse":
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.06f, 0.08f);
                yield return new WaitForSeconds(0.5f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.05f, 0.07f);
                yield return new WaitForSeconds(0.5f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.04f, 0.06f);
                yield return new WaitForSeconds(0.5f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.03f, 0.05f);
                yield return new WaitForSeconds(0.5f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.02f, 0.04f);
                yield return new WaitForSeconds(0.5f);
                AudioController.inst.PlaySound(AudioController.inst.TileStatus, 0.01f, 0.03f);
                break;
        }
    }
    public IEnumerator RunTile()
    {
        TileState = TileState.Gameplay; 
        EntrancePipe.LockIcon.enabled = false;
        _tileStartTime = DateTime.Now;

        _goldenVisuals._coverObj.gameObject.SetActive(false);
        
        if (IsMystery)
        AnnounceMystery(MysteriousAnnouncer);

        _playerPointsSumStart = 0;
        foreach (var player in Players)
        {
            player.TilePointsROI = 0; 
            _playerPointsSumStart += player.pp.SessionScore;
        }

        if (_waitForAll)
        {
            //wait until all the players are off the conveyor belt
            while (ConveyorBelt.Count > 0)
                yield return null;
        }

        if (_suddenDeath.gameObject.activeSelf)
            _suddenDeath.StartSuddenDeath();

        if (_releaseBar != null)
            _releaseBar.gameObject.SetActive(false); 
        
        if(_game != null)
        {
            _game.StartGame();
            _game.IsGameStarted = true;
        }

        _forceEndGameplay = false;

        _tileGameplayTimeElapsed = 0; 
        //Run the gameplay until we either get a signal from the game that it's done, or there is only one player left alive
        while (true)
        {
            //Stop if the game signals it's done
            if (_game != null && _game.DoneWithGameplay)
                break;

            //Stop if the timer runs out
            if (_timer > _tileDurationS)
                break;

            //Stop if there is only one player left alive and none on the belt
            if (!IsShop && AlivePlayers.Count <= ((_waitForAllDead) ? 0 : 1) && ConveyorBelt.Count <= 0)
                break;

            if (IsShop && AlivePlayers.Count <= 0 && ConveyorBelt.Count <= 0)
                break;

            if (_forceEndGameplay)
                break;

            if(_tileGameplayTimeElapsed > 240)
            {
                Debug.LogError("Finishing tile due exciting gameplay duration limit"); 
                break;
            }

            _tileGameplayTimeElapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"about to start podium: tileTimeElapsed: {_timer} tileDuration:{_tileDurationS} alivePlayers:{AlivePlayers.Count} ConveyorBelt:{ConveyorBelt.Count}");

        //Freeze the physics on any winning survivors so that they don't die while the podium is going up
        foreach(var ph in AlivePlayers)
        {
            if (ph.pb != null)
                ph.pb.EnableKinematicMode(); 
        }

        long _playerPointsSumFinish = 0;
        foreach (var player in Players)
            _playerPointsSumFinish += player.pp.SessionScore;

        if(Players.Count > 0)
            Debug.Log($"Tile {name} Rarity {RarityType} Gameplay finished in {(DateTime.Now - _tileStartTime).ToString(@"hh\:mm\:ss")}\n" +
                      $"Total Points Distributed: {(_playerPointsSumFinish - _playerPointsSumStart)}\n" + 
                      $"Average Points Gained Per Player: {(_playerPointsSumFinish - _playerPointsSumStart) / Players.Count}");


        if (!IsShop && Players.Count > 0)
            yield return _tc.Podium.RunPodium(this, Players);

        yield return null; //Wait one frame to allow time for bid handler to switch in case the tile has no bidders and instantly spins after countdown
        FinishTile(); 
    }

    private void AnnounceMystery(int Mystery)
    {
        switch (Mystery)
        {
            case 1:
                MyTTS.inst.PlayerSpeech("Cursed Tile", Amazon.Polly.VoiceId.Emma);
                break;
            case 2:
                MyTTS.inst.PlayerSpeech("10x Multiplier", Amazon.Polly.VoiceId.Emma);
                break;
            case 3:
                MyTTS.inst.PlayerSpeech("50x Multiplier", Amazon.Polly.VoiceId.Emma);
                break;
            case 4:
                MyTTS.inst.PlayerSpeech("100x Multiplier", Amazon.Polly.VoiceId.Emma);
                break;
            case 5:
                MyTTS.inst.PlayerSpeech("Null Tile", Amazon.Polly.VoiceId.Emma);
                break;
        }
    }

    public void EliminatePlayer(PlayerHandler ph, bool setRankScoreByElimOrder)
    {
        int rankScore = -1;
        if(setRankScoreByElimOrder)
            rankScore = EliminatedPlayers.Count;

        EliminatePlayer(ph, rankScore); 
    }

    public void EliminatePlayer(PlayerHandler ph, int rankScoreOverride = -1)
    {
        ph.SetState(PlayerHandlerState.Idle);

        //You can only be eliminated once
        if (!EliminatedPlayers.Contains(ph))
            EliminatedPlayers.Add(ph);

        if (rankScoreOverride != -1)
            ph.SetRankScore(rankScoreOverride);

        AlivePlayers.Remove(ph);
        ConveyorBelt.Remove(ph); //In case the player somehow died on the belt due to bug

        ph.GetPlayerBall().ExplodeBall();

    }

    public void EliminatePlayers(List<PlayerHandler> phs, bool setRankScoreByElimOrder)
    {
        int rankScore = -1;
        if(setRankScoreByElimOrder)
            rankScore = Players.Count - AlivePlayers.Count;

        foreach(var ph in phs)
            EliminatePlayer(ph, rankScore); 
    }

    public void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        if (_game == null)
            return;

        //If the game is not started, don't process any gameplay commands
        if (!_game.IsGameStarted)
            return;

        _game.ProcessGameplayCommand(messageId, twitchClient, ph, msg, rawEmotesRemoved);
    }


    //TODO: Backup timer, if all but one player isn't eliminated by the time this timer runs out, make the remaining players tie for first
    public IEnumerator RunTimer(int durationS)
    {
        _timer = 0;
        while (_timer < durationS)
        {
            float t = _timer / durationS;
            SetBackgroundShader(1 - t);
            _timerText.SetText( MyUtil.GetMinuteSecString((int)(durationS - _timer)));

            yield return new WaitForSeconds(1);

            _timer++;
        }
        SetBackgroundShader(0);
        _timerText.SetText(MyUtil.GetMinuteSecString(0));

    }

    private void UpdateTileTimer()
    {
        float t = _timer / _tileDurationS;

        if(t >= 1)
        {
            FinishTile();
            return;
        }

        SetBackgroundShader(1 - t); 
    }

    public void SetBackgroundShader(float t)
    {
        _background.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_FillAmount", t);
        _background.SetPropertyBlock(_mpb);
    }

    public PlayerHandler GetAlivePlayerViaUsername(string twitchUsername)
    {
        //Check if the usernames match, ignore upper vs lowercase
        return AlivePlayers.Find(ph => string.Equals(ph.pp.TwitchUsername, twitchUsername, System.StringComparison.OrdinalIgnoreCase));
    }

    public void FinishTile()
    {
        //TileActive = false;
        TileState = TileState.Inactive; 
        if (_game != null)
        {
            _game.CleanUpGame();
            _game.IsGameStarted = false;
        }

        _suddenDeath.CleanUp();

        Players.Clear();
        AlivePlayers.Clear();
        EliminatedPlayers.Clear();

        //Once the gameplay tile finishes, spin it to a new tile
        _tc.SpinNewTile(this);

        IsCurse = false;
        IsGolden = false;
        IsRuby = false;
        IsMystery = false;
        _goldenVisuals._coverObj.gameObject.SetActive(false);
    }

    public void SetTicketBonus(int count)
    {
        if (IsShop)
            return;

        TicketBonusAmount = count;
        UpdateTicketBonusText();
        _ticketBonusAmountText.enabled = true; 
    }

    private void ResetTicketBonus()
    {
        TicketBonusAmount = 0;
        _ticketBonusAmountText.enabled = false;
        UpdateTicketBonusText(); 
    }

    private void UpdateTicketBonusText()
    {
        _ticketBonusAmountText.SetText("Win Prize: " + MyUtil.AbbreviateNum4Char(TicketBonusAmount)); 
    }

    public Vector3 GetTicketBonusAmountPos()
    {
        return _ticketBonusAmountText.transform.position;
    }
    public TileController GetTileController()
    {
        return _tc;
    }
    public Game GetGame()
    {
        return _game;
    }
    public SuddenDeath GetSuddenDeath()
    {
        return _suddenDeath;
    }

    public void ForceEndGameplay()
    {
        _forceEndGameplay = true; 
    }

    public Vector3 GetGoldSpawnPos()
    {
        return _rarityText.transform.position;
    }
}
