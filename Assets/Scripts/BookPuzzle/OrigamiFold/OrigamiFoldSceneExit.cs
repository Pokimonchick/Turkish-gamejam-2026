using UnityEngine;

public class OrigamiFoldSceneExit : MonoBehaviour
{
    public string nextSceneName = "Village_Level_02_Stub";
    public bool loadSceneOnEnter = true;
    public GameObject visualRoot;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        Debug.Log("Loading next scene: " + nextSceneName, this);

        if (!loadSceneOnEnter)
        {
            return;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning($"{name}: nextSceneName is empty.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogWarning($"{name}: scene is not in build settings: {nextSceneName}", this);
            return;
        }

        try
        {
            SceneTransition.Load(nextSceneName);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"{name}: could not load scene {nextSceneName}. {exception.Message}", this);
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        try
        {
            if (other.CompareTag("Player"))
            {
                return true;
            }
        }
        catch (UnityException)
        {
        }

        return other.GetComponentInParent<OrigamiFoldPlayerMover>() != null
            || other.GetComponentInParent<OrigamiFoldPassenger>() != null;
    }
}
