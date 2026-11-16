using TMPro;
using UnityEngine;

/// <summary>Build included references to the user's UI icons and existing portraits.</summary>
[CreateAssetMenu(menuName = "Cthulhu/Dialogue History Assets")]
public sealed class DialogueHistoryAssets : ScriptableObject
{
    public Sprite historyIcon;
    public Sprite settingsIcon;
    public Sprite playerPortrait;
    public Sprite otherPortrait;
    public TMP_FontAsset font;
}
