using UnityEngine;

public class SetPosition : MonoBehaviour
{

    public float verticalOffset = 10;
    public GameObject targetObject;

    void Update()
    {
        Vector3 newPosition = targetObject.transform.position;
        newPosition.y += verticalOffset;
        gameObject.transform.position = newPosition;


        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            targetObject.transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );
    }
}
