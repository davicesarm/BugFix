using System.Collections.Generic;
using UnityEngine;

/*
Fluxo para o jogador desbloquear um trof�u:

PlayerTrophies playerTrophies = PlayerTrophies.Load();
playerTrophies.UnlockTrophy("<id do trofeu>");
 */

[System.Serializable]
public class PlayerTrophies
{
    public List<string> unlockedTrophies = new List<string>();

    public bool HasTrophy(string trophyId)
    {
        return unlockedTrophies.Contains(trophyId);
    }

    public void UnlockTrophy(string trophyId)
    {
        if (!unlockedTrophies.Contains(trophyId))
        {
            unlockedTrophies.Add(trophyId);
            Save();
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString("PlayerTrophies", json);
    }

    public static PlayerTrophies Load()
    {
        if (PlayerPrefs.HasKey("PlayerTrophies"))
        {
            string json = PlayerPrefs.GetString("PlayerTrophies");
            return JsonUtility.FromJson<PlayerTrophies>(json);
        }
        return new PlayerTrophies();
    }
}