using UnityEngine;

public class ShipController : MonoBehaviour
{
    public float moveSpeed = 5f;

    float screenHalfX;
    float screenHalfY;

    void Start()
    {
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        screenHalfX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, camZ)).x;
        screenHalfY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, camZ)).y;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, v, 0f) * moveSpeed * Time.deltaTime;
        transform.position += move;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -screenHalfX, screenHalfX);
        pos.y = Mathf.Clamp(pos.y, -screenHalfY, screenHalfY);
        transform.position = pos;
    }
}
