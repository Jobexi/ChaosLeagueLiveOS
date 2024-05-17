using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

// {get; set;} is required for dynamodb to automatically create playerprofile from entry

[Serializable]
[Table("PlayerProfiles")] 

public class PlayerProfile
{
    [PrimaryKey] 
    public string TwitchID { get; set; }
    public string TwitchUsername { get; set; }
    public string InvitedByID { get; set; }
    public string InvitesJSON { get; set; }
    

    public bool IsSubscriber { get; set; }

    public string NameColorHex { get; set; }
    public string CrownJSON {  get; set; }
    public string TrailGradientJSON { get; set; }
    public string SpeechBubbleFillHex { get; set; }
    public string SpeechBubbleTxtHex { get; set; }
    public string CurrentVoiceID { get; set; }
    [Ignore]
    public string[] PurchasedVoiceIDs { get; set; }

    // POINTS

    public int ThroneCaptures { get; set; }
    public int TimeOnThrone { get; set; }
    public int TotalTicketsSpent { get; set; }

    public int CurrentBid { get; set; }
       
    public int LifeTimeScore { get; set; }
    public int Gold { get; set; }
    public int SeasonScore { get; set; }
    public long SessionScore { get; set; }
    public DateTime LastInteraction { get; set; }

    public int InviteCount { get; set; }
    public int TomatoCount { get; set; }
    public int ShieldValue { get; set; }
    public int AutoBidRemainder { get; set; }
    public long Rubies { get; set; }
    public long Sapphires { get; set; }
    public long Diamonds { get; set; }
    public long Emeralds { get; set; }

    public int RiskSkips { get; set; }

    public bool IsNPC { get; set; }
    public int ModeNPC { get; set; }
    public int StateNPC { get; set; }
    public int Data1NPC { get; set; }
    public int Data2NPC { get; set; }
    public int Data3NPC { get; set; }

    public int CrownTexture1 { get; set; }
    public int CrownTexture2 { get; set; }
    public int CrownTexture3 { get; set; }
    public int CrownTexture4 { get; set; }
    public int CrownTexture5 { get; set; }
    public int CrownTexture6 { get; set; }
    public bool EnhancedCrown { get; set; }

    public string[] GetInviteIds() 
    {
        if(string.IsNullOrEmpty(InvitesJSON))
            return Array.Empty<string>();
        return JsonConvert.DeserializeObject<string[]>(InvitesJSON);
    }

    private void SetInviteIds(string[] inviteIds)
    {
        InvitesJSON = JsonConvert.SerializeObject(inviteIds);
    }

    public void AddInvite(string id)
    {
        InviteCount++;
        List<string> currentInvites = GetInviteIds().ToList();
        //Don't add if it's already in the list
        if (currentInvites.Contains(id))
            return;
        currentInvites.Add(id);
        SetInviteIds(currentInvites.ToArray());
        
    }
}