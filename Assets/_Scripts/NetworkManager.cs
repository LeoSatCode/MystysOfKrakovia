// Assinatura: NetworkManager.cs
using UnityEngine;
using UnityEngine.SceneManagement; // Precisamos disso para a lógica de cena
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

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

    void Start()
    {
        // Começa a conectar assim que o jogo abre
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado ao Master! Pronto para entrar em uma sala.");
        // Agora nós esperamos o comando do MainMenu para entrar na sala
        PhotonNetwork.JoinLobby(); // É uma boa prática entrar no lobby principal
    }

    public void JoinOrCreateRoom()
    {
        // Tentamos entrar na sala "Overworld". Se não existir, ela será criada.
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 20 };
        PhotonNetwork.JoinOrCreateRoom("Overworld_Room_1", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Entramos na sala Overworld! Carregando cena...");
        // Carrega a cena do mapa principal para todos na sala
        PhotonNetwork.LoadLevel("Overworld");
    }
}