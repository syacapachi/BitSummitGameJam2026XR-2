using UnityEngine;

public static class NetFxSpawnUtility
{
    public static void Spawn(
        GameObject fxPrefab,
        AudioClip sfx,
        Vector3 position,
        Quaternion rotation,
        float destroyAfterSeconds = 2f,
        float volume = 1f)
    {
        if (fxPrefab != null)
        {
            GameObject fx = Object.Instantiate(fxPrefab, position, rotation);
            Object.Destroy(fx, destroyAfterSeconds);
        }

        if (sfx != null)
        {
            AudioSource.PlayClipAtPoint(sfx, position, volume);
        }
    }
}