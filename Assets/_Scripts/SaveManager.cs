using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Adicionado para facilitar a busca

public static class SaveManager
{
    private const string SAVE_KEY = "MyCharacters_V3"; // Mudamos a chave para não dar conflito com o save antigo

    [System.Serializable]
    private class CharacterList { public List<CharacterData> characters = new List<CharacterData>(); }

    public static void SaveSingleCharacter(CharacterData characterToSave)
    {
        List<CharacterData> allChars = LoadCharacters();
        int index = allChars.FindIndex(c => c.characterName == characterToSave.characterName);

        if (index != -1) // Se o personagem já existe na lista, atualiza-o
            allChars[index] = characterToSave;
        else // Se for um personagem novo
            allChars.Add(characterToSave);

        SaveCharacterList(allChars);
    }

    // Renomeamos a função antiga para ser mais clara
    public static void SaveCharacterList(List<CharacterData> characters)
    {
        CharacterList listWrapper = new CharacterList { characters = characters };
        string json = JsonUtility.ToJson(listWrapper);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public static List<CharacterData> LoadCharacters()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            return JsonUtility.FromJson<CharacterList>(json).characters;
        }
        return new List<CharacterData>();
    }
}