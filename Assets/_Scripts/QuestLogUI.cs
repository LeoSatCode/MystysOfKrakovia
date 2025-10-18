using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI Instance { get; private set; }

    [SerializeField] private Transform contentParent; // O nosso "Quests_Content"
    [SerializeField] private GameObject questEntryPrefab; // O nosso "QuestEntry_Template"

    void Awake()
    {
        Instance = this;
    }

    // Esta função é chamada automaticamente quando o objeto é ativado
    void OnEnable()
    {
        UpdateQuestLog();
    }

    public void UpdateQuestLog()
    {
        // Se o QuestManager ainda não estiver pronto, não faz nada
        if (QuestManager.Instance == null) return;

        // Limpa a lista antiga de missões na UI
        foreach (Transform child in contentParent)
        {
            // Garante que não destruímos o nosso "molde" (prefab)
            if (child != questEntryPrefab.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Pede ao QuestManager a lista de missões ativas
        Dictionary<int, ActiveQuest> quests = QuestManager.Instance.activeQuests;

        // Cria uma entrada na UI para cada missão ativa
        foreach (var activeQuest in quests.Values)
        {
            Quest questData = QuestManager.Instance.GetQuestByID(activeQuest.questID);
            if (questData != null)
            {
                GameObject entry = Instantiate(questEntryPrefab, contentParent);
                entry.SetActive(true);

                TMP_Text text = entry.GetComponent<TMP_Text>();
                // Monta o texto da missão com o progresso atual
                text.text = $"{questData.title}\n- {questData.objective} {activeQuest.currentAmount} / {questData.requiredAmount}";
            }
        }
    }
}