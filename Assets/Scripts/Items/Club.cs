using UnityEngine;

public class Club : RespawnableItem
{
    private void Awake()
    {
        offsetRight = 0.5f;
        respawnRotation = new Vector3(-90, 0, 0);
    }
}
