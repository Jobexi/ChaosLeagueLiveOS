using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GoldShopEntry : ShopEntry
{
    [SerializeField] private SpriteRenderer _bubbleContainer;
    [SerializeField] private TextMeshPro _bubbleText;
    [SerializeField] private LineRenderer _bubbleStalk;

    [SerializeField] public List<string> BuyCommands;

    private Color _purchaseColor;
    private int valuechoice;

    public void InitEntry(int tier, bool isGolden, float goldenDiscount)
    {

        int randomPrice; 
        if(tier == 1)
        {
            randomPrice = 1;
            valuechoice = 1000000;
        }
        else if(tier == 2)
        {
            randomPrice = 4;
            valuechoice = 5000000;
        }
        else
        {
            randomPrice = 7;
            valuechoice = 10000000;
        }


        if (isGolden)
            randomPrice = Mathf.RoundToInt(randomPrice * (1 - goldenDiscount));

        InitEntryBase(randomPrice, BuyCommands);

    }

    public override void ReceivePlayer(PlayerBall pb)
    {
        AttemptPurchase(pb);
    }

    public void AttemptPurchase(PlayerBall pb)
    {
        if (pb.Ph.pp.Rubies < GoldCost)
        {
            Debug.Log("Player doesn't have enough Rubies");
            return;
        }

        pb.Ph.pp.Rubies -= GoldCost;
        //Gold subtraction popup
        TextPopupMaster.Inst.CreateTextPopup(pb.GetPosition(), Vector3.up, $"-{MyUtil.AbbreviateNum4Char(GoldCost)} RUBIES", Color.red);

        pb.Ph.pp.Gold += valuechoice;

        StartCoroutine(PurchaseAnimation(pb));

        AudioController.inst.PlaySound(AudioController.inst.StorePurchase, 0.95f, 1.05f);
    }
}
