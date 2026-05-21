using UnityEngine;

public class OrbitAround : MonoBehaviour
{
    public Transform centerObject;
    [SerializeField] private float speed = 30f;
    [SerializeField] private float radius = 4f;

    private float currentAngle;

    private void Start()
    {
        if (centerObject != null)
        {
            Vector3 offset = transform.position - centerObject.position;
            radius = new Vector2(offset.x, offset.z).magnitude;
            currentAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }
    }

    private void Update()
    {
        if (centerObject == null) return;

        currentAngle += speed * Time.deltaTime;
        float rad = currentAngle * Mathf.Deg2Rad;
        float x = centerObject.position.x + Mathf.Sin(rad) * radius;
        float z = centerObject.position.z + Mathf.Cos(rad) * radius;
        transform.position = new Vector3(x, transform.position.y, z);
    }
}
