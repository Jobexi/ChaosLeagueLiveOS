using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KingController : MonoBehaviour, TravelingIndicatorIO
{
    public PlayerBall currentKing = null;

    [SerializeField] private GameManager _gm;
    [SerializeField] public TileController _tileController;
    [SerializeField] private MeshRenderer kingInitialPlaceholder;
    [SerializeField] private Transform kingTransform;
    [SerializeField] private PBDetector _pbCollisionDetector;
    [SerializeField] private CleaningBarController _cleaningBarController;
    [SerializeField] private ParticleSystem confetti;
    [SerializeField] private TextMeshPro winnerNameText;
    [SerializeField] private TextMeshPro _tollRateText;
    [SerializeField] private AutoPredictions _autoPredictions;
    [SerializeField] private GoldDistributor _liveViewCount;
    [SerializeField] private Crown _crown;
    [SerializeField] private MyCameraController _myCameraController;

    [SerializeField] private TextMeshPro _kingPointsText;
    [SerializeField] private TextMeshPro _kingGoldText;
    [SerializeField] private TextMeshPro _kingSapphireText;
    [SerializeField] private TextMeshPro _kingEmeraldText;    
    [SerializeField] private TextMeshPro _kingDiamondText;
    [SerializeField] private TextMeshPro _kingRubyText;

    [SerializeField] private DefaultDefenseV2 _defaultDefenseV2;

    private float _previousKingDuration = 0;
    private float _currentKingTimer = 0;

    [SerializeField] private GameObject _newKingBlockade;
    [SerializeField] private TextMeshPro _newKingBlockadeTimer;
    [SerializeField] private Material _throneTileTrim;
    [SerializeField] private bool _enableNewKingBlockade = true; 

    public int TollRate = 0;
    public static PlayerHandler CKPH;
    public int timer = 0;
    public int duration = 300;

    public void Awake()
    {
        _newKingBlockade.SetActive(true);
        _newKingBlockadeTimer.enabled = true;
        StartCoroutine(NewKingBlockade());
    }

    public void UpdateTollRate(int rate)
    {
        if (currentKing == null)
        {
            CLDebug.Inst.Log("Failed to update toll rate. No currentKing");
            return;
        }

        if (rate == TollRate)
            return;

        if (RebellionController.RoyalCelebration)
        {
            currentKing.Ph.SpeechBubble($"Citizens will be rewarded with Gold from The Treasury during our Royal Celebration");
        }
        else
        {
            MyTTS.inst.Announce($"I decree a new toll rate: {rate}");
            currentKing.Ph.SpeechBubble($"I decree a new toll rate: {rate}");
        }
        TollRate = rate;
        
        _tileController.GameplayTile?.EntrancePipe.SetTollCost(rate);
        _tileController.CurrentBiddingTile?.EntrancePipe.SetTollCost(rate);
        _tileController.NextBiddingTile?.EntrancePipe.SetTollCost(rate);
    }

    private void Update()
    {
        _currentKingTimer += Time.deltaTime;
    }


    public IEnumerator ThroneNewKing(PlayerBall pb)
    {
        if(_enableNewKingBlockade)
            StartCoroutine(NewKingBlockade());

        //_myCameraController.KingFocusCameraMove(); 

        CleanupCurrentKing();

        _crown.UpdateCustomizations(CrownSerializer.GetColorListFromJSON(pb.Ph.pp.CrownJSON));

        var Txtr1 = pb.Ph.pp.CrownTexture1;
        var Txtr2 = pb.Ph.pp.CrownTexture2;

        var BaseMaterials = _crown._crownMeshRenderer.materials;
        var DesiredMaterials = _crown.EnhancedMaterials;
        BaseMaterials[0] = DesiredMaterials[Txtr1];
        BaseMaterials[1] = DesiredMaterials[Txtr2];
        _crown._crownMeshRenderer.materials = BaseMaterials;

        string newKingUsername = pb.Ph.pp.TwitchUsername;
        pb.Ph.pp.ThroneCaptures += 1; 
        _autoPredictions.NewKingSignal(newKingUsername, (int)_previousKingDuration);

        MyTTS.inst.Announce($"Throne captured by {newKingUsername}");
        winnerNameText.SetText(newKingUsername);

        winnerNameText.color = pb._usernameText.color;
        _throneTileTrim.color = pb._usernameText.color;

        pb.SetupAsKing(kingTransform.localScale);
        pb.UpdateBidCountText(); //Hides the bid count while king
        pb._rb2D.transform.position = kingTransform.position;
        pb._rb2D.transform.eulerAngles = kingTransform.eulerAngles;
        pb.OverridePointsTextTarget(_kingPointsText);
        //pb.pointsText.rectTransform = _kingPoints.rectTransform;

        AudioController.inst.PlaySound(AudioController.inst.NewKingThroned, 1f, 1f);
        AudioController.inst.PlaySound(AudioController.inst.Beheading, 1f, 1f);
        confetti.Play(); 

        //pointPopUpTimer = 0;

        currentKing = pb;
        CKPH = pb.Ph;
        UpdateGoldText();

        yield return StartCoroutine(_cleaningBarController.RunCleaningBar());

        UpdateCurrExponentScale();

        _defaultDefenseV2.ResetDefense(DefaultDefenseMode.Random, 10, 5); 

        //Force spending half of points on defense
        long halfOfPoints = pb.Ph.pp.SessionScore / 2;
        if (halfOfPoints > 0)
            _defaultDefenseV2.AddBonusDefense(halfOfPoints, pb.Ph);

        _liveViewCount.NewKingSignal(); 
    }

    private IEnumerator NewKingBlockade()
    {
        _newKingBlockade.SetActive(true);
        _newKingBlockadeTimer.enabled = true;
        timer = 0;
        duration = 300;         
        while (timer < duration)
        {
            _newKingBlockadeTimer.SetText((duration - timer).ToString());
            yield return new WaitForSeconds(1);
            timer++;
        }
        _newKingBlockade.SetActive(false);
        _newKingBlockadeTimer.enabled = false;
    }

    private void CleanupCurrentKing()
    {
        _previousKingDuration = _currentKingTimer;
        _currentKingTimer = 0;

        kingInitialPlaceholder.enabled = false; //Only needs to happen for first king

        //Destroy the previous king and send them the points they've earned for holding the position
        if (currentKing != null)
        {
            currentKing.Ph.SetState(PlayerHandlerState.Idle);

            //currentKing.Ph.pp.Gold += ((int)_previousKingDuration);
            currentKing.Ph.pp.TimeOnThrone += ((int)_previousKingDuration);
            currentKing.ExplodeBall();
            currentKing.ResetAndUpdatePointsTextTarget();

            currentKing._sbcV2.transform.localScale = Vector3.one;

            currentKing._usernameText.enabled = true;
            currentKing._usernameBackgroundHighlight.enabled = true;
            currentKing._inviterIndicator.enabled = true;

        }
    }
    
    public void UpdateGoldText()
    {
        _kingPointsText.SetText($"{MyUtil.AbbreviateNum4Char(currentKing.Ph.pp.SessionScore)} Points");
        _kingGoldText.SetText($"{MyUtil.AbbreviateNum4Char(currentKing.Ph.pp.Gold)} Gold");
        _kingEmeraldText.SetText($"{MyUtil.AbbreviateNum4Char(currentKing.Ph.pp.Emeralds)} Emeralds");
        _kingSapphireText.SetText($"{MyUtil.AbbreviateNum4Char(currentKing.Ph.pp.Sapphires)} Sapphires");
        _kingDiamondText.SetText($"{MyUtil.AbbreviateNum4Char(currentKing.Ph.pp.Diamonds)} Diamonds");
        _kingRubyText.SetText($"{MyUtil.AbbreviateNum4Char(currentKing.Ph.pp.Rubies)} Rubies");
    }

    private void UpdateCurrExponentScale()
    {
        if (_previousKingDuration < AppConfig.inst.GetF("TargetKingDuration"))
            _defaultDefenseV2.ExponentScale += 1;
        else
        {
            _defaultDefenseV2.ExponentScale -= 1;
            if (_defaultDefenseV2.ExponentScale < 1)
                _defaultDefenseV2.ExponentScale = 1;
        }
    }

    public void DetectedPB(PlayerBall pb)
    {
        if (_cleaningBarController.Activated)
            return;

        StartCoroutine(ThroneNewKing(pb)); 
    }


    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        return;
    }

    public Vector3 Get_TI_IO_Position()
    {
        return _tollRateText.transform.position;
    }


}
