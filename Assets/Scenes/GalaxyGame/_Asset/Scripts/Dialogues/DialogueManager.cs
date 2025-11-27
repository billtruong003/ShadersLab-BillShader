using UnityEngine;
using Sirenix.OdinInspector;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Title("Settings")]
    [EnumToggleButtons]
    [SerializeField] private DialogueViewMode defaultMode = DialogueViewMode.ScreenSpace;
    [SerializeField] private float textSpeed = 0.03f;

    [Title("Views")]
    [SerializeField] private DialogueViewScreen screenView;
    [SerializeField] private DialogueViewWorld worldView;

    private DialogueConversation currentConversation;
    private DialogueViewBase activeView;
    private int currentIndex;
    private bool isTyping;
    private string currentFullText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (screenView != null) screenView.Initialize();
        if (worldView != null) worldView.Initialize();
    }

    public void StartDialogue(DialogueConversation conversation)
    {
        if (conversation == null || conversation.Nodes.Count == 0) return;

        currentConversation = conversation;
        currentIndex = 0;

        // Quyết định dùng View nào
        DialogueViewMode targetMode = conversation.OverrideGlobalMode ? conversation.ViewMode : defaultMode;
        SwitchView(targetMode);

        GameEvents.SetGameControlLock(true);
        activeView.SetActive(true);
        ProcessNode(currentConversation.GetNode(currentIndex));
    }

    private void SwitchView(DialogueViewMode mode)
    {
        // Tắt view cũ nếu đang bật
        if (activeView != null) activeView.SetActive(false);

        activeView = mode == DialogueViewMode.ScreenSpace ? screenView : worldView;

        // Đảm bảo view kia tắt hoàn toàn
        if (mode == DialogueViewMode.ScreenSpace) worldView.SetActive(false);
        else screenView.SetActive(false);
    }

    public void HandleInput()
    {
        if (activeView == null || !activeView.gameObject.activeInHierarchy) return;

        if (isTyping)
        {
            isTyping = false;
            activeView.DisplayFullText(currentFullText);
        }
        else
        {
            var currentNode = currentConversation.GetNode(currentIndex);
            if (currentNode is DialogueLineNode || currentNode is DialogueEventNode)
            {
                AdvanceNode();
            }
        }
    }

    private void Update()
    {
        bool inputActive = activeView != null && activeView.gameObject.activeInHierarchy;
        if (inputActive && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            HandleInput();
        }
    }

    private void AdvanceNode()
    {
        currentIndex++;
        if (currentIndex < currentConversation.Nodes.Count)
        {
            ProcessNode(currentConversation.GetNode(currentIndex));
        }
        else
        {
            EndDialogue();
        }
    }

    private void ProcessNode(IDialogueNode node)
    {
        switch (node)
        {
            case DialogueLineNode line:
                currentFullText = line.Text;
                isTyping = true;
                activeView.ShowLine(line.Speaker, line.Text, textSpeed, () => isTyping = false);
                break;

            case DialogueChoiceNode choice:
                activeView.ShowChoices(choice.Options, OnChoiceSelected);
                break;

            case DialogueEventNode evt:
                if (evt.Actions != null) foreach (var action in evt.Actions) action.Execute();
                AdvanceNode();
                break;
        }
    }

    private void OnChoiceSelected(int index)
    {
        var choiceNode = currentConversation.GetNode(currentIndex) as DialogueChoiceNode;
        if (choiceNode == null || index >= choiceNode.Options.Count) return;

        var selectedOption = choiceNode.Options[index];

        if (selectedOption.OnSelectActions != null)
        {
            foreach (var action in selectedOption.OnSelectActions) action.Execute();
        }

        if (selectedOption.BranchToConversation != null)
        {
            StartDialogue(selectedOption.BranchToConversation);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        if (activeView != null) activeView.SetActive(false);
        currentConversation = null;
        activeView = null;
        GameEvents.SetGameControlLock(false);
    }
}