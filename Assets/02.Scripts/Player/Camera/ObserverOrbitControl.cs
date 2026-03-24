using UnityEngine;

public class ObserverOrbitControl : MonoBehaviour
{
    public Transform target;
    public float distance = 2.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public float yMinLimit = 20f;
    public float yMaxLimit = 80f;

    public LayerMask collisionLayers;
    public float cameraRadius = 0.2f;
    public float collisionPadding = 0.1f;

    private float x = 0.0f;
    private float y = 0.0f;


    void OnEnable()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    void LateUpdate()
    {
        if (!target) return;

        x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
        y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        Quaternion rotation = Quaternion.Euler(y, x, 0);

        Vector3 desiredPosition = target.position + (rotation * new Vector3(0.0f, 0.0f, -distance));
        Vector3 directionFromTarget = desiredPosition - target.position;

        float actualDistance = distance;
        RaycastHit hit;

        if (Physics.SphereCast(target.position, cameraRadius, directionFromTarget.normalized, out hit, distance, collisionLayers))
        {
            actualDistance = hit.distance - collisionPadding;
        }

        Vector3 finalPosition = target.position + (directionFromTarget.normalized * actualDistance);

        transform.position = finalPosition;
        transform.rotation = rotation;
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}