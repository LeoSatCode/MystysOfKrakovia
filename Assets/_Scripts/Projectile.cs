using UnityEngine;
using Photon.Pun;

public enum StatScalingType
{
    None,
    Strength,
    Intelligence,
    Dexterity
}

public class Projectile : MonoBehaviour
{
    [Header("Configurações Gerais")]
    [SerializeField] private float speed = 20f;

    [Header("Dano")]
    [SerializeField] private int baseDamage = 25;
    [SerializeField] private StatScalingType damageScaling = StatScalingType.None;

    [Header("Feedback Sonoro")]
    public AudioClip castSound; // Renomeado de attackSound para mais clareza
    public AudioClip hitSound;  // Som ao atingir algo

    private AudioSource audioSource; // Usado apenas para o som de lançamento

    private PhotonView _photonView;
    private PlayerController _owner;

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        // Pega o AudioSource se ele existir no prefab
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Destrói o projétil após 5 segundos se não atingir nada
        // Precisamos chamar a lógica de som aqui também
        StartCoroutine(DestroyAfterTime(5.0f));

        // Toca o som de lançamento
        if(audioSource != null && castSound != null)
        {
            audioSource.PlayOneShot(castSound);
        }
    }

    // Nova Coroutine para garantir que o som toque antes de destruir
    private System.Collections.IEnumerator DestroyAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Se o objeto ainda existir (não foi destruído por uma colisão)
        if (gameObject != null)
        {
            // Toca o som de "fizzle" ou desaparecimento no local
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }

            // Destrói na rede somente se for o dono
            if (_photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    void Update()
    {
        if (_photonView.IsMine)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    public void Initialize(PlayerController owner)
    {
        _owner = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_photonView.IsMine) { return; }
        if (other.CompareTag("Projectile")) { return; }

        // --- LÓGICA DO SOM DE IMPACTO (A GRANDE MUDANÇA) ---
        // Toca o som de impacto na posição atual do projétil.
        // Esta função cria um objeto temporário que se autodestrói.
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
        // A antiga chamada 'audioSource.PlayOneShot(hitSound)' foi removida.
        // --- FIM DA LÓGICA DO SOM ---

        // Lógica de dano continua a mesma
        Health healthComponent = other.GetComponent<Health>();
        if (healthComponent != null)
        {
            int totalDamage = baseDamage;
            if (_owner != null)
            {
                switch (damageScaling)
                {
                    case StatScalingType.Strength: totalDamage += _owner.strength; break;
                    case StatScalingType.Intelligence: totalDamage += _owner.intelligence; break;
                    case StatScalingType.Dexterity: totalDamage += _owner.dexterity; break;
                }
            }
            healthComponent.TakeDamage(totalDamage);
        }

        // Destrói o projétil para todos na rede
        PhotonNetwork.Destroy(gameObject);
    }
}