namespace Ratillery.Game.Animation;

/// <summary>
/// Plays an <see cref="AnimationClip"/>, advancing frames over time.
/// </summary>
public sealed class Animator
{
    private AnimationClip? _clip;
    private float _elapsed;

    public AnimationClip? CurrentClip => _clip;

    public int CurrentFrame { get; private set; }

    public bool Finished => _clip is not null && !_clip.Loop && CurrentFrame >= _clip.FrameCount - 1;

    public void Play(AnimationClip clip)
    {
        if (_clip == clip)
            return;

        _clip = clip;
        _elapsed = 0f;
        CurrentFrame = 0;
    }

    public void Update(float deltaTime)
    {
        if (_clip is null || Finished)
            return;

        _elapsed += deltaTime;
        while (_elapsed >= _clip.FrameDuration)
        {
            _elapsed -= _clip.FrameDuration;
            CurrentFrame++;
            if (CurrentFrame >= _clip.FrameCount)
                CurrentFrame = _clip.Loop ? 0 : _clip.FrameCount - 1;
        }
    }
}
