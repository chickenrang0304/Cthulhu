using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;

/// <summary>Shares pause ownership between settings and history, including Yarn keyboard input.</summary>
public static class DialogueMenuPause
{
    private static readonly HashSet<Object> Owners = new();
    private static readonly List<MonoBehaviour> SuspendedInputs = new();
    private static float previousTimeScale = 1f;
    private static int resumeAfterFrame = -1;
    private static bool restoring;
    private static GameObject previousSelection;

    public static bool IsBlockingInput => Owners.Count > 0 || Time.frameCount <= resumeAfterFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Owners.Clear();
        SuspendedInputs.Clear();
        previousTimeScale = 1f;
        resumeAfterFrame = -1;
        restoring = false;
        previousSelection = null;
    }

    public static void Acquire(Object owner)
    {
        if (!Owners.Add(owner) || Owners.Count != 1) return;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (EventSystem.current != null)
        {
            previousSelection = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null);
        }
        // Disabling LineAdvancer alone does not stop its input providers or InputAction callbacks.
        if (SuspendedInputs.Count != 0) return;
        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (!behaviour.enabled || !(behaviour is ILineAdvancerInput input)) continue;
            input.OnDialogueComplete();
            behaviour.enabled = false;
            SuspendedInputs.Add(behaviour);
        }
    }

    public static void Release(Object owner)
    {
        if (!Owners.Remove(owner) || Owners.Count != 0) return;
        Time.timeScale = previousTimeScale;
        resumeAfterFrame = Time.frameCount + 1;
        if (!restoring) RestoreInputAsync().Forget();
    }

    private static async YarnTask RestoreInputAsync()
    {
        restoring = true;
        // The click/ESC used to close a menu must not also advance the story.
        while (Owners.Count > 0 || Time.frameCount <= resumeAfterFrame)
            await YarnTask.Yield();
        foreach (var behaviour in SuspendedInputs)
        {
            if (behaviour == null) continue;
            behaviour.enabled = true;
            var input = (ILineAdvancerInput)behaviour;
            var runner = behaviour.GetComponentInParent<DialogueRunner>();
            if (runner == null) runner = Object.FindFirstObjectByType<DialogueRunner>();
            if (runner != null && runner.IsDialogueRunning) input.OnDialogueStarted();
        }
        SuspendedInputs.Clear();
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        previousSelection = null;
        restoring = false;
    }
}
