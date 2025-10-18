using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // O alvo que a câmera deve seguir

    [Header("Stats da Câmera")]
    [SerializeField] private float mouseSensitivity = 350.0f;
    [SerializeField] private float distanceFromTarget = 5.0f;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.0f, 0); // Para a câmera olhar para o peito, não para os pés
    [SerializeField] private float cameraYClamp = 80.0f;

    private float _xRotation = 20f; // Começa olhando um pouco para baixo
    private float _yRotation = 0f;

    void LateUpdate()
    {
        if (target == null) { return; }

        if (UIManager.Instance != null && UIManager.Instance.IsMenuOpen())
        {
            // Garante que o cursor fique visível se o menu for aberto
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Input.GetMouseButton(1)) // Botão direito
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            _yRotation += mouseX;
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -cameraYClamp, cameraYClamp);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Quaternion rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
        Vector3 targetPosition = target.position + offset;
        transform.position = targetPosition - (rotation * Vector3.forward * distanceFromTarget);
        transform.LookAt(targetPosition);
    }
}