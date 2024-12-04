using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Player : MonoBehaviour
{
    public GameObject SpawnPosition; // Set this to level 1 spawn position in the editor, rest are handled automatically
    public uint CurrentLevel = 1;

    public static Player Instance { get; private set; }
    public GameObject Club { get; private set; }
    public GameObject Ball { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (SpawnPosition == null)
            Debug.LogError("Spawn position not set. Make sure to assign a GameObject to the Spawn Position field in the Player script.");

        Club = GameObject.FindGameObjectWithTag("Club");
        if (Club == null)
            Debug.LogError("Club not found in the scene. Make sure to tag the club GameObject with the 'Club' tag.");

        Ball = GameObject.FindGameObjectWithTag("Ball");
        if (Ball == null)
            Debug.LogError("Ball not found in the scene. Make sure to tag the ball GameObject with the 'Ball' tag.");

        RespawnEverything();
    }

    public void RespawnEverything()
    {
        Respawn();
        RespawnBall();
        RespawnClub();
    }

    public void Respawn()
    {
        var target = SpawnPosition.transform;

        Utils.StopObject(this.gameObject);
        this.transform.SetPositionAndRotation(target.position, target.rotation);
    }

    public void RespawnBall()
    {
        Ball ballScript = Ball.GetComponent<Ball>();
        if (ballScript == null)
        {
            Debug.LogError("Ball script not found. Make sure to attach the Ball script to the ball GameObject.");
            return;
        }

        ballScript.Respawn(SpawnPosition.transform);
    }

    public void RespawnClub()
    {
        Club clubScript = Club.GetComponent<Club>();
        if (clubScript == null)
        {
            Debug.LogError("Club script not found. Make sure to attach the Club script to the club GameObject.");
            return;
        }

        clubScript.Respawn(SpawnPosition.transform);
    }
}