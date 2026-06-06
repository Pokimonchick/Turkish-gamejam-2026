using System.Collections;
using UnityEngine;

[System.Serializable]
public class OrigamiCellMove
{
    public OrigamiFoldCell cell;
    public Vector2Int targetGridPosition;
}

public class OrigamiFoldBoard : MonoBehaviour
{
    public float cellSize = 1f;
    public Vector2 originLocalPosition = Vector2.zero;
    public float foldAnimationDuration = 0.25f;

    public bool IsAnimating { get; private set; }

    public Vector3 GridToLocalPosition(Vector2Int gridPosition)
    {
        Vector2 localPosition = originLocalPosition + new Vector2(
            gridPosition.x * cellSize,
            gridPosition.y * cellSize);

        return new Vector3(localPosition.x, localPosition.y, 0f);
    }

    public void SnapCellToGrid(OrigamiFoldCell cell)
    {
        if (cell == null)
        {
            return;
        }

        cell.transform.localPosition = GridToLocalPosition(cell.gridPosition);
    }

    public Coroutine AnimateMoves(OrigamiCellMove[] moves)
    {
        if (IsAnimating)
        {
            return null;
        }

        return StartCoroutine(AnimateMovesRoutine(moves));
    }

    public IEnumerator AnimateMovesRoutine(OrigamiCellMove[] moves)
    {
        IsAnimating = true;

        if (moves == null || moves.Length == 0)
        {
            IsAnimating = false;
            yield break;
        }

        Coroutine[] runningMoves = new Coroutine[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            OrigamiCellMove move = moves[i];

            if (move == null || move.cell == null)
            {
                continue;
            }

            Vector3 targetLocalPosition = GridToLocalPosition(move.targetGridPosition);
            runningMoves[i] = StartCoroutine(move.cell.MoveToLocalPosition(
                targetLocalPosition,
                foldAnimationDuration));
        }

        for (int i = 0; i < runningMoves.Length; i++)
        {
            if (runningMoves[i] != null)
            {
                yield return runningMoves[i];
            }
        }

        for (int i = 0; i < moves.Length; i++)
        {
            OrigamiCellMove move = moves[i];

            if (move == null || move.cell == null)
            {
                continue;
            }

            move.cell.SetGridPosition(move.targetGridPosition);
            move.cell.transform.localPosition = GridToLocalPosition(move.targetGridPosition);
        }

        IsAnimating = false;
    }
}
