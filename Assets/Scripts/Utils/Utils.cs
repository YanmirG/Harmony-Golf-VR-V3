using System.Collections;
using UnityEngine;

public static class Utils
{
    public static void StopObject(GameObject obj)
    {
        var rb = obj.GetComponent<Rigidbody>();

        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public static void StartRespawnWithDelay(MonoBehaviour m, Vector3 position, Vector3 rotation, float delay)
    {
        m.StartCoroutine(RespawnWithDelay(m.gameObject, position, Quaternion.Euler(rotation), delay));
    }

    private static IEnumerator RespawnWithDelay(GameObject obj, Vector3 position, Quaternion rotation, float delay)
    {
        Debug.Log("Respawning " + obj.name + " in " + delay + " seconds.");

        Renderer renderer = obj.GetComponent<Renderer>();
        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (renderer != null) renderer.enabled = false;
        if (rb != null) { rb.useGravity = false; rb.isKinematic = true; }
        obj.transform.SetPositionAndRotation(position, rotation);
        Utils.StopObject(obj);

        yield return new WaitForSeconds(delay);

        obj.transform.SetPositionAndRotation(position, rotation);
        Utils.StopObject(obj);
        obj.transform.SetPositionAndRotation(position, rotation);
        if (renderer != null) renderer.enabled = true;
        if (rb != null) { rb.useGravity = true; rb.isKinematic = false; }

        Debug.Log("Respawned " + obj.name + ".");
    }
}
