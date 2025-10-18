using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static CharacterData SelectedCharacter { get; set; }
    public static Camera LocalPlayerCamera { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    // Função para ser chamada quando quisermos salvar
    public void SaveGame()
    {
        if (SelectedCharacter == null) return;

        // Encontra os componentes do jogador local
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        QuestManager qm = FindFirstObjectByType<QuestManager>();
        InventoryManager im = FindFirstObjectByType<InventoryManager>();

        // Pede a cada um para atualizar os dados do nosso "arquivo de save"
        pc?.UpdateSaveData(SelectedCharacter);
        qm?.UpdateSaveData(SelectedCharacter);
        im?.UpdateSaveData(SelectedCharacter);

        // Chama o SaveManager para escrever os dados no disco
        SaveManager.SaveSingleCharacter(SelectedCharacter);
        Debug.Log($"Progresso do personagem '{SelectedCharacter.characterName}' foi salvo!");
    }

    // Salva o jogo automaticamente quando o aplicativo fecha
    private void OnApplicationQuit()
    {
        SaveGame();
    }
}