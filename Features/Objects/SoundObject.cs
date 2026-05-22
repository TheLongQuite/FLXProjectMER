using AudioSystem;
using Exiled.API.Features;
using ProjectMER.Features.Serializable;
using UnityEngine;

namespace ProjectMER.Features.Objects;

public class SoundObject : MonoBehaviour
{
    private short _audioId = -1;

    public void Init(SerializableSound serializable) => Play(serializable);

    public void UpdateSound(SerializableSound serializable)
    {
        Stop();
        Play(serializable);
    }

    private void Play(SerializableSound serializable)
    {
        if (string.IsNullOrEmpty(serializable.SoundName))
        {
            Log.Info($"[SoundObject] SoundName is empty on object at {transform.position}. Skipping playback.");
            return;
        }

        try
        {
            _audioId = Methods.PlayAudio(
                $"{serializable.SoundName}.ogg",
                transform,
                serializable.Radius,
                serializable.MinRadius,
                serializable.Volume,
                "MER_Sound",
                serializable.Loop);
        }
        catch (Exception e)
        {
            Log.Info($"[SoundObject] Failed to play sound '{serializable.SoundName}': {e.Message}");
        }
    }

    public void Stop()
    {
        if (_audioId < 0)
            return;

        try
        {
            Methods.StopAudio(_audioId);
        }
        catch (Exception e)
        {
            Log.Info($"[SoundObject] Failed to stop audio id {_audioId}: {e.Message}");
        }

        _audioId = -1;
    }

    private void OnDestroy() => Stop();
}