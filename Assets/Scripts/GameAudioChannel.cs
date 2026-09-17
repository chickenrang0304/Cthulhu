using UnityEngine;

public enum GameAudioChannelType
{
    BGM,
    SFX
}

/// <summary>
/// Add this component to an AudioSource when automatic BGM/SFX detection is wrong.
/// Sources whose name or clip contains "BGM", "Music", "Theme" or "Ambient"
/// are treated as BGM by default. All other sources are treated as SFX.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class GameAudioChannel : MonoBehaviour
{
    [SerializeField] private GameAudioChannelType channel = GameAudioChannelType.SFX;

    public GameAudioChannelType Channel => channel;
}
