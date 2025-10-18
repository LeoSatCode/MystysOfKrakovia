using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NPCController : MonoBehaviour
{
    public string npcName;
    public List<int> questIDsToGive = new List<int>();

    [Header("Diálogos da Quest 1")]
    [TextArea(3, 5)] public string[] dialogueQuest1_Available;
    [TextArea(3, 5)] public string[] dialogueQuest1_InProgress;
    [TextArea(3, 5)] public string[] dialogueQuest1_Complete;

    [Header("Diálogos da Quest 2")]
    [TextArea(3, 5)] public string[] dialogueQuest2_Available;
    [TextArea(3, 5)] public string[] dialogueQuest2_InProgress;
    [TextArea(3, 5)] public string[] dialogueQuest2_Complete;
    
    [Header("Diálogos da Quest 3")]
    [TextArea(3, 5)] public string[] dialogueQuest3_Available;
    [TextArea(3, 5)] public string[] dialogueQuest3_InProgress;

    [Header("Diálogos da Quest 4")]
    [TextArea(3, 5)] public string[] dialogueQuest4_Available;
    [TextArea(3, 5)] public string[] dialogueQuest4_InProgress;

    [Header("Diálogos da Quest 5")]
    [TextArea(3, 5)] public string[] dialogueQuest5_InProgress;
    [TextArea(3, 5)] public string[] dialogueQuest5_Complete;

    [Header("Diálogo Padrão/Final")]
    [TextArea(3, 5)] public string[] dialogueAllQuestsDone;

    public void Interact()
    {
        if (QuestManager.Instance == null || DialogueManager.Instance == null) return;

        QuestManager qm = QuestManager.Instance;

        // --- VERIFICAÇÃO ESPECIAL PARA A MISSÃO 5 (MODO DETETIVE) ---
        if (npcName == "Capitão Valerius" && qm.activeQuests.ContainsKey(5))
        {
            Debug.Log("--- INICIANDO DIAGNÓSTICO DA MISSÃO 5 ---");

            if (qm.fragmentoDoSeloItem != null)
                Debug.Log($"1. O QuestManager está procurando pelo item: '{qm.fragmentoDoSeloItem.name}' (ID: {qm.fragmentoDoSeloItem.GetInstanceID()})");
            else
                Debug.Log("1. ERRO: O item 'fragmentoDoSeloItem' não está configurado no QuestManager!");

            bool playerHasItem = InventoryManager.Instance.HasItem(qm.fragmentoDoSeloItem);
            Debug.Log($"2. A função InventoryManager.HasItem() retornou: {playerHasItem}");

            if (InventoryManager.Instance.inventory.Count > 0)
            {
                // Mostra o nome e ID do primeiro item no inventário para comparar
                Debug.Log($"3. O primeiro item no inventário é: '{InventoryManager.Instance.inventory[0].item.name}' (ID: {InventoryManager.Instance.inventory[0].item.GetInstanceID()})");
            }
            else
            {
                Debug.Log("3. Seu inventário está vazio.");
            }
            Debug.Log("--- FIM DO DIAGNÓSTICO ---");

            if (playerHasItem)
            {
                qm.activeQuests[5].isComplete = true;
            }
        }
        
        if (npcName == "Elara" && qm.activeQuests.ContainsKey(3) && !qm.activeQuests[3].isComplete)
        {
            qm.CompleteQuest(3);
        }
        
        if (TryTurnInCompletedQuest(1, dialogueQuest1_Complete)) return;
        if (TryTurnInCompletedQuest(2, dialogueQuest2_Complete)) return;
        if (TryTurnInCompletedQuest(5, dialogueQuest5_Complete)) return;
        
        if (TryShowInProgressDialogue(1, dialogueQuest1_InProgress)) return;
        if (TryShowInProgressDialogue(2, dialogueQuest2_InProgress)) return;
        if (TryShowInProgressDialogue(3, dialogueQuest3_InProgress)) return;
        if (TryShowInProgressDialogue(4, dialogueQuest4_InProgress)) return;
        if (TryShowInProgressDialogue(5, dialogueQuest5_InProgress)) return;
        
        int nextQuestID = questIDsToGive.FirstOrDefault(id => !qm.completedQuests.Contains(id) && !qm.activeQuests.ContainsKey(id));
        if (nextQuestID != 0)
        {
            if (TryOfferQuest(nextQuestID)) return;
        }
        
        if (dialogueAllQuestsDone.Length > 0)
            DialogueManager.Instance.StartDialogue(npcName, dialogueAllQuestsDone, 0, false, false);
    }

    private bool TryTurnInCompletedQuest(int questID, string[] dialogue)
    {
        if (dialogue.Length > 0 && QuestManager.Instance.activeQuests.ContainsKey(questID) && QuestManager.Instance.activeQuests[questID].isComplete)
        {
            DialogueManager.Instance.StartDialogue(npcName, dialogue, questID, true, true);
            return true;
        }
        return false;
    }

    private bool TryShowInProgressDialogue(int questID, string[] dialogue)
    {
        if (dialogue.Length > 0 && QuestManager.Instance.activeQuests.ContainsKey(questID))
        {
            DialogueManager.Instance.StartDialogue(npcName, dialogue, 0, false, false);
            return true;
        }
        return false;
    }

    private bool TryOfferQuest(int questID)
    {
        string[] dialogue = null;
        switch(questID)
        {
            case 1: dialogue = dialogueQuest1_Available; break;
            case 2: dialogue = dialogueQuest2_Available; break;
            case 3: dialogue = dialogueQuest3_Available; break;
            case 4: dialogue = dialogueQuest4_Available; break;
        }

        if (dialogue != null && dialogue.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(npcName, dialogue, questID, true, false);
            return true;
        }
        return false;
    }
}