using ProjectMER.Features.Components;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace ProjectMER.Features;

// <summary>
/// A class which handles everything related to Unity's animation system applied to <see cref="SchematicObject"/>s.
/// </summary>
public class AnimationController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationController"/> class.
    /// </summary>
    /// <param name="schematic">The root schematic.</param>
    internal AnimationController(SchematicObject schematic)
    {
        AttachedSchematic = schematic;

        List<Animator> animatorList = [];
        List<AnimationStateTracker> trackerList = [];

        foreach (GameObject gameObject in schematic.AttachedBlocks)
        {
            if (!gameObject.TryGetComponent(out Animator animator))
                continue;

            animatorList.Add(animator);
            trackerList.Add(AnimationStateTracker.GetOrCreate(animator));
        }

        Animators = animatorList;
        StateTrackers = trackerList;
        Dictionary.Add(schematic, this);
    }

    /// <summary>
    /// Gets the attached <see cref="SchematicObject"/>.
    /// </summary>
    public SchematicObject AttachedSchematic { get; }

    /// <summary>
    /// Gets a <see cref="IReadOnlyList{T}"/> of <see cref="Animator"/> containing all the animators.
    /// </summary>
    public IReadOnlyList<Animator> Animators { get; }
    
    /// <summary>
    /// Gets a <see cref="IReadOnlyList{T}"/> of <see cref="AnimationStateTracker"/> containing all the state trackers.
    /// </summary>
    public IReadOnlyList<AnimationStateTracker> StateTrackers { get; }

    /// <summary>
    /// Подписаться на вход в конкретный стейт по имени.
    /// </summary>
    public void OnStateEnter(string stateName, Action callback, int animatorIndex = 0)
    {
        int hash = Animator.StringToHash(stateName);
        StateTrackers[animatorIndex].OnStateEntered += incomingHash =>
        {
            if (incomingHash == hash) 
                callback();
        };
    }

    /// <summary>
    /// Подписаться на выход из конкретного стейта по имени.
    /// </summary>
    public void OnStateExit(string stateName, Action callback, int animatorIndex = 0)
    {
        int hash = Animator.StringToHash(stateName);
        StateTrackers[animatorIndex].OnStateExited += exitedHash =>
        {
            if (exitedHash == hash) 
                callback();
        };
    }

    /// <summary>
    /// Подписаться на любую смену стейта.
    /// </summary>
    public void OnAnyStateChange(Action<int, int> callback, int animatorIndex = 0)
    {
        int previousHash = 0;
        StateTrackers[animatorIndex].OnStateEntered += newHash =>
        {
            callback(previousHash, newHash);
            previousHash = newHash;
        };
    }
    
    /// <summary>
    /// Plays an animation.
    /// </summary>
    /// <param name="stateName">The state to play.</param>
    /// <param name="animatorIndex">The index of the animator from which get and play the state.</param>
    public void Play(string stateName, int animatorIndex = 0) => Animators[animatorIndex].Play(stateName);

    /// <summary>
    /// Plays an animation.
    /// </summary>
    /// <param name="animParam">The animation parameter.</param>
    /// <param name="state">The new state.</param>
    /// <param name="animatorIndex">The index of the animator to modify.</param>
    public void Play(string animParam, bool state, int animatorIndex = 0)
        => Animators[animatorIndex].SetBool(animParam, state);

    /// <summary>
    /// Plays an animation.
    /// </summary>
    /// <param name="stateName">The state to play.</param>
    /// <param name="animatorName">The name of the animator from which get and play the state.</param>
    public void Play(string stateName, string animatorName)
        => Animators.FirstOrDefault(x => x.name == animatorName)?.Play(stateName);

    /// <summary>
    /// Stops an animation.
    /// </summary>
    /// <param name="animatorIndex">The index of the animator from which get and stop the state.</param>
    public void Stop(int animatorIndex = 0) => Animators[animatorIndex].StopPlayback();

    /// <summary>
    /// Stops an animation.
    /// </summary>
    /// <param name="animatorName">The name of the animator from which get and stop the state.</param>
    public void Stop(string animatorName) => Animators.FirstOrDefault(x => x.name == animatorName)?.StopPlayback();

    /// <summary>
    /// Gets a <see cref="AnimationController"/> from the given <see cref="SchematicObject"/>.
    /// </summary>
    /// <param name="schematic">The schematic to check.</param>
    /// <returns>The corresponding <see cref="AnimationController"/>, or <see langword="null"/> if not found.</returns>
    public static AnimationController Get(SchematicObject schematic)
        => Dictionary.TryGetValue(schematic, out AnimationController animationController) ? animationController
            : new(schematic);

    /// <summary>
    /// A <see cref="Dictionary{TKey, TValue}"/> of <see cref="SchematicObject"/> and <see cref="AnimationController"/>
    /// <br>containing all the relative <see cref="AnimationController"/> for each <see cref="SchematicObject"/>.</br>
    /// </summary>
    public static readonly Dictionary<SchematicObject, AnimationController> Dictionary = new();
}