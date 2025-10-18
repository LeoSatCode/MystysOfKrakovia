using System.Collections.Generic;

[System.Serializable]
public class CharacterData
{
    // Info Básica
    public string characterName;
    public string characterClass;

    // Progresso
    public int level;
    public int currentXP;
    public int attributePoints;
    public int constitution;
    public int strength;
    public int intelligence;
    public int dexterity;

    // Listas (salvamos os nomes ou IDs, não os objetos inteiros)
    public List<string> unlockedSkillNames = new List<string>();
    public string[] actionBarSkillNames = new string[5];
    public List<int> activeQuestIDs = new List<int>();
    public List<int> completedQuestIDs = new List<int>();

    // Inventário (precisamos de uma classe auxiliar)
    public List<InventorySaveData> inventory = new List<InventorySaveData>();
}

[System.Serializable]
public class InventorySaveData
{
    public string itemName;
    public int quantity;
}