using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class DamageZone : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float tickSpeed = 1f; // Dano a cada 1 segundo
    [SerializeField] private float duration = 8f;

    private List<Health> playersInZone = new List<Health>();

    void Start()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DestroyAfterDuration());
            InvokeRepeating("DealTickDamage", 0, tickSpeed);
        }
    }

    void DealTickDamage()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (Health playerHealth in playersInZone)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerTick);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null && !playersInZone.Contains(playerHealth))
            {
                playersInZone.Add(playerHealth);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null && playersInZone.Contains(playerHealth))
            {
                playersInZone.Remove(playerHealth);
            }
        }
    }

    private IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        PhotonNetwork.Destroy(gameObject);
    }
}