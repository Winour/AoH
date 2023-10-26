using System;
using System.Collections;
using UnityEngine;

public static class DelayedActions
{
    public static void DelayedFrameAction(Action action)
    {
        CoroutinesHolder.Instance.StartCoroutine(IE_DelayedFrameAction(action));
    }

    private static IEnumerator IE_DelayedFrameAction(Action action)
    {
        yield return null;
        action();
    }

    public static void DelayedAction(Action action, float delay)
    {
        CoroutinesHolder.Instance.StartCoroutine(IE_DelayedAction(action, delay));
    }

    private static IEnumerator IE_DelayedAction(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action();
    }

    public static void DelayedActionUntil(Action action, Func<bool> delay)
    {
        CoroutinesHolder.Instance.StartCoroutine(IE_DelayedActionUntil(action, delay));
    }

    private static IEnumerator IE_DelayedActionUntil(Action action, Func<bool> delay)
    {
        yield return new WaitUntil(delay);
        action();
    }

    public static void DelayedParticleSystemAction(this ParticleSystem particleSystem, Action action)
    {
        var main = particleSystem.main;
        var timeLeft = main.duration;
        DelayedAction(action, timeLeft);
    }
}
