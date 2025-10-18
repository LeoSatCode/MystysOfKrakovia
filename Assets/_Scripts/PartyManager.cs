// PartyManager.cs (versão completa e fundida)
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Party
{
    public Player Leader { get; private set; }
    public List<Player> Members { get; private set; }

    public Party(Player leader)
    {
        Leader = leader;
        Members = new List<Player> { leader };
    }

    public void AddMember(Player newMember)
    {
        if (!Members.Contains(newMember))
        {
            Members.Add(newMember);
        }
    }

    public void RemoveMember(Player member)
    {
        if (Members.Contains(member))
        {
            Members.Remove(member);
        }
    }

    public bool IsMember(Player player)
    {
        return Members.Contains(player);
    }
}

[RequireComponent(typeof(PhotonView))]
public class PartyManager : MonoBehaviourPunCallbacks
{
    public static PartyManager Instance { get; private set; }

    private Dictionary<Player, Party> _parties = new Dictionary<Player, Party>();
    private Party _myParty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SendInvite(Player targetPlayer)
    {
        if (targetPlayer == null || targetPlayer.IsLocal)
        {
            Debug.LogError("Alvo do convite é inválido.");
            return;
        }

        if (FindPartyOfPlayer(targetPlayer) != null)
        {
            Debug.LogWarning($"{targetPlayer.NickName} já está em um grupo.");
            return;
        }

        Debug.Log($"Enviando convite de {PhotonNetwork.LocalPlayer.NickName} para {targetPlayer.NickName}");
        photonView.RPC("RPC_ReceiveInvite", targetPlayer, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void RPC_ReceiveInvite(Player invitingPlayer, PhotonMessageInfo info)
    {
        Debug.Log($"Recebemos um convite de {invitingPlayer.NickName}");
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowInvitationPanel(invitingPlayer);
        }
    }

    public void RespondToInvite(Player invitingPlayer, bool accepted)
    {
        photonView.RPC("RPC_ReceiveInviteResponse", invitingPlayer, PhotonNetwork.LocalPlayer, accepted);
    }

    // --- Lógica de resposta e sincronização ---
    [PunRPC]
    private void RPC_ReceiveInviteResponse(Player respondingPlayer, bool accepted, PhotonMessageInfo info)
    {
        if (!accepted)
        {
            Debug.Log($"{respondingPlayer.NickName} recusou o convite.");
            return;
        }

        Debug.Log($"{respondingPlayer.NickName} aceitou o convite! Formando/adicionando ao grupo.");

        Player leader = PhotonNetwork.LocalPlayer;

        if (!_parties.ContainsKey(leader))
        {
            _parties[leader] = new Party(leader);
        }

        Party myParty = _parties[leader];
        myParty.AddMember(respondingPlayer);

        // --- NOVO: sincronização centralizada ---
        SyncPartyStateForAllMembers(myParty);
    }

    // --- NOVO: Função centralizada de sincronização ---
    private void SyncPartyStateForAllMembers(Party party)
    {
        if (party == null) return;
        int[] memberActorNumbersArray = party.Members.Select(p => p.ActorNumber).ToArray();
        foreach (Player member in party.Members)
        {
            photonView.RPC("RPC_UpdatePartyState", member, memberActorNumbersArray);
        }
    }

    // --- Atualização de estado do grupo ---
    [PunRPC]
    private void RPC_UpdatePartyState(int[] memberActorNumbers)
    {
        List<Player> members = new List<Player>();
        foreach (int actorNumber in memberActorNumbers)
        {
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (player != null)
            {
                members.Add(player);
            }
        }

        if (members.Count > 0)
        {
            _myParty = new Party(members[0]);
            _myParty.Members.Clear();
            _myParty.Members.AddRange(members);

            Debug.Log($"Meu grupo foi atualizado. Membros: {string.Join(", ", _myParty.Members.Select(p => p.NickName))}");

            // --- PONTO DE CONEXÃO COM A UI ---
            if (PartyUI.Instance != null)
            {
                PartyUI.Instance.UpdatePartyFrame(_myParty);
            }
        }
    }

    // --- NOVO: Lógica para sair do grupo ---
    public void LeaveParty()
    {
        if (_myParty == null) return;

        // Se eu sou o líder, o grupo é desfeito.
        if (_myParty.Leader.IsLocal)
        {
            photonView.RPC("RPC_DisbandParty", RpcTarget.All);
        }
        // Se eu sou um membro, aviso o líder que estou saindo.
        else
        {
            photonView.RPC("RPC_RequestLeaveParty", _myParty.Leader, PhotonNetwork.LocalPlayer);
        }
    }

    [PunRPC]
    private void RPC_RequestLeaveParty(Player playerWhoLeft)
    {
        // Apenas o líder executa isso
        if (!_parties.ContainsKey(PhotonNetwork.LocalPlayer)) return;

        Party party = _parties[PhotonNetwork.LocalPlayer];
        party.RemoveMember(playerWhoLeft);

        // Sincroniza o grupo atualizado para os membros restantes
        SyncPartyStateForAllMembers(party);
    }

    public Party FindPartyOfPlayer(Player player)
    {
        foreach (Party party in _parties.Values)
        {
            if (party.IsMember(player))
            {
                return party;
            }
        }
        return null;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (_parties.ContainsKey(otherPlayer))
        {
            Party party = _parties[otherPlayer];
            _parties.Remove(otherPlayer);

            foreach (Player member in party.Members)
            {
                if (member != otherPlayer)
                {
                    photonView.RPC("RPC_DisbandParty", member);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_DisbandParty()
    {
        _myParty = null;
        if (PartyUI.Instance != null) PartyUI.Instance.UpdatePartyFrame(null); // Limpa a UI
        Debug.Log("Seu grupo foi desfeito.");
    }

    // --- NOVO: Função pública para checar se alguém está no meu grupo ---
    public bool IsPlayerInMyParty(Player player)
    {
        if (_myParty == null) return false;
        return _myParty.IsMember(player);
    }
}
