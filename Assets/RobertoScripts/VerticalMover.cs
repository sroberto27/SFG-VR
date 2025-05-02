using UnityEngine;

public class VerticalMover : MonoBehaviour
{
    public float step = 0.05f;

    void Update()
    {
        Vector3 position = transform.position;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            position.y += step;
            transform.position = position;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            position.y -= step;
            transform.position = position;
        }
    }
}
