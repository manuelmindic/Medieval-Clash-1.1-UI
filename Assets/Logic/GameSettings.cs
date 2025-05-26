using UnityEngine;

public static class GameSettings
{
    public static bool UseManaSystem => PlayerPrefs.GetInt("ToggleManaState", 1) == 1;
    public static bool UseHiddenDecks => PlayerPrefs.GetInt("Deck", 1) == 1;
    public static bool UseBuffDebuffCards => PlayerPrefs.GetInt("ToggleBuffDebuffState", 1) == 1;
}