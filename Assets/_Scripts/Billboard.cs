using UnityEngine;

public class Billboard : MonoBehaviour
{
    // LateUpdate é a melhor opção para lógica de câmera.
    void LateUpdate()
    {
        // Verifica se o GameManager já tem a referência da câmera do jogador.
        if (GameManager.LocalPlayerCamera != null)
        {
            // Copia a rotação da câmera do jogador. Simples e direto.
            transform.rotation = GameManager.LocalPlayerCamera.transform.rotation;
        }
    }
}