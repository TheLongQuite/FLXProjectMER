﻿using System;
using UnityEngine;

namespace ProjectMER.Features.Components;

public class AnimationStateTracker : MonoBehaviour
{
    private Animator _animator;
    private int _layerIndex;
    private int _previousStateHash;
    private bool _initialized;
    private bool _inTransition;

    public event Action<int> OnStateEntered;
    public event Action<int> OnStateExited;
    public event Action<int> OnTransitionStarted;

    public bool IsInitialized => _initialized;

    public int CurrentStateHash => _previousStateHash;

    public void Init(Animator animator, int layerIndex = 0)
    {
        _animator = animator;
        _layerIndex = layerIndex;
    }

    private void Update()
    {
        if (!_animator || !_initialized && !TryInitialize())
            return;

        bool isInTransition = _animator.IsInTransition(_layerIndex);
        if (isInTransition && !_inTransition)
        {
            _inTransition = true;
            OnTransitionStarted?.Invoke(_animator.GetNextAnimatorStateInfo(_layerIndex).shortNameHash);
        }
        else if (!isInTransition && _inTransition)
        {
            _inTransition = false;
        }

        int currentHash = _animator.GetCurrentAnimatorStateInfo(_layerIndex).shortNameHash;
        if (currentHash == _previousStateHash)
            return;

        OnStateExited?.Invoke(_previousStateHash);
        OnStateEntered?.Invoke(currentHash);
        _previousStateHash = currentHash;
    }

    private bool TryInitialize()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(_layerIndex);
        if (info.shortNameHash == 0)
            return false;

        _previousStateHash = info.shortNameHash;
        _initialized = true;
        
        OnStateEntered?.Invoke(_previousStateHash);
        return true;
    }

    public static AnimationStateTracker GetOrCreate(Animator animator, int layerIndex = 0)
    {
        if (animator.TryGetComponent(out AnimationStateTracker tracker))
            return tracker;

        tracker = animator.gameObject.AddComponent<AnimationStateTracker>();
        tracker.Init(animator, layerIndex);
        return tracker;
    }
}