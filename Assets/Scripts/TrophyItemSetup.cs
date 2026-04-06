using UnityEngine;

public class TrophyItemSetup : MonoBehaviour
{
    public ShowTrophy showTrophy;
    public Trophy trophy;
    void Start()
    {
        if (showTrophy != null && trophy != null)
        {
            showTrophy.SetTrophy(trophy);
        }
    }
}