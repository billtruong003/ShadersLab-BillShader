using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Data/Conversation")]
public class DialogueConversation : ScriptableObject
{
    [BoxGroup("Settings")]
    [EnumToggleButtons]
    public DialogueViewMode ViewMode = DialogueViewMode.ScreenSpace;

    [BoxGroup("Settings")]
    public bool OverrideGlobalMode = false;

    [SerializeReference, ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "GetLabel")]
    public List<IDialogueNode> Nodes = new List<IDialogueNode>();

    public IDialogueNode GetNode(int index) => (index >= 0 && index < Nodes.Count) ? Nodes[index] : null;
}

public interface IDialogueNode
{
    string GetLabel();
}

[Serializable, LabelWidth(100)]
public class DialogueLineNode : IDialogueNode
{
    [HorizontalGroup("Split", 60), PreviewField(60, ObjectFieldAlignment.Left), HideLabel]
    [ReadOnly] public Sprite IconPreview;

    [HorizontalGroup("Split")]
    [VerticalGroup("Split/Right")]
    [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
    public CharacterProfile Speaker;

    [VerticalGroup("Split/Right")]
    [TextArea(3, 10), HideLabel]
    public string Text;

    public string GetLabel() => Speaker != null ? $"{Speaker.CharacterName}: {Text}" : "Empty Line";

    [OnInspectorInit]
    private void UpdatePreview() { if (Speaker != null) IconPreview = Speaker.Portrait; }
}

[Serializable]
public class DialogueChoiceNode : IDialogueNode
{
    [ListDrawerSettings(ShowFoldout = true)]
    public List<ChoiceOption> Options = new List<ChoiceOption>();

    public string GetLabel() => $"CHOICE: {Options.Count} Options";

    [Serializable]
    public struct ChoiceOption
    {
        [TextArea(2, 4)] public string Text;

        // Thay UnityEvent bằng List Action
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = true)]
        public List<DialogueAction> OnSelectActions;

        [AssetsOnly]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        public DialogueConversation BranchToConversation;
    }
}

[Serializable]
public class DialogueEventNode : IDialogueNode
{
    [SerializeReference]
    [ListDrawerSettings(ShowFoldout = true)]
    public List<DialogueAction> Actions = new List<DialogueAction>();

    public string GetLabel() => "EVENT: Execute Actions";
}