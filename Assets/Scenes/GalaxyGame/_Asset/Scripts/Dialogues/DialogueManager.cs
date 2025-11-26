using UnityEngine;
using Sirenix.OdinInspector;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private DialogueView view;
    [SerializeField] private float textSpeed = 0.03f;

    private DialogueConversation currentConversation;
    private int currentIndex;
    private bool isTyping;
    private string currentFullText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        view.SetActive(false);
    }

    public void StartDialogue(DialogueConversation conversation)
    {
        if (conversation == null || conversation.Nodes.Count == 0) return;

        currentConversation = conversation;
        currentIndex = 0;

        GameEvents.SetGameControlLock(true);
        view.SetActive(true);
        ProcessNode(currentConversation.GetNode(currentIndex));
    }

    public void HandleInput()
    {
        if (!view.gameObject.activeSelf) return;

        if (isTyping)
        {
            isTyping = false;
            view.DisplayFullText(currentFullText);
        }
        else
        {
            var currentNode = currentConversation.GetNode(currentIndex);
            // Chỉ cần check LineNode hoặc EventNode để đi tiếp
            if (currentNode is DialogueLineNode || currentNode is DialogueEventNode)
            {
                AdvanceNode();
            }
        }
    }

    private void Update()
    {
        if (view.gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
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
                view.ShowLine(line.Speaker, line.Text, textSpeed, () => isTyping = false);
                break;

            case DialogueChoiceNode choice:
                view.ShowChoices(choice.Options, OnChoiceSelected);
                break;

            case DialogueEventNode evt:
                // Fix lỗi CS1061: Gọi đúng biến Actions đã định nghĩa lại trong DialogueConversation
                if (evt.Actions != null)
                {
                    foreach (var action in evt.Actions) action.Execute();
                }
                AdvanceNode(); // Event chạy xong thì tự next
                break;
        }
    }

    private void OnChoiceSelected(int index)
    {
        var choiceNode = currentConversation.GetNode(currentIndex) as DialogueChoiceNode;
        if (choiceNode == null || index >= choiceNode.Options.Count) return;

        var selectedOption = choiceNode.Options[index];

        // Thực thi Actions của Choice
        if (selectedOption.OnSelectActions != null)
        {
            foreach (var action in selectedOption.OnSelectActions)
            {
                action.Execute();
            }
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
        view.SetActive(false);
        currentConversation = null;
        GameEvents.SetGameControlLock(false);
    }
}