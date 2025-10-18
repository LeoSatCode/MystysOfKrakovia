using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject characterListPanel;
    [SerializeField] private GameObject creationPanel;

    [Header("UI da Lista de Personagens")]
    [SerializeField] private Transform characterListContent;
    [SerializeField] private GameObject characterButtonPrefab;
    [SerializeField] private Button enterWorldButton;

    [Header("UI da Criação de Personagens")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Dropdown classDropdown;

    private List<CharacterData> _characters;
    private CharacterData _selectedCharacter;

    void Start()
    {
        if (characterButtonPrefab != null)
            characterButtonPrefab.SetActive(false);

        if (enterWorldButton != null)
            enterWorldButton.interactable = false;

        _characters = SaveManager.LoadCharacters();

        if (_characters != null && _characters.Count > 0)
        {
            characterListPanel.SetActive(true);
            creationPanel.SetActive(false);
            RefreshCharacterListUI();
        }
        else
        {
            characterListPanel.SetActive(false);
            creationPanel.SetActive(true);
        }
    }

    private void RefreshCharacterListUI()
    {
        foreach (Transform child in characterListContent)
        {
            if (child.gameObject.activeSelf)
                Destroy(child.gameObject);
        }

        foreach (CharacterData character in _characters)
        {
            GameObject buttonGO = Instantiate(characterButtonPrefab, characterListContent);
            buttonGO.SetActive(true);
            
            TMP_Text buttonText = buttonGO.GetComponentInChildren<TMP_Text>();
            buttonText.text = $"{character.characterName} - {character.characterClass} (Nível {character.level})";
            
            Button button = buttonGO.GetComponent<Button>();
            button.onClick.AddListener(() => SelectCharacter(character));
            
            Button deleteButton = buttonGO.transform.Find("DeleteButton").GetComponent<Button>();
            deleteButton.onClick.AddListener(() => DeleteCharacter(character));
        }
        
        _selectedCharacter = null;
        if (enterWorldButton != null)
            enterWorldButton.interactable = false;
    }

    public void SelectCharacter(CharacterData character)
    {
        _selectedCharacter = character;
        if (enterWorldButton != null)
            enterWorldButton.interactable = true;
    }

    public void ShowCreationPanel()
    {
        creationPanel.SetActive(true);
        characterListPanel.SetActive(false);
    }

    public void ShowCharacterListPanel()
    {
        creationPanel.SetActive(false);
        characterListPanel.SetActive(true);
    }
    
    // ESTA É A FUNÇÃO MODIFICADA
    public void SaveNewCharacter()
    {
        if (string.IsNullOrEmpty(nameInputField.text)) { return; }

        // 1. Primeiro, criamos o objeto de dados com as informações básicas
        CharacterData newCharacter = new CharacterData
        {
            characterName = nameInputField.text,
            characterClass = classDropdown.options[classDropdown.value].text,
            level = 1,
            attributePoints = 0 // Personagens novos começam com 0 pontos para distribuir
        };

        // 2. Agora, usamos um 'switch' para definir os atributos com base na classe
        switch (newCharacter.characterClass)
        {
            case "Guerreiro":
                newCharacter.constitution = 12;
                newCharacter.strength = 10;
                newCharacter.intelligence = 6;
                newCharacter.dexterity = 8;
                break;

            case "Mago":
                newCharacter.constitution = 8;
                newCharacter.strength = 6;
                newCharacter.intelligence = 12;
                newCharacter.dexterity = 10;
                break;

            case "Arqueiro":
                newCharacter.constitution = 10;
                newCharacter.strength = 8;
                newCharacter.intelligence = 6;
                newCharacter.dexterity = 12;
                break;

            case "Clérigo":
                newCharacter.constitution = 10;
                newCharacter.strength = 8;
                newCharacter.intelligence = 10;
                newCharacter.dexterity = 8;
                break;

            default:
                Debug.LogWarning($"Classe '{newCharacter.characterClass}' não reconhecida! Usando atributos padrão.");
                newCharacter.constitution = 8;
                newCharacter.strength = 8;
                newCharacter.intelligence = 8;
                newCharacter.dexterity = 8;
                break;
        }

        if (_characters == null)
            _characters = new List<CharacterData>();

        _characters.Add(newCharacter);
        
        SaveManager.SaveCharacterList(_characters);

        creationPanel.SetActive(false);
        characterListPanel.SetActive(true);
        RefreshCharacterListUI();
    }

    public void DeleteCharacter(CharacterData characterToDelete)
    {
        _characters.Remove(characterToDelete);
        
        SaveManager.SaveCharacterList(_characters);
        
        RefreshCharacterListUI();

        if (_characters.Count == 0)
        {
            if (enterWorldButton != null)
                enterWorldButton.interactable = false;
        }
    }

    public void EnterWorld()
    {
        if (_selectedCharacter == null) { return; }

        GameManager.SelectedCharacter = _selectedCharacter;

        PhotonNetwork.NickName = _selectedCharacter.characterName;

        characterListPanel.SetActive(false);
        creationPanel.SetActive(false);
        
        // Assume que você tem um NetworkManager que lida com a conexão
        NetworkManager.Instance.JoinOrCreateRoom();
    }
}