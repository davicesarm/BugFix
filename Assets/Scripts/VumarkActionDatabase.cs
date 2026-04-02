using System;
using System.Collections.Generic;
using UnityEngine;

public enum VumarkActionType
{
    ShowText,
    ShowRandomDebuff,
    LoadScene,
    None
}

[Serializable]
public class VumarkActionEntry
{
    public string vumarkId;
    public VumarkActionType actionType = VumarkActionType.ShowText;
    [TextArea(2, 5)]
    public string text;
    public string sceneName;
}

[CreateAssetMenu(fileName = "VumarkActionDatabase", menuName = "Vumark/Action Database")]
public class VumarkActionDatabase : ScriptableObject
{
    [SerializeField]
    private List<VumarkActionEntry> actions = new();

    private Dictionary<string, VumarkActionEntry> cachedMap;

    private void OnEnable()
    {
        cachedMap = null;
    }

    public bool TryGetAction(string vumarkId, out VumarkActionEntry action)
    {
        action = null;

        if (string.IsNullOrWhiteSpace(vumarkId))
            return false;

        EnsureCache();
        return cachedMap.TryGetValue(vumarkId.Trim(), out action);
    }

    private void EnsureCache()
    {
        if (cachedMap != null)
            return;

        cachedMap = new Dictionary<string, VumarkActionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in actions)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.vumarkId))
                continue;

            cachedMap[entry.vumarkId.Trim()] = entry;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cachedMap = null;

        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in actions)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.vumarkId))
                continue;

            string trimmedId = entry.vumarkId.Trim();
            if (!ids.Add(trimmedId))
            {
                Debug.LogWarning($"VumarkActionDatabase: ID duplicado encontrado: {trimmedId}", this);
            }

            if (entry.actionType == VumarkActionType.ShowText && string.IsNullOrWhiteSpace(entry.text))
            {
                Debug.LogWarning($"VumarkActionDatabase: ShowText sem texto para ID: {trimmedId}", this);
            }

            if (entry.actionType == VumarkActionType.LoadScene && string.IsNullOrWhiteSpace(entry.sceneName))
            {
                Debug.LogWarning($"VumarkActionDatabase: LoadScene sem sceneName para ID: {trimmedId}", this);
            }
        }
    }
#endif
}
