using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameProgressData
{
    public int remainingHints;
    public List<string> scannedVumarkIds = new List<string>();
}

public static class GameProgressStore
{
    private const string PlayerPrefsKey = "BugFix.GameProgress";

    private static GameProgressData cachedData;
    private static bool isLoaded;
    private static int maxHints = int.MaxValue;

    public static void Initialize(int maxHints)
    {
        EnsureLoaded();

        GameProgressStore.maxHints = Mathf.Max(0, maxHints);

        cachedData.remainingHints = Mathf.Clamp(cachedData.remainingHints, 0, GameProgressStore.maxHints);

        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            cachedData.remainingHints = GameProgressStore.maxHints;
            Save();
        }
    }

    public static int RemainingHints
    {
        get
        {
            EnsureLoaded();
            return cachedData.remainingHints;
        }
    }

    public static bool HasHints => RemainingHints > 0;

    public static bool TryConsumeHint()
    {
        EnsureLoaded();

        if (cachedData.remainingHints <= 0)
            return false;

        cachedData.remainingHints = Mathf.Max(0, cachedData.remainingHints - 1);
        Save();
        return true;
    }

    public static void AddHint(int amount = 1)
    {
        EnsureLoaded();

        cachedData.remainingHints = Mathf.Clamp(cachedData.remainingHints + amount, 0, maxHints);
        Save();
    }

    public static bool IsVumarkAlreadyScanned(string vumarkId)
    {
        if (string.IsNullOrWhiteSpace(vumarkId))
            return false;

        EnsureLoaded();
        return cachedData.scannedVumarkIds.Contains(vumarkId);
    }

    public static void MarkVumarkAsScanned(string vumarkId)
    {
        if (string.IsNullOrWhiteSpace(vumarkId))
            return;

        EnsureLoaded();

        if (cachedData.scannedVumarkIds.Contains(vumarkId))
            return;

        cachedData.scannedVumarkIds.Add(vumarkId);
        Save();
    }

    public static void Save()
    {
        EnsureLoaded();
        string json = JsonUtility.ToJson(cachedData);
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }

    public static void ResetProgress(int maxHints)
    {
        GameProgressStore.maxHints = Mathf.Max(0, maxHints);

        cachedData = new GameProgressData
        {
            remainingHints = GameProgressStore.maxHints,
            scannedVumarkIds = new List<string>()
        };
        isLoaded = true;
        Save();
    }

    public static void ClearSavedProgress()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();
        cachedData = new GameProgressData();
        isLoaded = true;
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey);
            cachedData = string.IsNullOrWhiteSpace(json)
                ? new GameProgressData()
                : JsonUtility.FromJson<GameProgressData>(json);
        }

        if (cachedData == null)
        {
            cachedData = new GameProgressData();
        }

        if (cachedData.scannedVumarkIds == null)
        {
            cachedData.scannedVumarkIds = new List<string>();
        }

        isLoaded = true;
    }
}