using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class BossMeleeHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 35;
    [SerializeField] private AudioSource hitSoundSource; // Arraste o AudioSource AQUI no Inspector

    private List<Collider> _collidersHit;
    private Collider _hitboxCollider;
    private bool _colliderFound = false; // Flag para saber se o Awake funcionou

    void Awake()
    {
        _collidersHit = new List<Collider>();
        _hitboxCollider = GetComponent<Collider>();

        if (_hitboxCollider == null)
        {
            Debug.LogError($"[BossMeleeHitbox] CRÍTICO: Nenhum componente 'Collider' foi encontrado no MESMO objeto '{gameObject.name}'! Verifique a configuração do prefab.", this);
            _colliderFound = false;
        }
        else
        {
            Debug.Log($"[BossMeleeHitbox] Collider encontrado com sucesso no objeto '{gameObject.name}'. Tipo: {_hitboxCollider.GetType()}", this);
            _hitboxCollider.isTrigger = true; // Garante que é um trigger
            _hitboxCollider.enabled = false; // Garante que começa desabilitado
            _colliderFound = true;
        }

        // Verifica o AudioSource também, por segurança
        if (hitSoundSource == null)
        {
             // Tenta pegar um AudioSource no mesmo objeto se não foi arrastado
             hitSoundSource = GetComponent<AudioSource>();
             if(hitSoundSource != null)
             {
                 Debug.LogWarning($"[BossMeleeHitbox] AudioSource para 'hitSoundSource' não foi arrastado no Inspector do objeto '{gameObject.name}', mas um foi encontrado no mesmo objeto e será usado.", this);
             }
             // Se ainda assim for nulo, apenas avisa. Não é crítico como o collider.
             else
             {
                 Debug.LogWarning($"[BossMeleeHitbox] Nenhuma referência de 'hitSoundSource' (AudioSource) configurada no Inspector do objeto '{gameObject.name}'. Som de impacto não tocará.", this);
             }
        }
    }

    public void EnableHitbox()
    {
        // Se o collider não foi encontrado no Awake, não faz nada e avisa
        if (!_colliderFound) {
             Debug.LogError($"[BossMeleeHitbox] Tentativa de ATIVAR hitbox falhou porque o Collider não foi encontrado no Awake (Objeto: '{gameObject.name}').", this);
             return;
        }

        _collidersHit.Clear();
        _hitboxCollider.enabled = true;
        Debug.Log($"[BossMeleeHitbox] Hitbox ATIVADA no objeto '{gameObject.name}'.");
    }

    public void DisableHitbox()
    {
        // Se o collider não foi encontrado no Awake, não faz nada
        if (!_colliderFound) {
            Debug.LogError($"[BossMeleeHitbox] Tentativa de DESATIVAR hitbox falhou porque o Collider não foi encontrado no Awake (Objeto: '{gameObject.name}').", this);
            return;
        }

        _hitboxCollider.enabled = false;
        Debug.Log($"[BossMeleeHitbox] Hitbox DESATIVADA no objeto '{gameObject.name}'.");
    }

    private void OnTriggerEnter(Collider other)
    {
         // Se o collider não foi encontrado, não processa colisões
        if (!_colliderFound) return;

        // Adiciona um log para CADA colisão detectada ANTES de qualquer filtro
        Debug.Log($"[BossMeleeHitbox] OnTriggerEnter com '{other.gameObject.name}' (Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        if (!PhotonNetwork.IsMasterClient) return;
        if (!other.CompareTag("Player")) return;
        if (_collidersHit.Contains(other)) return;

        Health healthComponent = other.GetComponent<Health>();
        if (healthComponent != null)
        {
            Debug.Log($"[BossMeleeHitbox] CAUSANDO {damage} de dano a '{other.gameObject.name}'!");
            _collidersHit.Add(other);
            healthComponent.TakeDamage(damage); // Passa o Photon.Realtime.Player do boss? Talvez não precise aqui.
            if (hitSoundSource != null) hitSoundSource.Play();
        }
         else
        {
             Debug.LogWarning($"[BossMeleeHitbox] Colidiu com '{other.gameObject.name}' que tem a tag Player, mas não tem componente Health.", other.gameObject);
        }
    }
}