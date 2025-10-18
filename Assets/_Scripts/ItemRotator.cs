using UnityEngine;

public class ItemRotator : MonoBehaviour
{
    [Header("Girar")]
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Flutuar")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.25f;

    private Vector3 startPosition;

    void Start()
    {
        // Guarda a posição inicial do objeto
        startPosition = transform.position;
    }

    void Update()
    {
        // --- LÓGICA DE GIRAR ---
        // Gira o objeto em torno do seu próprio eixo Y
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // --- LÓGICA DE FLUTUAR (SOBE E DESCE) ---
        // Usa uma função de seno para criar um movimento suave de "sobe e desce"
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}