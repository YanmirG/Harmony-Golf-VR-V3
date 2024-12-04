using System.Collections;
using UnityEngine;

public abstract class RespawnableItem : MonoBehaviour
{
    protected float distanceAhead = 1.0f;
    protected float offsetRight = 0.0f;
    protected float offsetUp = 5.0f;
    protected float respawnDelay = 1.0f;
    protected Vector3 respawnPosition = Vector3.zero;
    protected Vector3 respawnRotation;

    public void Respawn(Transform target)
    {
        respawnPosition = target.position + target.forward * distanceAhead + target.right * offsetRight;
        respawnPosition.y += offsetUp;

        Utils.StartRespawnWithDelay(this, respawnPosition, respawnRotation, respawnDelay);
    }

    public void Respawn()
    {
        if (respawnPosition == Vector3.zero)
        {
            Debug.LogError("Respawn position not set. Make sure to assign to call Respawn(Transform target) first.");
            return;
        }

        Utils.StartRespawnWithDelay(this, respawnPosition, respawnRotation, respawnDelay);
    }
}
