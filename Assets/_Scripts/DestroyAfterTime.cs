using UnityEngine;
using Photon.Pun;

public class DestroyAfterTime : MonoBehaviour
{
    public float lifetime = 2f;

    void Start()
    {
        // Apenas o dono do objeto (quem o instanciou) deve agendar a destruição.
        if (GetComponent<PhotonView>() == null || GetComponent<PhotonView>().IsMine)
        {
            StartCoroutine(DestroyRoutine());
        }
    }

    private System.Collections.IEnumerator DestroyRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        PhotonNetwork.Destroy(gameObject);
    }
}