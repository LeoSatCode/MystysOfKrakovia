using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject responsesContent;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button continueButton;

    void Awake() => Instance = this;

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // CORREÇÃO: A assinatura agora tem 5 parâmetros para cobrir todos os casos
    public void StartDialogue(string npcName, string[] lines, int questID, bool showResponseButtons, bool isCompletion)
    {
        dialoguePanel.SetActive(true);
        npcNameText.text = npcName;
        dialogueText.text = lines.Length > 0 ? lines[0] : "...";

        bool isQuestOffer = showResponseButtons && !isCompletion;
        bool isQuestTurnIn = showResponseButtons && isCompletion;

        responsesContent.SetActive(isQuestOffer || isQuestTurnIn);
        continueButton.gameObject.SetActive(!showResponseButtons);

        // Limpa os cliques para evitar bugs
        acceptButton.onClick.RemoveAllListeners();
        declineButton.onClick.RemoveAllListeners();
        continueButton.onClick.RemoveAllListeners();

        if (isQuestOffer)
        {
            acceptButton.gameObject.SetActive(true);
            declineButton.gameObject.SetActive(true);
            
            acceptButton.GetComponentInChildren<TMP_Text>().text = "Aceitar";
            acceptButton.onClick.AddListener(() => {
                QuestManager.Instance.AcceptQuest(questID);
                EndDialogue();
            });

            declineButton.GetComponentInChildren<TMP_Text>().text = "Recusar";
            declineButton.onClick.AddListener(EndDialogue);
        }
        else if (isQuestTurnIn)
        {
            acceptButton.gameObject.SetActive(true);
            declineButton.gameObject.SetActive(false); // Esconde o botão de recusar

            acceptButton.GetComponentInChildren<TMP_Text>().text = "Completar Missão";
            acceptButton.onClick.AddListener(() => {
                QuestManager.Instance.CompleteQuest(questID);
                EndDialogue();
            });
        }
        else // Conversa normal
        {
            continueButton.onClick.AddListener(EndDialogue);
        }
    }

    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}