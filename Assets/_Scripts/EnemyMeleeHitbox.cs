using UnityEngine;
using System.Collections.Generic;
using Photon.Pun; // Importante adicionar

public class EnemyMeleeHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    private List<Collider> _collidersHit; 
    private Collider _hitboxCollider;

    void Awake()
    {
        _collidersHit = new List<Collider>();
        _hitboxCollider = GetComponent<Collider>();
    }

    public void EnableHitbox()
    {
        _collidersHit.Clear();
        _hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        _hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // --- CORREÇÃO DE AUTORIDADE ---
        // Apenas o Master Client (dono do inimigo) pode registrar o dano.
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        // --- FIM DA CORREÇÃO ---

        if (!other.CompareTag("Player")) return;
        if (_collidersHit.Contains(other)) return;

        Health healthComponent = other.GetComponent<Health>();
        if (healthComponent != null)
        {
            _collidersHit.Add(other);
            healthComponent.TakeDamage(damage);
        }
    }
}