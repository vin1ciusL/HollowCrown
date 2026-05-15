using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    void LateUpdate()
    {
        // Se o target foi desativado, limpa a referência
        if (target != null && !target.gameObject.activeInHierarchy)
            target = null;

        if (target == null)
        {
            GameObject hero = GameObject.FindWithTag("Player");
            if (hero != null) target = hero.transform;
            return;
        }

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}