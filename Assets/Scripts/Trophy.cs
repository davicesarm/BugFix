using UnityEngine;

[CreateAssetMenu(fileName = "NewTrophy", menuName = "Trophies/Trophy")]
public class Trophy : ScriptableObject
{
    public string trophyId;
    public string trophyName;
    public string description;
    public GameObject model;
    public Sprite icon;
}