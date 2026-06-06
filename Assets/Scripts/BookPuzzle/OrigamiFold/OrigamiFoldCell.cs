using System.Collections;
using UnityEngine;

public class OrigamiFoldCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Vector2Int initialGridPosition;

    private void Awake()
    {
        if (initialGridPosition == Vector2Int.zero && gridPosition != Vector2Int.zero)
        {
            initialGridPosition = gridPosition;
        }
    }

    public void SetGridPosition(Vector2Int newGridPosition)
    {
        gridPosition = newGridPosition;
    }

    public IEnumerator MoveToLocalPosition(Vector3 targetLocalPosition, float duration)
    {
        Vector3 startLocalPosition = transform.localPosition;

        if (duration <= 0f)
        {
            transform.localPosition = targetLocalPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
            yield return null;
        }

        transform.localPosition = targetLocalPosition;
    }
}
