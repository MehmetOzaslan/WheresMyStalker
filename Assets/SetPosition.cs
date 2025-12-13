using UnityEngine;

public class SetPosition : MonoBehaviour
{

    public GameObject targetObject;

    void Update()
    {
        gameObject.transform.position = targetObject.transform.position;
        
        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            targetObject.transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );
    }
}
