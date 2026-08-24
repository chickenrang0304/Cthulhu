
using UnityEngine;
using Yarn.Unity;

public class FilterManager : MonoBehaviour
{
    public static FilterManager Instance { get; private set; }

    [SerializeField] private DialogueRunner dialogueRunner;
    private const string VariableName = "$filterOn";

    public bool FilterOn { get; private set; } = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (dialogueRunner.VariableStorage.TryGetValue<bool>(VariableName, out bool value))
        {
            FilterOn = value;
        }
        else
        {
            dialogueRunner.VariableStorage.SetValue(VariableName, FilterOn);
        }
    }

    public void ToggleFilter()
    {
        FilterOn = !FilterOn;
        dialogueRunner.VariableStorage.SetValue(VariableName, FilterOn);

        foreach (var tracker in FilterableOptionText.ActiveTrackers)
        {
            tracker.RefreshText(FilterOn);
        }

        if (FilterableLastLine.Instance != null)
        {
            FilterableLastLine.Instance.RefreshText(FilterOn);
        }
    }
}