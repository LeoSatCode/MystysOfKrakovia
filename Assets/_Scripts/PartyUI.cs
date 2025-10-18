using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PartyUI : MonoBehaviour
{
    public static PartyUI Instance { get; private set; }

    [Header("Referências da UI")]
    [SerializeField] private GameObject partyPanel; // O painel principal que contém tudo
    [SerializeField] private Transform memberFrameContainer; // O objeto com o Vertical Layout Group
    [SerializeField] private GameObject partyMemberFramePrefab; // Nosso prefab criado na Parte 1
    [SerializeField] private GameObject leavePartyButton; // Um botão que você deve criar na sua UI

    void Awake() => Instance = this;

    void Start()
    {
        partyPanel.SetActive(false); // Começa escondido
    }

    public void UpdatePartyFrame(Party party)
    {
        // Se o grupo não existe ou só tem 1 pessoa (você), esconde o painel.
        if (party == null || party.Members.Count <= 1)
        {
            partyPanel.SetActive(false);
            return;
        }

        partyPanel.SetActive(true);

        // 1. Limpa os frames antigos
        foreach (Transform child in memberFrameContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Cria um frame novo para cada membro (que não seja eu)
        foreach (Player member in party.Members)
        {
            if (member.IsLocal) continue; // Pula o jogador local

            GameObject frameGO = Instantiate(partyMemberFramePrefab, memberFrameContainer);
            frameGO.GetComponent<PartyMemberUIFrame>().Initialize(member);
        }
    }

    // Função para ser chamada pelo OnClick do seu botão de sair do grupo
    public void OnLeavePartyButtonClicked()
    {
        if(PartyManager.Instance != null)
        {
            PartyManager.Instance.LeaveParty();
        }
    }
}