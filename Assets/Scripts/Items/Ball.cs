using UnityEngine;

public class Ball : RespawnableItem
{
    private void Awake()
    {
        offsetRight = 0.0f;
        respawnRotation = new Vector3(0, 0, 0);
    }
}
