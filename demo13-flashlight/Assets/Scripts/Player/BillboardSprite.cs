using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam == null) return;
        transform.rotation = mainCam.transform.rotation;
    }
}
