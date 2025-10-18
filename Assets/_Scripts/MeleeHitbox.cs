using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

[RequireComponent(typeof(AudioSource))] // Garante que sempre haverá um AudioSource
public class MeleeHitbox : MonoBehaviour
{
    [Header("Configuração de Dano")]
    [SerializeField] private int baseDamage = 15;

    [Header("Feedback Sonoro")]
    [SerializeField] private AudioClip swordSwingSound; // Som da espada do Guerreiro
    [SerializeField] private AudioClip maceSwingSound;   // Som da maça do Clérigo

    private PlayerController owner;
    private List<Collider> _collidersHit;
    private Collider _hitboxCollider;
    private AudioSource audioSource; // Referência para o som

    void Awake()
    {
        _collidersHit = new List<Collider>();
        _hitboxCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>(); // Pega a referência do AudioSource
    }

    void Start()
    {
        owner = GetComponentInParent<PlayerController>();
    }

    public void EnableHitbox()
    {
        _collidersHit.Clear();
        _hitboxCollider.enabled = true;

        // --- LÓGICA DO SOM DE ATAQUE ---
        if (owner != null && owner.GetComponent<PhotonView>().IsMine)
        {
            // Pega a classe do personagem atual
            string playerClass = GameManager.SelectedCharacter.characterClass;
            AudioClip clipToPlay = null;

            // Decide qual som tocar
            if (playerClass == "Guerreiro")
            {
                clipToPlay = swordSwingSound;
            }
            else if (playerClass == "Clérigo")
            {
                clipToPlay = maceSwingSound;
            }

            // Toca o som escolhido, se houver um
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }
        // --- FIM DA LÓGICA DO SOM ---
    }

    public void DisableHitbox()
    {
        _hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null || !owner.GetComponent<PhotonView>().IsMine)
        {
            return;
        }

        if (_collidersHit.Contains(other)) { return; }

        Health healthComponent = other.GetComponent<Health>();
        if (healthComponent != null)
        {
            _collidersHit.Add(other);
            int totalDamage = baseDamage + owner.strength;
            healthComponent.TakeDamage(totalDamage);
        }
    }
}