using UnityEngine;

/// <summary>
/// UTILITY — fades an AudioSource out and then stops it. When <c>surviveSceneLoad</c> is set the
/// sound keeps playing through a scene change (the ending score rings out under the end
/// screen instead of being cut off when the Ending scene loads), then destroys itself.
/// </summary>
public class AudioFadeOut : MonoBehaviour
{
    AudioSource source;
    float startVolume, seconds, t;
    bool destroyWhenDone;

    public static void Begin(AudioSource source, float seconds, bool surviveSceneLoad)
    {
        if (!source) return;
        if (surviveSceneLoad)
        {
            source.transform.SetParent(null, true);
            DontDestroyOnLoad(source.gameObject);
        }
        var f = source.gameObject.AddComponent<AudioFadeOut>();
        f.source = source; f.startVolume = source.volume; f.seconds = Mathf.Max(.05f, seconds); f.destroyWhenDone = surviveSceneLoad;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        if (source) source.volume = Mathf.Lerp(startVolume, 0, t / seconds);
        if (t < seconds) return;
        if (source) source.Stop();
        if (destroyWhenDone) Destroy(gameObject); else Destroy(this);
    }
}
