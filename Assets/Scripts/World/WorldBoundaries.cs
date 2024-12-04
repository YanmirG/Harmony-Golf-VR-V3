using UnityEngine;

public class WorldBoundaries : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("World boundaries triggered.");

        if (other.CompareTag("Ball"))
        {
            Player.Instance.RespawnBall();
        }

        if (other.CompareTag("Club"))
        {
            Player.Instance.RespawnClub();
        }

        if (other.CompareTag("Player"))
        {
            Player.Instance.RespawnEverything();
        }
    }
}
