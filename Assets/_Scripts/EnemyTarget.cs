    using UnityEngine;
    using Photon.Pun;

    public class EnemyTarget : MonoBehaviour, IPunObservable
    {
        [SerializeField] private GameObject selector;
        [SerializeField] private int maxHealth = 100;
        
        private int _currentHealth;
        private PhotonView _photonView;

        void Awake()
        {
            _photonView = GetComponent<PhotonView>();
        }

        void Start()
        {
            _currentHealth = maxHealth;
        }

        // Esta função foi removida, pois o projétil agora chama o RPC diretamente.

        [PunRPC]
        public void RPC_TakeDamage(int damage)
        {
            // APENAS o Master Client tem autoridade para mudar a vida
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            _currentHealth -= damage;
            Debug.Log(gameObject.name + " tomou " + damage + " de dano. Vida restante: " + _currentHealth);

            if (_currentHealth <= 0)
            {
                Debug.Log(gameObject.name + " foi derrotado!");
                
                // CORREÇÃO: Usamos PhotonNetwork.Destroy para remover o objeto para TODOS os jogadores.
                // Esta chamada só será executada pelo Master Client, mas o efeito é para todos.
                PhotonNetwork.Destroy(gameObject);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // O Master Client é o "dono" da vida do inimigo e a envia para os outros.
                if (PhotonNetwork.IsMasterClient)
                {
                    stream.SendNext(_currentHealth);
                }
            }
            else
            {
                // Os outros clientes recebem a informação de vida e atualizam-na.
                _currentHealth = (int)stream.ReceiveNext();
            }
        }

        public void Select() { selector.SetActive(true); }
        public void Deselect() { selector.SetActive(false); }
    }