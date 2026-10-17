using Unity.VisualScripting;
using UnityEngine;

public class TrackPlayerLocation : MonoBehaviour
{

    public GameObject playerCenterEye;

    public float smoothing = 0.5f;
    Vector3 oldPlayerLocation;
    Vector3 currentPlayerLocation;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(playerCenterEye != null){
        oldPlayerLocation = playerCenterEye.transform.position;
        currentPlayerLocation = playerCenterEye.transform.position;
        }

    }

    // Update is called once per frame
    void Update()
    {
        oldPlayerLocation = currentPlayerLocation;
        currentPlayerLocation = playerCenterEye.transform.position;

        transform.position = Vector3.Lerp(oldPlayerLocation, currentPlayerLocation, smoothing);
    }
}
