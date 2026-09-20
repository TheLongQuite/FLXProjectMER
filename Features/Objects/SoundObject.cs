using AudioSystem;
using AudioSystem.Models;
using Exiled.API.Features;
using ProjectMER.Features.Serializable;
using UnityEngine;

namespace ProjectMER.Features.Objects;

public class SoundObject : MonoBehaviour
{
    private AudioHandle? _audioHandle;

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
            Log.Debug($"[SoundObject] SoundName is empty on object at {transform.position}. Skipping playback.");
            return;
        }

        try
        {
            _audioHandle = Methods.Play($"{serializable.SoundName}.ogg", r =>
                {
                    r.Follow = transform;
                    r.MaxDistance = serializable.Radius;
                    r.MinDistance = serializable.MinRadius;
                    r.Volume = serializable.Volume;
                    r.Label = "MER_Sound";
                    r.Loop = serializable.Loop;
                });
        }
        catch (Exception e)
        {
            Log.Debug($"[SoundObject] Failed to play sound '{serializable.SoundName}': {e.Message}");
        }
    }

    public void Stop()
    {
        if (_audioHandle == null)
            return;

        try
        {
            _audioHandle.Stop();
        }
        catch (Exception e)
        {
            Log.Debug($"[SoundObject] Failed to stop audio id {_audioHandle}: {e.Message}");
        }

        _audioHandle = null;
    }

    private void OnDestroy() => Stop();
}