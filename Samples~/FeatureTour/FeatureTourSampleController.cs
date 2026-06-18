using System.Collections.Generic;
using UnityEngine;
using TMPro;
using kkmia.TalkSystem;

public sealed class FeatureTourSampleController : MonoBehaviour, IDialogueVariableResolver, IDialogueConditionEvaluator
{
    [SerializeField] private string playerName = "Player";
    [SerializeField] private bool canTakeLeftRoute = true;
    [SerializeField] private TMP_Text eventLogText;
    [SerializeField] private TMP_Text backlogText;
    [SerializeField] private GameObject questFlagObject;
    [SerializeField] private GameObject timelineMarkerObject;
    [SerializeField] private Animator sampleAnimator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip routeSelectedClip;

    private readonly List<string> _events = new List<string>();
    private DialogueSaveData _savedState;
    private bool _questFlag;

    private void Start()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.SetVariableResolver(this);
        DialogueManager.Instance.SetConditionEvaluator(this);
        DialogueManager.Instance.DialogueEventTriggered += OnDialogueEvent;
        DialogueManager.Instance.LineCompleted += OnLineCompleted;
        DialogueManager.Instance.StartDialogueForState("SampleStart");
        RefreshSampleState();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.DialogueEventTriggered -= OnDialogueEvent;
        DialogueManager.Instance.LineCompleted -= OnLineCompleted;
    }

    public bool TryResolve(string variableName, DialogueData data, out string value)
    {
        if (variableName == "playerName")
        {
            value = playerName;
            return true;
        }

        value = null;
        return false;
    }

    public bool Evaluate(string conditionKey, DialogueData data)
    {
        if (conditionKey == "can_take_left")
            return canTakeLeftRoute;

        return true;
    }

    public void SaveDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            _savedState = DialogueManager.Instance.CaptureState();
            AddEventLog("saved_state");
        }
    }

    public void RestoreDialogue()
    {
        if (DialogueManager.Instance != null && _savedState != null)
        {
            DialogueManager.Instance.RestoreState(_savedState);
            AddEventLog("restored_state");
            RefreshBacklog();
        }
    }

    public void ToggleLeftRoute()
    {
        canTakeLeftRoute = !canTakeLeftRoute;
        AddEventLog(canTakeLeftRoute ? "left_route_enabled" : "left_route_disabled");
    }

    private void OnDialogueEvent(DialogueEventContext context)
    {
        if (context != null && !string.IsNullOrEmpty(context.EventKey))
        {
            ApplySampleEvent(context.EventKey);
            AddEventLog(context.EventKey);
        }
    }

    private void OnLineCompleted(DialogueEventContext context)
    {
        RefreshBacklog();
    }

    private void ApplySampleEvent(string eventKey)
    {
        if (eventKey == "sample_started")
        {
            _questFlag = false;
            if (timelineMarkerObject != null)
                timelineMarkerObject.SetActive(false);
        }
        else if (eventKey == "left_route" || eventKey == "right_route")
        {
            _questFlag = true;
            if (audioSource != null && routeSelectedClip != null)
                audioSource.PlayOneShot(routeSelectedClip);
        }
        else if (eventKey == "sample_finished")
        {
            if (timelineMarkerObject != null)
                timelineMarkerObject.SetActive(true);

            if (sampleAnimator != null)
                sampleAnimator.SetTrigger("DialogueFinished");
        }

        RefreshSampleState();
    }

    private void RefreshSampleState()
    {
        if (questFlagObject != null)
            questFlagObject.SetActive(_questFlag);
    }

    private void AddEventLog(string eventKey)
    {
        _events.Add(eventKey);

        if (eventLogText == null) return;
        eventLogText.text = string.Join("\n", _events.ToArray());
    }

    private void RefreshBacklog()
    {
        if (backlogText == null || DialogueManager.Instance == null) return;

        var lines = new List<string>();
        foreach (var entry in DialogueManager.Instance.History)
            lines.Add(entry.Speaker + ": " + entry.Text);

        backlogText.text = string.Join("\n", lines.ToArray());
    }
}
