using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class Quest
{
    public int id;
    public string title;
    public string objective;
    public int requiredAmount;
    public enum QuestType { Kill, Interact }
    public QuestType questType;
    public int xpReward;
}

public class ActiveQuest
{
    public int questID;
    public int currentAmount;
    public bool isComplete;
}

public class QuestManager : MonoBehaviourPun
{
    public static QuestManager Instance { get; private set; }

    [Header("Recompensas de Missão (Itens)")]
    // --- LINHA QUE FALTAVA ---
    public ItemData fragmentoDoSeloItem;

    [Header("Recompensas de Missão (Skills)")]
    public ClassSkillReward quest1ClassRewards;

    [Header("Referências da Cena")]
    public GameObject portalCemiterio;

    public Dictionary<int, ActiveQuest> activeQuests = new Dictionary<int, ActiveQuest>();
    public List<int> completedQuests = new List<int>();
    private List<Quest> _allQuests = new List<Quest>();

    void Awake()
    {
        Instance = this;
        InitializeAllQuests();
    }

    private void InitializeAllQuests()
    {
        _allQuests.Add(new Quest { id = 1, title = "Limpando o Cemitério", objective = "Zumbis Derrotados:", requiredAmount = 5, questType = Quest.QuestType.Kill, xpReward = 100 });
        _allQuests.Add(new Quest { id = 2, title = "A Origem da Névoa", objective = "Totens Profanos Destruídos:", requiredAmount = 3, questType = Quest.QuestType.Interact, xpReward = 150 });
        _allQuests.Add(new Quest { id = 3, title = "A Sabedoria dos Anciãos", objective = "Fale com Elara em Vilarosto.", requiredAmount = 1, questType = Quest.QuestType.Interact, xpReward = 50 });
        _allQuests.Add(new Quest { id = 4, title = "O Lamento da Guardiã", objective = "Espectro do Desespero Derrotado:", requiredAmount = 1, questType = Quest.QuestType.Kill, xpReward = 300 });
        _allQuests.Add(new Quest { id = 5, title = "A Quebra do Selo", objective = "Entregue o Fragmento do Selo para o Capitão Valerius.", requiredAmount = 1, questType = Quest.QuestType.Interact, xpReward = 500 });
    }

    public Quest GetQuestByID(int id)
    {
        return _allQuests.Find(q => q.id == id);
    }

    public void AcceptQuest(int questID)
    {
        if (completedQuests.Contains(questID) || activeQuests.ContainsKey(questID)) return;
        ActiveQuest newQuest = new ActiveQuest { questID = questID, currentAmount = 0, isComplete = false };
        activeQuests.Add(questID, newQuest);
        if (QuestLogUI.Instance != null)
            QuestLogUI.Instance.UpdateQuestLog();
    }

    public void AddQuestProgress_Kill(string enemyName, int amount)
    {
        foreach (ActiveQuest quest in new List<ActiveQuest>(activeQuests.Values))
        {
            if (quest.isComplete) continue;
            Quest questData = GetQuestByID(quest.questID);
            bool questProgressed = false;

            if (questData.id == 1 && questData.questType == Quest.QuestType.Kill && enemyName.Contains("Zombie"))
            {
                quest.currentAmount += amount;
                questProgressed = true;
            }
            else if (questData.id == 4 && questData.questType == Quest.QuestType.Kill && enemyName.Contains("EspectroDoDesespero"))
            {
                quest.currentAmount += amount;
                questProgressed = true;
            }

            if (questProgressed)
            {
                if (quest.currentAmount >= questData.requiredAmount)
                {
                    quest.isComplete = true;
                    if (questData.id == 4)
                    {
                        completedQuests.Add(quest.questID);
                        activeQuests.Remove(quest.questID);
                        AcceptQuest(5);
                    }
                }
                if (QuestLogUI.Instance != null) QuestLogUI.Instance.UpdateQuestLog();
            }
        }
    }

    public void AddQuestProgress_Interact(int questID)
    {
        if (activeQuests.ContainsKey(questID) && !activeQuests[questID].isComplete)
        {
            ActiveQuest quest = activeQuests[questID];
            quest.currentAmount++;
            if (quest.currentAmount >= GetQuestByID(questID).requiredAmount)
            {
                quest.isComplete = true;
            }
            if (QuestLogUI.Instance != null)
                QuestLogUI.Instance.UpdateQuestLog();
        }
    }

    public void CompleteQuest(int questID)
    {
        if (activeQuests.ContainsKey(questID))
        {
            ActiveQuest quest = activeQuests[questID];
            Quest questData = GetQuestByID(questID);

            if (quest.isComplete || questData.questType == Quest.QuestType.Interact)
            {
                foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                {
                    if (player.GetComponent<PhotonView>().IsMine)
                    {
                        if (questID == 1 && quest1ClassRewards != null)
                        {
                            string playerClass = GameManager.SelectedCharacter.characterClass;
                            Skill skillToGive = quest1ClassRewards.GetSkillForClass(playerClass);
                            if (skillToGive != null) { player.UnlockSkill(skillToGive); }
                        }

                        if (questData.xpReward > 0) { player.AddXP(questData.xpReward); }

                        if (questID == 5 && fragmentoDoSeloItem != null)
                        {
                            InventoryManager.Instance.RemoveItem(fragmentoDoSeloItem);
                            if (portalCemiterio != null)
                            {
                                PortalController portal = portalCemiterio.GetComponent<PortalController>();
                                if (portal != null)
                                {
                                    portal.ActivatePortalForAll();
                                }
                                else
                                {
                                    Debug.LogError("O objeto do portal foi encontrado, mas o script PortalController não está nele!");
                                }
                            }
                        }
                        break;
                    }
                }

                activeQuests.Remove(questID);
                completedQuests.Add(questID);

                if (QuestLogUI.Instance != null)
                    QuestLogUI.Instance.UpdateQuestLog();
            }
        }
    }

    // Adicione estas duas funções ao seu QuestManager.cs
    public void UpdateSaveData(CharacterData saveData)
    {
        saveData.activeQuestIDs = new List<int>(activeQuests.Keys);
        saveData.completedQuestIDs = new List<int>(completedQuests);
    }

    public void LoadFromSaveData(CharacterData saveData)
    {
        activeQuests.Clear();
        foreach (int id in saveData.activeQuestIDs)
        {
            // Precisamos reconstruir o 'ActiveQuest' a partir do ID
            // Esta parte da lógica precisa ser mais robusta no futuro, mas por agora funciona.
            activeQuests.Add(id, new ActiveQuest { questID = id, currentAmount = 0, isComplete = false });
        }

        completedQuests = new List<int>(saveData.completedQuestIDs);
        if (QuestLogUI.Instance != null) QuestLogUI.Instance.UpdateQuestLog();
    }
}