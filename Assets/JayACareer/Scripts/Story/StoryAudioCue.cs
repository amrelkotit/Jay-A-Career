using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NARRATIVE SCRIPT — environmental storytelling through sound (Week 7 "Sound Design":
/// sound gives gameplay cues, feedback and drives emotion; "attach sounds to game events").
///
/// Plays a looping 3D sound only while the story state matches <see cref="playWhile"/>, with
/// a short fade, so the soundscape follows the story without any extra code:
///   • Luka's phone buzzes on the counter after the deal — a cue that leads the player to the
///     "P." message — and falls silent once the message has been read
///   • Hale's lab fixtures hum while the lights are on; on Day 3 the dark lab is silent until
///     Hale walks back in
/// The conditions use the same <see cref="StoryCondition"/> data as dialogue and triggers.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class StoryAudioCue : MonoBehaviour
{
    [Tooltip("The sound plays while ALL of these are true (empty = always).")]
    public List<StoryCondition> playWhile = new List<StoryCondition>();
    [Range(0, 1)] public float volume = .6f;
    public float fadeSeconds = .6f;

    AudioSource source;
    bool shouldPlay;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true; source.playOnAwake = false; source.volume = 0;
    }

    void OnEnable()
    {
        StoryStateManager.Instance.Changed += Evaluate;
        Evaluate();
    }

    void OnDisable()
    {
        if (StoryStateManager.Exists) StoryStateManager.Instance.Changed -= Evaluate;
        if (source) source.Stop();
    }

    void Evaluate() { shouldPlay = StoryCondition.AllMet(playWhile, StoryStateManager.Instance); }

    void Update()
    {
        float target = shouldPlay ? volume : 0;
        if (shouldPlay && !source.isPlaying) source.Play();
        source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * volume / Mathf.Max(.01f, fadeSeconds));
        if (!shouldPlay && source.isPlaying && source.volume <= 0.001f) source.Stop();
    }
}
