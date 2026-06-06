using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldWorkbenchBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Prototype_BookPuzzle.unity";
    private const string WorkbenchScenePath = "Assets/Scenes/Prototype_OrigamiFold_Workbench.unity";

    private enum WorkbenchStep
    {
        Step1,
        Step2,
        Step3,
        Step3_1,
        Step3_2,
        Step3_3,
        Step3_4,
        Step3_5,
        Step3_6
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 1")]
    public static void RebuildWorkbenchStep1()
    {
        RebuildWorkbench(WorkbenchStep.Step1);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 2")]
    public static void RebuildWorkbenchStep2()
    {
        RebuildWorkbench(WorkbenchStep.Step2);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3")]
    public static void RebuildWorkbenchStep3()
    {
        RebuildWorkbench(WorkbenchStep.Step3);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.1 Grid Aligned")]
    public static void RebuildWorkbenchStep3_1()
    {
        RebuildWorkbench(WorkbenchStep.Step3_1);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.2 Horizontal Controls")]
    public static void RebuildWorkbenchStep3_2()
    {
        RebuildWorkbench(WorkbenchStep.Step3_2);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.3 Vertical Controls")]
    public static void RebuildWorkbenchStep3_3()
    {
        RebuildWorkbench(WorkbenchStep.Step3_3);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.4 Symmetric Squeeze")]
    public static void RebuildWorkbenchStep3_4()
    {
        RebuildWorkbench(WorkbenchStep.Step3_4);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.5 Unified Map")]
    public static void RebuildWorkbenchStep3_5()
    {
        RebuildWorkbench(WorkbenchStep.Step3_5);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.6 Whole Strip Fold")]
    public static void RebuildWorkbenchStep3_6()
    {
        RebuildWorkbench(WorkbenchStep.Step3_6);
    }

    private static void RebuildWorkbench(WorkbenchStep step)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Origami fold workbench rebuild is disabled while Unity is in Play Mode.");
            return;
        }

        if (!File.Exists(SourceScenePath))
        {
            Debug.LogWarning($"Source scene not found: {SourceScenePath}");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);

        if (!EditorSceneManager.SaveScene(sourceScene, WorkbenchScenePath, true))
        {
            Debug.LogWarning($"Could not save workbench scene copy: {WorkbenchScenePath}");
            return;
        }

        Scene workbenchScene = EditorSceneManager.OpenScene(WorkbenchScenePath, OpenSceneMode.Single);

        SetLegacyContainersActive(false);
        RebuildOrigamiObjects(step);
        EditorSceneManager.SaveScene(workbenchScene);
        AssetDatabase.Refresh();

        Debug.Log($"Rebuilt origami fold workbench {step}: {WorkbenchScenePath}");
    }

    private static void RebuildOrigamiObjects(WorkbenchStep step)
    {
        DeleteIfExists("ORIGAMI_FOLD_SYSTEM");
        DeleteIfExists("ORIGAMI_FOLD_POINTS");
        DeleteIfExists("ORIGAMI_FOLD_LINKS");
        DeleteIfExists("ORIGAMI_DEBUG");
        DeleteIfExists("ORIGAMI_WORKBENCH_VISUALS");
        DeleteIfExists("ORIGAMI_GRID");
        DeleteIfExists("ORIGAMI_GRID_GUIDES");
        DeleteIfExists("ORIGAMI_ACTIONS");
        DeleteIfExists("ORIGAMI_SQUEEZE_HORIZONTAL");
        DeleteIfExists("ORIGAMI_SQUEEZE_VERTICAL");
        DeleteIfExists("ORIGAMI_UNIFIED_MAP");

        GameObject systemRoot = new GameObject("ORIGAMI_FOLD_SYSTEM");
        GameObject pointsRoot = new GameObject("ORIGAMI_FOLD_POINTS");
        GameObject linksRoot = new GameObject("ORIGAMI_FOLD_LINKS");
        GameObject debugRoot = new GameObject("ORIGAMI_DEBUG");
        GameObject visualsRoot = new GameObject("ORIGAMI_WORKBENCH_VISUALS");

        Camera camera = FindMainCameraOrCreate(step);
        CreateWorkbenchVisuals(visualsRoot.transform, step);

        GameObject executeIndicator = CreateExecuteIndicator(debugRoot.transform);
        CreateInstructionText(debugRoot.transform, step);

        if (step == WorkbenchStep.Step3_6)
        {
            RebuildWholeStripOrigamiObjects(
                systemRoot,
                pointsRoot,
                linksRoot,
                camera,
                executeIndicator);

            return;
        }

        if (step == WorkbenchStep.Step3_5)
        {
            RebuildUnifiedMapOrigamiObjects(
                systemRoot,
                pointsRoot,
                linksRoot,
                camera,
                executeIndicator);

            return;
        }

        if (step == WorkbenchStep.Step3_4)
        {
            RebuildSqueezeOrigamiObjects(
                systemRoot,
                pointsRoot,
                linksRoot,
                camera,
                executeIndicator);

            return;
        }

        if (step == WorkbenchStep.Step3_3)
        {
            RebuildVerticalOrigamiObjects(
                systemRoot,
                pointsRoot,
                linksRoot,
                camera,
                executeIndicator);

            return;
        }

        bool hasGrid = step != WorkbenchStep.Step1;
        bool hasMergedPoints = step == WorkbenchStep.Step3
            || step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2;
        bool isGridAligned = step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2;

        Vector2 boardOrigin = isGridAligned ? new Vector2(-1.5f, 0f) : new Vector2(-1.5f, -0.4f);
        float cellSize = 1f;
        float cellVisualSize = isGridAligned ? 0.92f : 0.86f;
        float pointSize = isGridAligned ? 0.22f : step == WorkbenchStep.Step3 ? 0.42f : 0.55f;
        float sourcePointColliderRadius = isGridAligned
            ? pointSize * 0.5f
            : step == WorkbenchStep.Step3 ? 0.28f : 0.35f;
        float mergedPointSize = isGridAligned ? 0.30f : 0.68f;
        float mergedPointColliderRadius = isGridAligned ? mergedPointSize * 0.5f : 0.42f;

        Vector3 topLeftPosition;
        Vector3 topRightPosition;
        Vector3 bottomLeftPosition;
        Vector3 bottomRightPosition;
        Vector3 mergedTopPosition;
        Vector3 mergedBottomPosition;

        if (isGridAligned)
        {
            Vector3 collapsibleCenter = GridToLocalPosition(boardOrigin, cellSize, new Vector2Int(1, 0));
            float leftX = collapsibleCenter.x - cellSize * 0.5f;
            float rightX = collapsibleCenter.x + cellSize * 0.5f;
            float topY = collapsibleCenter.y + cellSize * 0.5f;
            float bottomY = collapsibleCenter.y - cellSize * 0.5f;

            topLeftPosition = new Vector3(leftX, topY, 0f);
            topRightPosition = new Vector3(rightX, topY, 0f);
            bottomLeftPosition = new Vector3(leftX, bottomY, 0f);
            bottomRightPosition = new Vector3(rightX, bottomY, 0f);
            mergedTopPosition = topRightPosition;
            mergedBottomPosition = bottomRightPosition;
        }
        else if (step == WorkbenchStep.Step3)
        {
            topLeftPosition = new Vector3(-1f, 0.38f, 0f);
            topRightPosition = new Vector3(0f, 0.38f, 0f);
            bottomLeftPosition = new Vector3(-1f, -1.18f, 0f);
            bottomRightPosition = new Vector3(0f, -1.18f, 0f);
            mergedTopPosition = new Vector3(-0.5f, 0.38f, 0f);
            mergedBottomPosition = new Vector3(-0.5f, -1.18f, 0f);
        }
        else
        {
            topLeftPosition = new Vector3(-1f, 1f, 0f);
            topRightPosition = new Vector3(1f, 1f, 0f);
            bottomLeftPosition = new Vector3(-1f, -1f, 0f);
            bottomRightPosition = new Vector3(1f, -1f, 0f);
            mergedTopPosition = Vector3.zero;
            mergedBottomPosition = Vector3.zero;
        }

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "OrigamiPoint_TopLeft",
            topLeftPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "OrigamiPoint_TopRight",
            topRightPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "OrigamiPoint_BottomLeft",
            bottomLeftPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "OrigamiPoint_BottomRight",
            bottomRightPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint mergedTop = null;
        OrigamiFoldPoint mergedBottom = null;

        if (hasMergedPoints)
        {
            mergedTop = CreateFoldPoint(
                "OrigamiPoint_MergedTop",
                mergedTopPosition,
                Color.cyan,
                new Vector3(mergedPointSize, mergedPointSize, 1f),
                mergedPointColliderRadius,
                35,
                pointsRoot.transform);

            mergedBottom = CreateFoldPoint(
                "OrigamiPoint_MergedBottom",
                mergedBottomPosition,
                Color.cyan,
                new Vector3(mergedPointSize, mergedPointSize, 1f),
                mergedPointColliderRadius,
                35,
                pointsRoot.transform);
        }

        OrigamiFoldMoveAction compressHorizontalAction = null;

        if (hasGrid)
        {
            compressHorizontalAction = CreateGridAndAction(
                step,
                boardOrigin,
                cellSize,
                cellVisualSize,
                new[] { topLeft, topRight, bottomLeft, bottomRight },
                new[] { mergedTop, mergedBottom });
        }

        if (hasMergedPoints)
        {
            ConfigureMergedClickAction(mergedTop, compressHorizontalAction, camera);
            ConfigureMergedClickAction(mergedBottom, compressHorizontalAction, camera);
            mergedTop.gameObject.SetActive(false);
            mergedBottom.gameObject.SetActive(false);
        }

        if (isGridAligned)
        {
            CreateGridGuides(boardOrigin, cellSize);
        }

        OrigamiFoldLink top = null;
        OrigamiFoldLink bottom = null;
        OrigamiFoldLink left = null;
        OrigamiFoldLink right = null;
        OrigamiFoldLink topUnfold = null;
        OrigamiFoldLink topRightToLeft = null;
        OrigamiFoldLink bottomLeftToRight = null;
        OrigamiFoldLink bottomRightToLeft = null;

        if (step == WorkbenchStep.Step3_2)
        {
            top = CreateFoldLink(
                "OrigamiLink_Top_LTR",
                topLeft,
                topRight,
                executeIndicator,
                linksRoot.transform);

            topRightToLeft = CreateFoldLink(
                "OrigamiLink_Top_RTL",
                topRight,
                topLeft,
                executeIndicator,
                linksRoot.transform);

            bottomLeftToRight = CreateFoldLink(
                "OrigamiLink_Bottom_LTR",
                bottomLeft,
                bottomRight,
                executeIndicator,
                linksRoot.transform);

            bottomRightToLeft = CreateFoldLink(
                "OrigamiLink_Bottom_RTL",
                bottomRight,
                bottomLeft,
                executeIndicator,
                linksRoot.transform);

            ConfigureFoldMoveLink(top, compressHorizontalAction, true);
            ConfigureFoldMoveLink(topRightToLeft, compressHorizontalAction, true);
            ConfigureFoldMoveLink(bottomLeftToRight, compressHorizontalAction, true);
            ConfigureFoldMoveLink(bottomRightToLeft, compressHorizontalAction, true);
        }
        else
        {
            top = CreateFoldLink(
                "OrigamiLink_Top",
                topLeft,
                topRight,
                executeIndicator,
                linksRoot.transform);

            if (hasGrid)
            {
                ConfigureFoldMoveLink(top, compressHorizontalAction, true);
            }
        }

        if (step == WorkbenchStep.Step1 || step == WorkbenchStep.Step2)
        {
            bottom = CreateFoldLink(
                "OrigamiLink_Bottom",
                bottomLeft,
                bottomRight,
                executeIndicator,
                linksRoot.transform);

            left = CreateFoldLink(
                "OrigamiLink_Left",
                topLeft,
                bottomLeft,
                executeIndicator,
                linksRoot.transform);

            right = CreateFoldLink(
                "OrigamiLink_Right",
                topRight,
                bottomRight,
                executeIndicator,
                linksRoot.transform);
        }

        if (step == WorkbenchStep.Step2)
        {
            topUnfold = CreateFoldLink(
                "OrigamiLink_Top_Unfold",
                topRight,
                topLeft,
                executeIndicator,
                linksRoot.transform);

            ConfigureFoldMoveLink(topUnfold, compressHorizontalAction, false);
        }

        OrigamiFoldDragController controller = systemRoot.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = camera;
        controller.snapDistance = 0.45f;
        controller.autoFindLinks = true;

        if (step == WorkbenchStep.Step3_2)
        {
            controller.links = new[]
            {
                top,
                topRightToLeft,
                bottomLeftToRight,
                bottomRightToLeft
            };
        }
        else if (hasMergedPoints)
        {
            controller.links = new[] { top };
        }
        else if (step == WorkbenchStep.Step2)
        {
            controller.links = new[] { top, topUnfold, bottom, left, right };
        }
        else
        {
            controller.links = new[] { top, bottom, left, right };
        }
    }

    private static void RebuildWholeStripOrigamiObjects(
        GameObject systemRoot,
        GameObject pointsRoot,
        GameObject linksRoot,
        Camera camera,
        GameObject executeIndicator)
    {
        GameObject actionsRoot = new GameObject("ORIGAMI_ACTIONS");
        GameObject guidesRoot = new GameObject("ORIGAMI_GRID_GUIDES");
        GameObject mapRoot = new GameObject("ORIGAMI_UNIFIED_MAP");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(mapRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        OrigamiFoldActionCoordinator coordinator = systemRoot.AddComponent<OrigamiFoldActionCoordinator>();
        CreateWholeStripMapCells(
            cellsRoot.transform,
            out OrigamiFoldTransformStack[,] stacks);

        OrigamiFoldLink[] rowLinks = CreateWholeStripRowZone(
            stacks,
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            camera,
            executeIndicator,
            coordinator);

        OrigamiFoldLink[] columnLinks = CreateWholeStripColumnZone(
            stacks,
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            camera,
            executeIndicator,
            coordinator);

        CreateWholeStripGuides(guidesRoot.transform);

        OrigamiFoldDragController controller = systemRoot.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = camera;
        controller.snapDistance = 0.5f;
        controller.autoFindLinks = true;
        controller.links = new[]
        {
            rowLinks[0],
            rowLinks[1],
            rowLinks[2],
            rowLinks[3],
            columnLinks[0],
            columnLinks[1],
            columnLinks[2],
            columnLinks[3]
        };

    }

    private static GameObject[,] CreateWholeStripMapCells(
        Transform parent,
        out OrigamiFoldTransformStack[,] stacks)
    {
        GameObject[,] cells = new GameObject[5, 4];
        stacks = new OrigamiFoldTransformStack[5, 4];
        float cellVisualSize = 0.92f;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Color color = new Color(0.18f, 0.24f, 0.30f);

                if (x == 3 && y == 1)
                {
                    color = new Color(0.95f, 0.76f, 0.26f);
                }
                else if (y == 1)
                {
                    color = new Color(0.84f, 0.56f, 0.20f);
                }
                else if (x == 3)
                {
                    color = new Color(0.44f, 0.32f, 0.68f);
                }

                Vector3 position = new Vector3((x - 2) * 1f, (y - 1.5f) * 1f, 0f);
                GameObject cell = CreateSqueezeCell(
                    $"MapCell_{x}_{y}",
                    position,
                    color,
                    cellVisualSize,
                    parent,
                    $"{x},{y}");

                OrigamiFoldTransformStack stack = cell.AddComponent<OrigamiFoldTransformStack>();
                stack.CaptureBaseTransform();

                cells[x, y] = cell;
                stacks[x, y] = stack;
            }
        }

        return cells;
    }

    private static OrigamiFoldLink[] CreateWholeStripRowZone(
        OrigamiFoldTransformStack[,] stacks,
        Transform actionsRoot,
        Transform pointsRoot,
        Transform linksRoot,
        Camera camera,
        GameObject executeIndicator,
        OrigamiFoldActionCoordinator coordinator)
    {
        float mapLeftX = -2.5f;
        float mapRightX = 2.5f;
        float rowCenterY = -0.5f;
        float rowTopY = rowCenterY + 0.5f;
        float rowBottomY = rowCenterY - 0.5f;
        float pointSize = 0.22f;
        float mergedPointSize = 0.30f;

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "Row_Point_TopLeft",
            new Vector3(mapLeftX, rowTopY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "Row_Point_BottomLeft",
            new Vector3(mapLeftX, rowBottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "Row_Point_TopRight",
            new Vector3(mapRightX, rowTopY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "Row_Point_BottomRight",
            new Vector3(mapRightX, rowBottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint mergedLeft = CreateFoldPoint(
            "Row_Point_MergedLeft",
            new Vector3(mapLeftX, rowCenterY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldPoint mergedRight = CreateFoldPoint(
            "Row_Point_MergedRight",
            new Vector3(mapRightX, rowCenterY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldStripSqueezeAction action = CreateStripSqueezeAction(
            "Row_StripSqueezeAction",
            actionsRoot,
            CreateRowStripTargets(stacks, 1),
            new[] { topLeft, bottomLeft, topRight, bottomRight },
            new[] { mergedLeft, mergedRight },
            coordinator);

        ConfigureMergedStripClickAction(mergedLeft, action, camera);
        ConfigureMergedStripClickAction(mergedRight, action, camera);
        mergedLeft.gameObject.SetActive(false);
        mergedRight.gameObject.SetActive(false);

        OrigamiFoldLink leftTopToBottom = CreateFoldLink(
            "Row_Link_Left_TTB",
            topLeft,
            bottomLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink leftBottomToTop = CreateFoldLink(
            "Row_Link_Left_BTT",
            bottomLeft,
            topLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink rightTopToBottom = CreateFoldLink(
            "Row_Link_Right_TTB",
            topRight,
            bottomRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink rightBottomToTop = CreateFoldLink(
            "Row_Link_Right_BTT",
            bottomRight,
            topRight,
            executeIndicator,
            linksRoot);

        ConfigureFoldStripSqueezeLink(leftTopToBottom, action, true);
        ConfigureFoldStripSqueezeLink(leftBottomToTop, action, true);
        ConfigureFoldStripSqueezeLink(rightTopToBottom, action, true);
        ConfigureFoldStripSqueezeLink(rightBottomToTop, action, true);

        return new[]
        {
            leftTopToBottom,
            leftBottomToTop,
            rightTopToBottom,
            rightBottomToTop
        };
    }

    private static OrigamiFoldLink[] CreateWholeStripColumnZone(
        OrigamiFoldTransformStack[,] stacks,
        Transform actionsRoot,
        Transform pointsRoot,
        Transform linksRoot,
        Camera camera,
        GameObject executeIndicator,
        OrigamiFoldActionCoordinator coordinator)
    {
        float mapBottomY = -2f;
        float mapTopY = 2f;
        float columnCenterX = 1f;
        float columnLeftX = columnCenterX - 0.5f;
        float columnRightX = columnCenterX + 0.5f;
        float pointSize = 0.22f;
        float mergedPointSize = 0.30f;

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "Column_Point_TopLeft",
            new Vector3(columnLeftX, mapTopY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "Column_Point_TopRight",
            new Vector3(columnRightX, mapTopY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "Column_Point_BottomLeft",
            new Vector3(columnLeftX, mapBottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "Column_Point_BottomRight",
            new Vector3(columnRightX, mapBottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint mergedTop = CreateFoldPoint(
            "Column_Point_MergedTop",
            new Vector3(columnCenterX, mapTopY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldPoint mergedBottom = CreateFoldPoint(
            "Column_Point_MergedBottom",
            new Vector3(columnCenterX, mapBottomY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldStripSqueezeAction action = CreateStripSqueezeAction(
            "Column_StripSqueezeAction",
            actionsRoot,
            CreateColumnStripTargets(stacks, 3),
            new[] { topLeft, topRight, bottomLeft, bottomRight },
            new[] { mergedTop, mergedBottom },
            coordinator);

        ConfigureMergedStripClickAction(mergedTop, action, camera);
        ConfigureMergedStripClickAction(mergedBottom, action, camera);
        mergedTop.gameObject.SetActive(false);
        mergedBottom.gameObject.SetActive(false);

        OrigamiFoldLink topLeftToRight = CreateFoldLink(
            "Column_Link_Top_LTR",
            topLeft,
            topRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink topRightToLeft = CreateFoldLink(
            "Column_Link_Top_RTL",
            topRight,
            topLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink bottomLeftToRight = CreateFoldLink(
            "Column_Link_Bottom_LTR",
            bottomLeft,
            bottomRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink bottomRightToLeft = CreateFoldLink(
            "Column_Link_Bottom_RTL",
            bottomRight,
            bottomLeft,
            executeIndicator,
            linksRoot);

        ConfigureFoldStripSqueezeLink(topLeftToRight, action, true);
        ConfigureFoldStripSqueezeLink(topRightToLeft, action, true);
        ConfigureFoldStripSqueezeLink(bottomLeftToRight, action, true);
        ConfigureFoldStripSqueezeLink(bottomRightToLeft, action, true);

        return new[]
        {
            topLeftToRight,
            topRightToLeft,
            bottomLeftToRight,
            bottomRightToLeft
        };
    }

    private static OrigamiStripContributionTarget[] CreateRowStripTargets(
        OrigamiFoldTransformStack[,] stacks,
        int foldRow)
    {
        OrigamiStripContributionTarget[] targets = new OrigamiStripContributionTarget[20];
        int index = 0;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Vector3 offset = Vector3.zero;
                Vector3 scaleMultiplier = Vector3.one;

                if (y < foldRow)
                {
                    offset = new Vector3(0f, 0.5f, 0f);
                }
                else if (y == foldRow)
                {
                    scaleMultiplier = new Vector3(1f, 0.02f, 1f);
                }
                else
                {
                    offset = new Vector3(0f, -0.5f, 0f);
                }

                targets[index] = new OrigamiStripContributionTarget
                {
                    stack = stacks[x, y],
                    activeLocalPositionOffset = offset,
                    activeLocalScaleMultiplier = scaleMultiplier
                };
                index++;
            }
        }

        return targets;
    }

    private static OrigamiStripContributionTarget[] CreateColumnStripTargets(
        OrigamiFoldTransformStack[,] stacks,
        int foldColumn)
    {
        OrigamiStripContributionTarget[] targets = new OrigamiStripContributionTarget[20];
        int index = 0;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Vector3 offset = Vector3.zero;
                Vector3 scaleMultiplier = Vector3.one;

                if (x < foldColumn)
                {
                    offset = new Vector3(0.5f, 0f, 0f);
                }
                else if (x == foldColumn)
                {
                    scaleMultiplier = new Vector3(0.02f, 1f, 1f);
                }
                else
                {
                    offset = new Vector3(-0.5f, 0f, 0f);
                }

                targets[index] = new OrigamiStripContributionTarget
                {
                    stack = stacks[x, y],
                    activeLocalPositionOffset = offset,
                    activeLocalScaleMultiplier = scaleMultiplier
                };
                index++;
            }
        }

        return targets;
    }

    private static OrigamiFoldStripSqueezeAction CreateStripSqueezeAction(
        string objectName,
        Transform parent,
        OrigamiStripContributionTarget[] targets,
        OrigamiFoldPoint[] sourcePoints,
        OrigamiFoldPoint[] mergedPoints,
        OrigamiFoldActionCoordinator coordinator)
    {
        GameObject actionObject = new GameObject(objectName);
        actionObject.transform.SetParent(parent);

        OrigamiFoldStripSqueezeAction action =
            actionObject.AddComponent<OrigamiFoldStripSqueezeAction>();
        action.animationDuration = 0.3f;
        action.isActive = false;
        action.coordinator = coordinator;
        action.useCoordinator = true;
        action.targets = targets;
        action.enableAfterActive = ToGameObjects(mergedPoints);
        action.disableAfterActive = ToGameObjects(sourcePoints);
        action.enableAfterInactive = ToGameObjects(sourcePoints);
        action.disableAfterInactive = ToGameObjects(mergedPoints);

        return action;
    }

    private static void ConfigureMergedStripClickAction(
        OrigamiFoldPoint mergedPoint,
        OrigamiFoldStripSqueezeAction action,
        Camera camera)
    {
        if (mergedPoint == null)
        {
            return;
        }

        OrigamiFoldClickAction clickAction = mergedPoint.gameObject
            .AddComponent<OrigamiFoldClickAction>();

        clickAction.targetCamera = camera;
        clickAction.targetStripSqueezeAction = action;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = mergedPoint.pointId;
    }

    private static void ConfigureFoldStripSqueezeLink(
        OrigamiFoldLink link,
        OrigamiFoldStripSqueezeAction action,
        bool activeStateOnExecute)
    {
        if (link == null)
        {
            return;
        }

        link.bidirectional = false;
        link.targetStripSqueezeAction = action;
        link.activeStateOnExecute = activeStateOnExecute;
    }

    private static void CreateWholeStripGuides(Transform parent)
    {
        Color gridColor = new Color(1f, 1f, 1f, 0.22f);

        for (int x = 0; x <= 5; x++)
        {
            CreateGuideLine(
                $"WholeStripGuide_Vertical_{x}",
                new Vector3(-2.5f + x, 0f, 0.1f),
                new Vector3(0.02f, 4.05f, 1f),
                gridColor,
                parent);
        }

        for (int y = 0; y <= 4; y++)
        {
            CreateGuideLine(
                $"WholeStripGuide_Horizontal_{y}",
                new Vector3(0f, -2f + y, 0.1f),
                new Vector3(5.05f, 0.02f, 1f),
                gridColor,
                parent);
        }

        CreateStripFrameGuide(
            "Row_FoldStripGuide",
            new Vector3(0f, -0.5f, 0f),
            new Vector2(5f, 1f),
            parent);

        CreateStripFrameGuide(
            "Column_FoldStripGuide",
            new Vector3(1f, 0f, 0f),
            new Vector2(1f, 4f),
            parent);
    }

    private static void CreateStripFrameGuide(
        string objectName,
        Vector3 center,
        Vector2 size,
        Transform parent)
    {
        Color regionColor = new Color(1f, 0.9f, 0.18f, 0.48f);
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        CreateGuideLine(
            $"{objectName}_Left",
            new Vector3(center.x - halfWidth, center.y, 0.08f),
            new Vector3(0.035f, size.y + 0.08f, 1f),
            regionColor,
            parent);

        CreateGuideLine(
            $"{objectName}_Right",
            new Vector3(center.x + halfWidth, center.y, 0.08f),
            new Vector3(0.035f, size.y + 0.08f, 1f),
            regionColor,
            parent);

        CreateGuideLine(
            $"{objectName}_Top",
            new Vector3(center.x, center.y + halfHeight, 0.08f),
            new Vector3(size.x + 0.08f, 0.035f, 1f),
            regionColor,
            parent);

        CreateGuideLine(
            $"{objectName}_Bottom",
            new Vector3(center.x, center.y - halfHeight, 0.08f),
            new Vector3(size.x + 0.08f, 0.035f, 1f),
            regionColor,
            parent);
    }

    private static void RebuildUnifiedMapOrigamiObjects(
        GameObject systemRoot,
        GameObject pointsRoot,
        GameObject linksRoot,
        Camera camera,
        GameObject executeIndicator)
    {
        GameObject actionsRoot = new GameObject("ORIGAMI_ACTIONS");
        GameObject guidesRoot = new GameObject("ORIGAMI_GRID_GUIDES");
        GameObject mapRoot = new GameObject("ORIGAMI_UNIFIED_MAP");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(mapRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        OrigamiFoldActionCoordinator coordinator = systemRoot.AddComponent<OrigamiFoldActionCoordinator>();
        GameObject[,] cells = CreateUnifiedMapCells(cellsRoot.transform);

        OrigamiFoldLink[] horizontalLinks = CreateUnifiedHorizontalZone(
            cells,
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            camera,
            executeIndicator,
            coordinator);

        OrigamiFoldLink[] verticalLinks = CreateUnifiedVerticalZone(
            cells,
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            camera,
            executeIndicator,
            coordinator);

        CreateUnifiedMapGuides(guidesRoot.transform);

        OrigamiFoldDragController controller = systemRoot.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = camera;
        controller.snapDistance = 0.5f;
        controller.autoFindLinks = true;
        controller.links = new[]
        {
            horizontalLinks[0],
            horizontalLinks[1],
            horizontalLinks[2],
            horizontalLinks[3],
            verticalLinks[0],
            verticalLinks[1],
            verticalLinks[2],
            verticalLinks[3]
        };
    }

    private static GameObject[,] CreateUnifiedMapCells(Transform parent)
    {
        GameObject[,] cells = new GameObject[5, 4];
        float cellVisualSize = 0.92f;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Color color = new Color(0.18f, 0.24f, 0.30f);

                if (x == 1 && y == 1)
                {
                    color = new Color(0.90f, 0.58f, 0.20f);
                }
                else if (x == 4 && y == 1)
                {
                    color = new Color(0.72f, 0.38f, 0.72f);
                }
                else if (y == 1 && x <= 3)
                {
                    color = new Color(0.20f, 0.34f, 0.42f);
                }
                else if (x == 4)
                {
                    color = new Color(0.28f, 0.36f, 0.44f);
                }

                Vector3 position = new Vector3((x - 2) * 1f, (y - 1.5f) * 1f, 0f);
                cells[x, y] = CreateSqueezeCell(
                    $"MapCell_{x}_{y}",
                    position,
                    color,
                    cellVisualSize,
                    parent,
                    $"{x},{y}");
            }
        }

        return cells;
    }

    private static OrigamiFoldLink[] CreateUnifiedHorizontalZone(
        GameObject[,] cells,
        Transform actionsRoot,
        Transform pointsRoot,
        Transform linksRoot,
        Camera camera,
        GameObject executeIndicator,
        OrigamiFoldActionCoordinator coordinator)
    {
        GameObject leftCell = cells[0, 1];
        GameObject collapsibleCell = cells[1, 1];
        GameObject rightCell = cells[2, 1];
        GameObject bufferCell = cells[3, 1];
        Vector3 collapsibleCenter = collapsibleCell.transform.localPosition;
        float leftX = collapsibleCenter.x - 0.5f;
        float rightX = collapsibleCenter.x + 0.5f;
        float topY = collapsibleCenter.y + 0.5f;
        float bottomY = collapsibleCenter.y - 0.5f;
        float pointSize = 0.22f;
        float mergedPointSize = 0.30f;

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "H_Point_TopLeft",
            new Vector3(leftX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "H_Point_TopRight",
            new Vector3(rightX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "H_Point_BottomLeft",
            new Vector3(leftX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "H_Point_BottomRight",
            new Vector3(rightX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint mergedTop = CreateFoldPoint(
            "H_Point_MergedTop",
            new Vector3(collapsibleCenter.x, topY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldPoint mergedBottom = CreateFoldPoint(
            "H_Point_MergedBottom",
            new Vector3(collapsibleCenter.x, bottomY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldSqueezeAction action = CreateSqueezeAction(
            "H_SqueezeAction",
            actionsRoot,
            new[]
            {
                new OrigamiSqueezeTarget
                {
                    target = leftCell.transform,
                    activeLocalPositionOffset = new Vector3(0.5f, 0f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = rightCell.transform,
                    activeLocalPositionOffset = new Vector3(-0.5f, 0f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = bufferCell.transform,
                    activeLocalPositionOffset = new Vector3(-0.5f, 0f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = collapsibleCell.transform,
                    activeLocalPositionOffset = Vector3.zero,
                    activeLocalScale = new Vector3(0.02f, 1f, 1f)
                }
            },
            collapsibleCell,
            new[] { topLeft, topRight, bottomLeft, bottomRight },
            new[] { mergedTop, mergedBottom });

        action.coordinator = coordinator;
        action.useCoordinator = true;

        ConfigureMergedSqueezeClickAction(mergedTop, action, camera);
        ConfigureMergedSqueezeClickAction(mergedBottom, action, camera);
        mergedTop.gameObject.SetActive(false);
        mergedBottom.gameObject.SetActive(false);

        OrigamiFoldLink topLeftToRight = CreateFoldLink(
            "H_Link_Top_LTR",
            topLeft,
            topRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink topRightToLeft = CreateFoldLink(
            "H_Link_Top_RTL",
            topRight,
            topLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink bottomLeftToRight = CreateFoldLink(
            "H_Link_Bottom_LTR",
            bottomLeft,
            bottomRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink bottomRightToLeft = CreateFoldLink(
            "H_Link_Bottom_RTL",
            bottomRight,
            bottomLeft,
            executeIndicator,
            linksRoot);

        ConfigureFoldSqueezeLink(topLeftToRight, action, true);
        ConfigureFoldSqueezeLink(topRightToLeft, action, true);
        ConfigureFoldSqueezeLink(bottomLeftToRight, action, true);
        ConfigureFoldSqueezeLink(bottomRightToLeft, action, true);

        return new[]
        {
            topLeftToRight,
            topRightToLeft,
            bottomLeftToRight,
            bottomRightToLeft
        };
    }

    private static OrigamiFoldLink[] CreateUnifiedVerticalZone(
        GameObject[,] cells,
        Transform actionsRoot,
        Transform pointsRoot,
        Transform linksRoot,
        Camera camera,
        GameObject executeIndicator,
        OrigamiFoldActionCoordinator coordinator)
    {
        GameObject bottomCell = cells[4, 0];
        GameObject collapsibleCell = cells[4, 1];
        GameObject topCell = cells[4, 2];
        GameObject bufferCell = cells[4, 3];
        Vector3 collapsibleCenter = collapsibleCell.transform.localPosition;
        float leftX = collapsibleCenter.x - 0.5f;
        float rightX = collapsibleCenter.x + 0.5f;
        float topY = collapsibleCenter.y + 0.5f;
        float bottomY = collapsibleCenter.y - 0.5f;
        float pointSize = 0.22f;
        float mergedPointSize = 0.30f;

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "V_Point_TopLeft",
            new Vector3(leftX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "V_Point_TopRight",
            new Vector3(rightX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "V_Point_BottomLeft",
            new Vector3(leftX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "V_Point_BottomRight",
            new Vector3(rightX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint mergedLeft = CreateFoldPoint(
            "V_Point_MergedLeft",
            new Vector3(leftX, collapsibleCenter.y, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldPoint mergedRight = CreateFoldPoint(
            "V_Point_MergedRight",
            new Vector3(rightX, collapsibleCenter.y, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldSqueezeAction action = CreateSqueezeAction(
            "V_SqueezeAction",
            actionsRoot,
            new[]
            {
                new OrigamiSqueezeTarget
                {
                    target = bottomCell.transform,
                    activeLocalPositionOffset = new Vector3(0f, 0.5f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = topCell.transform,
                    activeLocalPositionOffset = new Vector3(0f, -0.5f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = bufferCell.transform,
                    activeLocalPositionOffset = new Vector3(0f, -0.5f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = collapsibleCell.transform,
                    activeLocalPositionOffset = Vector3.zero,
                    activeLocalScale = new Vector3(1f, 0.02f, 1f)
                }
            },
            collapsibleCell,
            new[] { topLeft, topRight, bottomLeft, bottomRight },
            new[] { mergedLeft, mergedRight });

        action.coordinator = coordinator;
        action.useCoordinator = true;

        ConfigureMergedSqueezeClickAction(mergedLeft, action, camera);
        ConfigureMergedSqueezeClickAction(mergedRight, action, camera);
        mergedLeft.gameObject.SetActive(false);
        mergedRight.gameObject.SetActive(false);

        OrigamiFoldLink leftTopToBottom = CreateFoldLink(
            "V_Link_Left_TTB",
            topLeft,
            bottomLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink leftBottomToTop = CreateFoldLink(
            "V_Link_Left_BTT",
            bottomLeft,
            topLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink rightTopToBottom = CreateFoldLink(
            "V_Link_Right_TTB",
            topRight,
            bottomRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink rightBottomToTop = CreateFoldLink(
            "V_Link_Right_BTT",
            bottomRight,
            topRight,
            executeIndicator,
            linksRoot);

        ConfigureFoldSqueezeLink(leftTopToBottom, action, true);
        ConfigureFoldSqueezeLink(leftBottomToTop, action, true);
        ConfigureFoldSqueezeLink(rightTopToBottom, action, true);
        ConfigureFoldSqueezeLink(rightBottomToTop, action, true);

        return new[]
        {
            leftTopToBottom,
            leftBottomToTop,
            rightTopToBottom,
            rightBottomToTop
        };
    }

    private static void CreateUnifiedMapGuides(Transform parent)
    {
        Color gridColor = new Color(1f, 1f, 1f, 0.22f);

        for (int x = 0; x <= 5; x++)
        {
            CreateGuideLine(
                $"UnifiedGuide_Vertical_{x}",
                new Vector3(-2.5f + x, 0f, 0.1f),
                new Vector3(0.02f, 4.05f, 1f),
                gridColor,
                parent);
        }

        for (int y = 0; y <= 4; y++)
        {
            CreateGuideLine(
                $"UnifiedGuide_Horizontal_{y}",
                new Vector3(0f, -2f + y, 0.1f),
                new Vector3(5.05f, 0.02f, 1f),
                gridColor,
                parent);
        }

        CreateSqueezeFrameGuide("H_UnifiedFoldZone", new Vector3(-1f, -0.5f, 0f), parent);
        CreateSqueezeFrameGuide("V_UnifiedFoldZone", new Vector3(2f, -0.5f, 0f), parent);
    }

    private static void RebuildSqueezeOrigamiObjects(
        GameObject systemRoot,
        GameObject pointsRoot,
        GameObject linksRoot,
        Camera camera,
        GameObject executeIndicator)
    {
        GameObject actionsRoot = new GameObject("ORIGAMI_ACTIONS");
        GameObject guidesRoot = new GameObject("ORIGAMI_GRID_GUIDES");

        OrigamiFoldLink[] horizontalLinks = CreateHorizontalSqueezeTest(
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            guidesRoot.transform,
            camera,
            executeIndicator);

        OrigamiFoldLink[] verticalLinks = CreateVerticalSqueezeTest(
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            guidesRoot.transform,
            camera,
            executeIndicator);

        OrigamiFoldDragController controller = systemRoot.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = camera;
        controller.snapDistance = 0.5f;
        controller.autoFindLinks = true;
        controller.links = new[]
        {
            horizontalLinks[0],
            horizontalLinks[1],
            horizontalLinks[2],
            horizontalLinks[3],
            verticalLinks[0],
            verticalLinks[1],
            verticalLinks[2],
            verticalLinks[3]
        };
    }

    private static OrigamiFoldLink[] CreateHorizontalSqueezeTest(
        Transform actionsRoot,
        Transform pointsRoot,
        Transform linksRoot,
        Transform guidesRoot,
        Camera camera,
        GameObject executeIndicator)
    {
        GameObject testRoot = new GameObject("ORIGAMI_SQUEEZE_HORIZONTAL");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(testRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        float cellVisualSize = 0.92f;
        float pointSize = 0.22f;
        float mergedPointSize = 0.30f;
        Vector3 collapsibleCenter = new Vector3(-3.5f, 0f, 0f);
        float leftX = collapsibleCenter.x - 0.5f;
        float rightX = collapsibleCenter.x + 0.5f;
        float topY = collapsibleCenter.y + 0.5f;
        float bottomY = collapsibleCenter.y - 0.5f;

        GameObject cell0 = CreateSqueezeCell(
            "H_Cell_0",
            new Vector3(-4.5f, 0f, 0f),
            new Color(0.16f, 0.46f, 0.78f),
            cellVisualSize,
            cellsRoot.transform);

        GameObject cell1 = CreateSqueezeCell(
            "H_Cell_1_Collapsible",
            collapsibleCenter,
            new Color(0.90f, 0.58f, 0.20f),
            cellVisualSize,
            cellsRoot.transform);

        GameObject cell2 = CreateSqueezeCell(
            "H_Cell_2",
            new Vector3(-2.5f, 0f, 0f),
            new Color(0.28f, 0.68f, 0.38f),
            cellVisualSize,
            cellsRoot.transform);

        GameObject buffer = CreateSqueezeCell(
            "H_Cell_3_Buffer",
            new Vector3(-1.5f, 0f, 0f),
            new Color(0.36f, 0.22f, 0.42f),
            cellVisualSize,
            cellsRoot.transform);

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "H_Point_TopLeft",
            new Vector3(leftX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "H_Point_TopRight",
            new Vector3(rightX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "H_Point_BottomLeft",
            new Vector3(leftX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "H_Point_BottomRight",
            new Vector3(rightX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint mergedTop = CreateFoldPoint(
            "H_Point_MergedTop",
            new Vector3(collapsibleCenter.x, topY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldPoint mergedBottom = CreateFoldPoint(
            "H_Point_MergedBottom",
            new Vector3(collapsibleCenter.x, bottomY, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldSqueezeAction action = CreateSqueezeAction(
            "H_SqueezeAction",
            actionsRoot,
            new[]
            {
                new OrigamiSqueezeTarget
                {
                    target = cell0.transform,
                    activeLocalPositionOffset = new Vector3(0.5f, 0f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = cell2.transform,
                    activeLocalPositionOffset = new Vector3(-0.5f, 0f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = buffer.transform,
                    activeLocalPositionOffset = new Vector3(-0.5f, 0f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = cell1.transform,
                    activeLocalPositionOffset = Vector3.zero,
                    activeLocalScale = new Vector3(0.02f, 1f, 1f)
                }
            },
            cell1,
            new[] { topLeft, topRight, bottomLeft, bottomRight },
            new[] { mergedTop, mergedBottom });

        ConfigureMergedSqueezeClickAction(mergedTop, action, camera);
        ConfigureMergedSqueezeClickAction(mergedBottom, action, camera);
        mergedTop.gameObject.SetActive(false);
        mergedBottom.gameObject.SetActive(false);

        OrigamiFoldLink topLeftToRight = CreateFoldLink(
            "H_Link_Top_LTR",
            topLeft,
            topRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink topRightToLeft = CreateFoldLink(
            "H_Link_Top_RTL",
            topRight,
            topLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink bottomLeftToRight = CreateFoldLink(
            "H_Link_Bottom_LTR",
            bottomLeft,
            bottomRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink bottomRightToLeft = CreateFoldLink(
            "H_Link_Bottom_RTL",
            bottomRight,
            bottomLeft,
            executeIndicator,
            linksRoot);

        ConfigureFoldSqueezeLink(topLeftToRight, action, true);
        ConfigureFoldSqueezeLink(topRightToLeft, action, true);
        ConfigureFoldSqueezeLink(bottomLeftToRight, action, true);
        ConfigureFoldSqueezeLink(bottomRightToLeft, action, true);

        CreateSqueezeFrameGuide("H_Guide_Collapsible", collapsibleCenter, guidesRoot);

        return new[]
        {
            topLeftToRight,
            topRightToLeft,
            bottomLeftToRight,
            bottomRightToLeft
        };
    }

    private static OrigamiFoldLink[] CreateVerticalSqueezeTest(
        Transform actionsRoot,
        Transform pointsRoot,
        Transform linksRoot,
        Transform guidesRoot,
        Camera camera,
        GameObject executeIndicator)
    {
        GameObject testRoot = new GameObject("ORIGAMI_SQUEEZE_VERTICAL");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(testRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        float cellVisualSize = 0.92f;
        float pointSize = 0.22f;
        float mergedPointSize = 0.30f;
        Vector3 collapsibleCenter = new Vector3(2.2f, -0.5f, 0f);
        float leftX = collapsibleCenter.x - 0.5f;
        float rightX = collapsibleCenter.x + 0.5f;
        float topY = collapsibleCenter.y + 0.5f;
        float bottomY = collapsibleCenter.y - 0.5f;

        GameObject cell0 = CreateSqueezeCell(
            "V_Cell_0",
            new Vector3(2.2f, -1.5f, 0f),
            new Color(0.16f, 0.46f, 0.78f),
            cellVisualSize,
            cellsRoot.transform);

        GameObject cell1 = CreateSqueezeCell(
            "V_Cell_1_Collapsible",
            collapsibleCenter,
            new Color(0.90f, 0.58f, 0.20f),
            cellVisualSize,
            cellsRoot.transform);

        GameObject cell2 = CreateSqueezeCell(
            "V_Cell_2",
            new Vector3(2.2f, 0.5f, 0f),
            new Color(0.28f, 0.68f, 0.38f),
            cellVisualSize,
            cellsRoot.transform);

        GameObject buffer = CreateSqueezeCell(
            "V_Cell_3_Buffer",
            new Vector3(2.2f, 1.5f, 0f),
            new Color(0.36f, 0.22f, 0.42f),
            cellVisualSize,
            cellsRoot.transform);

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "V_Point_TopLeft",
            new Vector3(leftX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "V_Point_TopRight",
            new Vector3(rightX, topY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "V_Point_BottomLeft",
            new Vector3(leftX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "V_Point_BottomRight",
            new Vector3(rightX, bottomY, 0f),
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            pointSize * 0.5f,
            30,
            pointsRoot);

        OrigamiFoldPoint mergedLeft = CreateFoldPoint(
            "V_Point_MergedLeft",
            new Vector3(leftX, collapsibleCenter.y, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldPoint mergedRight = CreateFoldPoint(
            "V_Point_MergedRight",
            new Vector3(rightX, collapsibleCenter.y, 0f),
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointSize * 0.5f,
            35,
            pointsRoot);

        OrigamiFoldSqueezeAction action = CreateSqueezeAction(
            "V_SqueezeAction",
            actionsRoot,
            new[]
            {
                new OrigamiSqueezeTarget
                {
                    target = cell0.transform,
                    activeLocalPositionOffset = new Vector3(0f, 0.5f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = cell2.transform,
                    activeLocalPositionOffset = new Vector3(0f, -0.5f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = buffer.transform,
                    activeLocalPositionOffset = new Vector3(0f, -0.5f, 0f),
                    activeLocalScale = Vector3.one
                },
                new OrigamiSqueezeTarget
                {
                    target = cell1.transform,
                    activeLocalPositionOffset = Vector3.zero,
                    activeLocalScale = new Vector3(1f, 0.02f, 1f)
                }
            },
            cell1,
            new[] { topLeft, topRight, bottomLeft, bottomRight },
            new[] { mergedLeft, mergedRight });

        ConfigureMergedSqueezeClickAction(mergedLeft, action, camera);
        ConfigureMergedSqueezeClickAction(mergedRight, action, camera);
        mergedLeft.gameObject.SetActive(false);
        mergedRight.gameObject.SetActive(false);

        OrigamiFoldLink leftTopToBottom = CreateFoldLink(
            "V_Link_Left_TTB",
            topLeft,
            bottomLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink leftBottomToTop = CreateFoldLink(
            "V_Link_Left_BTT",
            bottomLeft,
            topLeft,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink rightTopToBottom = CreateFoldLink(
            "V_Link_Right_TTB",
            topRight,
            bottomRight,
            executeIndicator,
            linksRoot);

        OrigamiFoldLink rightBottomToTop = CreateFoldLink(
            "V_Link_Right_BTT",
            bottomRight,
            topRight,
            executeIndicator,
            linksRoot);

        ConfigureFoldSqueezeLink(leftTopToBottom, action, true);
        ConfigureFoldSqueezeLink(leftBottomToTop, action, true);
        ConfigureFoldSqueezeLink(rightTopToBottom, action, true);
        ConfigureFoldSqueezeLink(rightBottomToTop, action, true);

        CreateSqueezeFrameGuide("V_Guide_Collapsible", collapsibleCenter, guidesRoot);

        return new[]
        {
            leftTopToBottom,
            leftBottomToTop,
            rightTopToBottom,
            rightBottomToTop
        };
    }

    private static GameObject CreateSqueezeCell(
        string objectName,
        Vector3 localPosition,
        Color color,
        float cellVisualSize,
        Transform parent,
        string labelText = null)
    {
        GameObject cellObject = new GameObject(objectName);
        cellObject.transform.SetParent(parent);
        cellObject.transform.localPosition = localPosition;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(cellObject.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(cellVisualSize, cellVisualSize, 1f);

        Collider visualCollider = visual.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = 10;

        CreateCellLabel(string.IsNullOrEmpty(labelText) ? objectName : labelText, cellObject.transform);

        return cellObject;
    }

    private static OrigamiFoldSqueezeAction CreateSqueezeAction(
        string objectName,
        Transform parent,
        OrigamiSqueezeTarget[] targets,
        GameObject collapsibleCell,
        OrigamiFoldPoint[] sourcePoints,
        OrigamiFoldPoint[] mergedPoints)
    {
        GameObject actionObject = new GameObject(objectName);
        actionObject.transform.SetParent(parent);

        OrigamiFoldSqueezeAction action = actionObject.AddComponent<OrigamiFoldSqueezeAction>();
        action.animationDuration = 0.3f;
        action.isActive = false;
        action.targets = targets;
        action.enableAfterActive = ToGameObjects(mergedPoints);
        action.disableAfterActive = Combine(collapsibleCell, ToGameObjects(sourcePoints));
        action.enableBeforeInactive = collapsibleCell == null
            ? new GameObject[0]
            : new[] { collapsibleCell };
        action.enableAfterInactive = ToGameObjects(sourcePoints);
        action.disableAfterInactive = ToGameObjects(mergedPoints);

        return action;
    }

    private static void ConfigureMergedSqueezeClickAction(
        OrigamiFoldPoint mergedPoint,
        OrigamiFoldSqueezeAction action,
        Camera camera)
    {
        if (mergedPoint == null)
        {
            return;
        }

        OrigamiFoldClickAction clickAction = mergedPoint.gameObject
            .AddComponent<OrigamiFoldClickAction>();

        clickAction.targetCamera = camera;
        clickAction.targetSqueezeAction = action;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = mergedPoint.pointId;
    }

    private static void ConfigureFoldSqueezeLink(
        OrigamiFoldLink link,
        OrigamiFoldSqueezeAction action,
        bool activeStateOnExecute)
    {
        if (link == null)
        {
            return;
        }

        link.bidirectional = false;
        link.targetSqueezeAction = action;
        link.activeStateOnExecute = activeStateOnExecute;
    }

    private static void CreateSqueezeFrameGuide(
        string objectName,
        Vector3 center,
        Transform parent)
    {
        Color regionColor = new Color(1f, 0.9f, 0.18f, 0.45f);

        CreateGuideLine(
            $"{objectName}_Left",
            new Vector3(center.x - 0.5f, center.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            parent);

        CreateGuideLine(
            $"{objectName}_Right",
            new Vector3(center.x + 0.5f, center.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            parent);

        CreateGuideLine(
            $"{objectName}_Top",
            new Vector3(center.x, center.y + 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            parent);

        CreateGuideLine(
            $"{objectName}_Bottom",
            new Vector3(center.x, center.y - 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            parent);
    }

    private static void RebuildVerticalOrigamiObjects(
        GameObject systemRoot,
        GameObject pointsRoot,
        GameObject linksRoot,
        Camera camera,
        GameObject executeIndicator)
    {
        Vector2 boardOrigin = new Vector2(0f, -1.5f);
        float cellSize = 1f;
        float cellVisualSize = 0.92f;
        float pointSize = 0.22f;
        float sourcePointColliderRadius = pointSize * 0.5f;
        float mergedPointSize = 0.30f;
        float mergedPointColliderRadius = mergedPointSize * 0.5f;

        Vector3 collapsibleCenter = GridToLocalPosition(boardOrigin, cellSize, new Vector2Int(0, 1));
        float leftX = collapsibleCenter.x - cellSize * 0.5f;
        float rightX = collapsibleCenter.x + cellSize * 0.5f;
        float topY = collapsibleCenter.y + cellSize * 0.5f;
        float bottomY = collapsibleCenter.y - cellSize * 0.5f;

        Vector3 topLeftPosition = new Vector3(leftX, topY, 0f);
        Vector3 topRightPosition = new Vector3(rightX, topY, 0f);
        Vector3 bottomLeftPosition = new Vector3(leftX, bottomY, 0f);
        Vector3 bottomRightPosition = new Vector3(rightX, bottomY, 0f);

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "OrigamiPoint_TopLeft",
            topLeftPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "OrigamiPoint_TopRight",
            topRightPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "OrigamiPoint_BottomLeft",
            bottomLeftPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "OrigamiPoint_BottomRight",
            bottomRightPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint mergedLeft = CreateFoldPoint(
            "OrigamiPoint_MergedLeft",
            bottomLeftPosition,
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointColliderRadius,
            35,
            pointsRoot.transform);

        OrigamiFoldPoint mergedRight = CreateFoldPoint(
            "OrigamiPoint_MergedRight",
            bottomRightPosition,
            Color.cyan,
            new Vector3(mergedPointSize, mergedPointSize, 1f),
            mergedPointColliderRadius,
            35,
            pointsRoot.transform);

        OrigamiFoldMoveAction compressVerticalAction = CreateVerticalGridAndAction(
            boardOrigin,
            cellSize,
            cellVisualSize,
            new[] { topLeft, topRight, bottomLeft, bottomRight },
            new[] { mergedLeft, mergedRight });

        ConfigureMergedClickAction(mergedLeft, compressVerticalAction, camera);
        ConfigureMergedClickAction(mergedRight, compressVerticalAction, camera);
        mergedLeft.gameObject.SetActive(false);
        mergedRight.gameObject.SetActive(false);

        CreateVerticalGridGuides(boardOrigin, cellSize);

        OrigamiFoldLink leftTopToBottom = CreateFoldLink(
            "OrigamiLink_Left_TTB",
            topLeft,
            bottomLeft,
            executeIndicator,
            linksRoot.transform);

        OrigamiFoldLink leftBottomToTop = CreateFoldLink(
            "OrigamiLink_Left_BTT",
            bottomLeft,
            topLeft,
            executeIndicator,
            linksRoot.transform);

        OrigamiFoldLink rightTopToBottom = CreateFoldLink(
            "OrigamiLink_Right_TTB",
            topRight,
            bottomRight,
            executeIndicator,
            linksRoot.transform);

        OrigamiFoldLink rightBottomToTop = CreateFoldLink(
            "OrigamiLink_Right_BTT",
            bottomRight,
            topRight,
            executeIndicator,
            linksRoot.transform);

        ConfigureFoldMoveLink(leftTopToBottom, compressVerticalAction, true);
        ConfigureFoldMoveLink(leftBottomToTop, compressVerticalAction, true);
        ConfigureFoldMoveLink(rightTopToBottom, compressVerticalAction, true);
        ConfigureFoldMoveLink(rightBottomToTop, compressVerticalAction, true);

        OrigamiFoldDragController controller = systemRoot.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = camera;
        controller.snapDistance = 0.45f;
        controller.autoFindLinks = true;
        controller.links = new[]
        {
            leftTopToBottom,
            leftBottomToTop,
            rightTopToBottom,
            rightBottomToTop
        };
    }

    private static OrigamiFoldMoveAction CreateVerticalGridAndAction(
        Vector2 boardOrigin,
        float cellSize,
        float cellVisualSize,
        OrigamiFoldPoint[] sourcePoints,
        OrigamiFoldPoint[] mergedPoints)
    {
        GameObject gridRoot = new GameObject("ORIGAMI_GRID");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(gridRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        GameObject actionsRoot = new GameObject("ORIGAMI_ACTIONS");

        OrigamiFoldBoard board = gridRoot.AddComponent<OrigamiFoldBoard>();
        board.cellSize = cellSize;
        board.originLocalPosition = boardOrigin;
        board.foldAnimationDuration = 0.25f;

        OrigamiFoldCell cell0 = CreateCell(
            "Cell_0_0",
            new Vector2Int(0, 0),
            new Color(0.16f, 0.46f, 0.78f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell1 = CreateCell(
            "Cell_0_1",
            new Vector2Int(0, 1),
            new Color(0.90f, 0.58f, 0.20f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell2 = CreateCell(
            "Cell_0_2",
            new Vector2Int(0, 2),
            new Color(0.28f, 0.68f, 0.38f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell buffer = CreateCell(
            "Cell_0_3_Buffer",
            new Vector2Int(0, 3),
            new Color(0.36f, 0.22f, 0.42f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        GameObject actionObject = new GameObject("OrigamiFoldMoveAction_CompressVertical");
        actionObject.transform.SetParent(actionsRoot.transform);

        OrigamiFoldMoveAction action = actionObject.AddComponent<OrigamiFoldMoveAction>();
        action.board = board;
        action.isActive = false;
        action.movesWhenActive = new[]
        {
            new OrigamiCellMove
            {
                cell = cell2,
                targetGridPosition = new Vector2Int(0, 1)
            },
            new OrigamiCellMove
            {
                cell = buffer,
                targetGridPosition = new Vector2Int(0, 2)
            }
        };
        action.movesWhenInactive = new[]
        {
            new OrigamiCellMove
            {
                cell = cell2,
                targetGridPosition = new Vector2Int(0, 2)
            },
            new OrigamiCellMove
            {
                cell = buffer,
                targetGridPosition = new Vector2Int(0, 3)
            }
        };
        action.enableWhenActive = ToGameObjects(mergedPoints);
        action.disableWhenActive = Combine(cell1.gameObject, ToGameObjects(sourcePoints));
        action.enableWhenInactive = Combine(cell1.gameObject, ToGameObjects(sourcePoints));
        action.disableWhenInactive = ToGameObjects(mergedPoints);

        board.SnapCellToGrid(cell0);
        board.SnapCellToGrid(cell1);
        board.SnapCellToGrid(cell2);
        board.SnapCellToGrid(buffer);

        return action;
    }

    private static OrigamiFoldMoveAction CreateGridAndAction(
        WorkbenchStep step,
        Vector2 boardOrigin,
        float cellSize,
        float cellVisualSize,
        OrigamiFoldPoint[] sourcePoints,
        OrigamiFoldPoint[] mergedPoints)
    {
        GameObject gridRoot = new GameObject("ORIGAMI_GRID");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(gridRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        GameObject actionsRoot = new GameObject("ORIGAMI_ACTIONS");

        OrigamiFoldBoard board = gridRoot.AddComponent<OrigamiFoldBoard>();
        board.cellSize = cellSize;
        board.originLocalPosition = boardOrigin;
        board.foldAnimationDuration = 0.25f;

        OrigamiFoldCell cell0 = CreateCell(
            "Cell_0_0",
            new Vector2Int(0, 0),
            new Color(0.16f, 0.46f, 0.78f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell1 = CreateCell(
            "Cell_1_0",
            new Vector2Int(1, 0),
            new Color(0.84f, 0.66f, 0.22f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell2 = CreateCell(
            "Cell_2_0",
            new Vector2Int(2, 0),
            new Color(0.28f, 0.68f, 0.38f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell buffer = CreateCell(
            "Cell_3_0_Buffer",
            new Vector2Int(3, 0),
            new Color(0.36f, 0.22f, 0.42f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        GameObject actionObject = new GameObject("OrigamiFoldMoveAction_CompressHorizontal");
        actionObject.transform.SetParent(actionsRoot.transform);

        OrigamiFoldMoveAction action = actionObject.AddComponent<OrigamiFoldMoveAction>();
        action.board = board;
        action.isActive = false;
        action.movesWhenActive = new[]
        {
            new OrigamiCellMove
            {
                cell = cell2,
                targetGridPosition = new Vector2Int(1, 0)
            },
            new OrigamiCellMove
            {
                cell = buffer,
                targetGridPosition = new Vector2Int(2, 0)
            }
        };
        action.movesWhenInactive = new[]
        {
            new OrigamiCellMove
            {
                cell = cell2,
                targetGridPosition = new Vector2Int(2, 0)
            },
            new OrigamiCellMove
            {
                cell = buffer,
                targetGridPosition = new Vector2Int(3, 0)
            }
        };

        if (step == WorkbenchStep.Step3
            || step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2)
        {
            action.enableWhenActive = ToGameObjects(mergedPoints);
            action.disableWhenActive = Combine(cell1.gameObject, ToGameObjects(sourcePoints));
            action.enableWhenInactive = Combine(cell1.gameObject, ToGameObjects(sourcePoints));
            action.disableWhenInactive = ToGameObjects(mergedPoints);
        }
        else
        {
            action.enableWhenActive = new GameObject[0];
            action.disableWhenActive = new[] { cell1.gameObject };
            action.enableWhenInactive = new[] { cell1.gameObject };
            action.disableWhenInactive = new GameObject[0];
        }

        board.SnapCellToGrid(cell0);
        board.SnapCellToGrid(cell1);
        board.SnapCellToGrid(cell2);
        board.SnapCellToGrid(buffer);

        return action;
    }

    private static OrigamiFoldCell CreateCell(
        string objectName,
        Vector2Int gridPosition,
        Color color,
        float cellVisualSize,
        OrigamiFoldBoard board,
        Transform parent)
    {
        GameObject cellObject = new GameObject(objectName);
        cellObject.transform.SetParent(parent);

        OrigamiFoldCell cell = cellObject.AddComponent<OrigamiFoldCell>();
        cell.gridPosition = gridPosition;
        cell.initialGridPosition = gridPosition;
        cellObject.transform.localPosition = board.GridToLocalPosition(gridPosition);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(cellObject.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(cellVisualSize, cellVisualSize, 1f);

        Collider visualCollider = visual.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = 10;

        CreateCellLabel(objectName, cellObject.transform);

        return cell;
    }

    private static void CreateCellLabel(string textValue, Transform parent)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent);
        labelObject.transform.localPosition = new Vector3(0f, -0.31f, -0.02f);

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = textValue;
        text.characterSize = 0.06f;
        text.fontSize = 18;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;

        Renderer renderer = labelObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 20;
        }
    }

    private static void CreateGridGuides(Vector2 boardOrigin, float cellSize)
    {
        GameObject guidesRoot = new GameObject("ORIGAMI_GRID_GUIDES");
        Color gridColor = new Color(1f, 1f, 1f, 0.28f);
        Color regionColor = new Color(1f, 0.9f, 0.18f, 0.45f);

        for (int i = 0; i <= 4; i++)
        {
            float x = boardOrigin.x + (i * cellSize) - (cellSize * 0.5f);
            CreateGuideLine(
                $"Guide_Vertical_{i}",
                new Vector3(x, 0f, 0.1f),
                new Vector3(0.025f, 1.18f, 1f),
                gridColor,
                guidesRoot.transform);
        }

        Vector3 collapsibleCenter = GridToLocalPosition(boardOrigin, cellSize, new Vector2Int(1, 0));
        CreateGuideLine(
            "Guide_Collapsible_Left",
            new Vector3(collapsibleCenter.x - 0.5f, collapsibleCenter.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Right",
            new Vector3(collapsibleCenter.x + 0.5f, collapsibleCenter.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Top",
            new Vector3(collapsibleCenter.x, collapsibleCenter.y + 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Bottom",
            new Vector3(collapsibleCenter.x, collapsibleCenter.y - 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            guidesRoot.transform);
    }

    private static void CreateVerticalGridGuides(Vector2 boardOrigin, float cellSize)
    {
        GameObject guidesRoot = new GameObject("ORIGAMI_GRID_GUIDES");
        Color gridColor = new Color(1f, 1f, 1f, 0.28f);
        Color regionColor = new Color(1f, 0.9f, 0.18f, 0.45f);

        float leftX = boardOrigin.x - cellSize * 0.5f;
        float rightX = boardOrigin.x + cellSize * 0.5f;
        float columnCenterY = boardOrigin.y + cellSize * 1.5f;

        CreateGuideLine(
            "Guide_Column_Left",
            new Vector3(leftX, columnCenterY, 0.1f),
            new Vector3(0.025f, 4.18f, 1f),
            gridColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Column_Right",
            new Vector3(rightX, columnCenterY, 0.1f),
            new Vector3(0.025f, 4.18f, 1f),
            gridColor,
            guidesRoot.transform);

        for (int i = 0; i <= 4; i++)
        {
            float y = boardOrigin.y + (i * cellSize) - (cellSize * 0.5f);
            CreateGuideLine(
                $"Guide_Horizontal_{i}",
                new Vector3(boardOrigin.x, y, 0.1f),
                new Vector3(1.18f, 0.025f, 1f),
                gridColor,
                guidesRoot.transform);
        }

        Vector3 collapsibleCenter = GridToLocalPosition(boardOrigin, cellSize, new Vector2Int(0, 1));
        CreateGuideLine(
            "Guide_Collapsible_Left",
            new Vector3(collapsibleCenter.x - 0.5f, collapsibleCenter.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Right",
            new Vector3(collapsibleCenter.x + 0.5f, collapsibleCenter.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Top",
            new Vector3(collapsibleCenter.x, collapsibleCenter.y + 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Bottom",
            new Vector3(collapsibleCenter.x, collapsibleCenter.y - 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            guidesRoot.transform);
    }

    private static void CreateGuideLine(
        string objectName,
        Vector3 position,
        Vector3 scale,
        Color color,
        Transform parent)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = objectName;
        line.transform.SetParent(parent);
        line.transform.position = position;
        line.transform.localScale = scale;

        Collider collider = line.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = line.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = 5;
    }

    private static Vector3 GridToLocalPosition(Vector2 origin, float cellSize, Vector2Int gridPosition)
    {
        return new Vector3(
            origin.x + gridPosition.x * cellSize,
            origin.y + gridPosition.y * cellSize,
            0f);
    }

    private static void ConfigureMergedClickAction(
        OrigamiFoldPoint mergedPoint,
        OrigamiFoldMoveAction action,
        Camera camera)
    {
        if (mergedPoint == null)
        {
            return;
        }

        OrigamiFoldClickAction clickAction = mergedPoint.gameObject
            .AddComponent<OrigamiFoldClickAction>();

        clickAction.targetCamera = camera;
        clickAction.targetMoveAction = action;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = mergedPoint.pointId;
    }

    private static GameObject[] ToGameObjects(OrigamiFoldPoint[] points)
    {
        if (points == null)
        {
            return new GameObject[0];
        }

        int count = 0;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                count++;
            }
        }

        GameObject[] objects = new GameObject[count];
        int index = 0;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                objects[index] = points[i].gameObject;
                index++;
            }
        }

        return objects;
    }

    private static GameObject[] Combine(GameObject first, GameObject[] rest)
    {
        int restLength = rest == null ? 0 : rest.Length;
        int extra = first == null ? 0 : 1;
        GameObject[] objects = new GameObject[restLength + extra];
        int index = 0;

        if (first != null)
        {
            objects[index] = first;
            index++;
        }

        for (int i = 0; i < restLength; i++)
        {
            objects[index] = rest[i];
            index++;
        }

        return objects;
    }

    private static void DeleteIfExists(string objectName)
    {
        GameObject existing = FindSceneObject(objectName);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void SetLegacyContainersActive(bool active)
    {
        string[] legacyContainers =
        {
            "MANAGERS",
            "BOOK",
            "PATH_GRAPH",
            "ACTORS",
            "INTERACTABLES",
            "UI",
            "FOLD_ACTIONS"
        };

        for (int i = 0; i < legacyContainers.Length; i++)
        {
            GameObject legacy = FindSceneObject(legacyContainers[i]);

            if (legacy != null)
            {
                legacy.SetActive(active);
            }
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate.name == objectName
                && candidate.scene == activeScene
                && candidate.hideFlags == HideFlags.None)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Camera FindMainCameraOrCreate(WorkbenchStep step)
    {
        Camera camera = Camera.main;

        if (camera != null)
        {
            ConfigureCamera(camera, step);
            return camera;
        }

        GameObject existing = GameObject.Find("Main Camera");

        if (existing != null)
        {
            camera = existing.GetComponent<Camera>();

            if (camera != null)
            {
                ConfigureCamera(camera, step);
                return camera;
            }
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        camera = cameraObject.AddComponent<Camera>();
        ConfigureCamera(camera, step);
        cameraObject.AddComponent<AudioListener>();

        return camera;
    }

    private static void ConfigureCamera(Camera camera, WorkbenchStep step)
    {
        if (step == WorkbenchStep.Step3_6)
        {
            camera.transform.position = new Vector3(0f, 0.1f, -10f);
            camera.orthographicSize = 4.4f;
        }
        else if (step == WorkbenchStep.Step3_5)
        {
            camera.transform.position = new Vector3(0f, 0.15f, -10f);
            camera.orthographicSize = 4.3f;
        }
        else if (step == WorkbenchStep.Step3_4)
        {
            camera.transform.position = new Vector3(-1f, 0.1f, -10f);
            camera.orthographicSize = 4.2f;
        }
        else if (step == WorkbenchStep.Step3_3)
        {
            camera.transform.position = new Vector3(0f, 0.15f, -10f);
            camera.orthographicSize = 3.7f;
        }
        else if (step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2)
        {
            camera.transform.position = new Vector3(0f, 0.2f, -10f);
            camera.orthographicSize = 3.1f;
        }
        else
        {
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographicSize = 3.6f;
        }

        camera.orthographic = true;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateWorkbenchVisuals(Transform parent, WorkbenchStep step)
    {
        if (step == WorkbenchStep.Step1)
        {
            CreateWorkbenchTile(
                "WorkbenchTile_TopLeft",
                new Vector3(-0.5f, 0.5f, 0.2f),
                new Color(0.18f, 0.28f, 0.42f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            CreateWorkbenchTile(
                "WorkbenchTile_TopRight",
                new Vector3(0.5f, 0.5f, 0.2f),
                new Color(0.25f, 0.38f, 0.28f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            CreateWorkbenchTile(
                "WorkbenchTile_BottomLeft",
                new Vector3(-0.5f, -0.5f, 0.2f),
                new Color(0.42f, 0.30f, 0.18f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            CreateWorkbenchTile(
                "WorkbenchTile_BottomRight",
                new Vector3(0.5f, -0.5f, 0.2f),
                new Color(0.36f, 0.24f, 0.40f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            return;
        }

        if (step == WorkbenchStep.Step3_6)
        {
            CreateWorkbenchTile(
                "WorkbenchPlate_WholeStripMap",
                new Vector3(0f, 0f, 0.35f),
                new Color(0.12f, 0.14f, 0.16f),
                new Vector3(5.35f, 4.35f, 1f),
                parent);

            return;
        }

        if (step == WorkbenchStep.Step3_5)
        {
            CreateWorkbenchTile(
                "WorkbenchPlate_UnifiedMap",
                new Vector3(0f, 0f, 0.35f),
                new Color(0.12f, 0.14f, 0.16f),
                new Vector3(5.25f, 4.25f, 1f),
                parent);

            return;
        }

        if (step == WorkbenchStep.Step3_4)
        {
            CreateWorkbenchTile(
                "WorkbenchPlate_HorizontalSqueeze",
                new Vector3(-3f, 0f, 0.35f),
                new Color(0.12f, 0.14f, 0.16f),
                new Vector3(4.25f, 1.15f, 1f),
                parent);

            CreateWorkbenchTile(
                "WorkbenchPlate_VerticalSqueeze",
                new Vector3(2.2f, 0f, 0.35f),
                new Color(0.14f, 0.13f, 0.17f),
                new Vector3(1.15f, 4.25f, 1f),
                parent);

            return;
        }

        CreateWorkbenchTile(
            "WorkbenchPlate",
            step == WorkbenchStep.Step3_3
                ? new Vector3(0f, 0f, 0.35f)
                : step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2
                ? new Vector3(0f, 0f, 0.35f)
                : new Vector3(0f, -0.4f, 0.35f),
            new Color(0.12f, 0.14f, 0.16f),
            step == WorkbenchStep.Step3_3
                ? new Vector3(1.15f, 4.25f, 1f)
                : step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2
                ? new Vector3(4.25f, 1.15f, 1f)
                : new Vector3(4.3f, 1.25f, 1f),
            parent);
    }

    private static void CreateWorkbenchTile(
        string objectName,
        Vector3 position,
        Color color,
        Vector3 scale,
        Transform parent)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tile.name = objectName;
        tile.transform.SetParent(parent);
        tile.transform.position = position;
        tile.transform.localScale = scale;

        Collider collider = tile.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = tile.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = 0;
    }

    private static OrigamiFoldPoint CreateFoldPoint(
        string objectName,
        Vector3 position,
        Color color,
        Vector3 visualScale,
        float colliderRadius,
        int sortingOrder,
        Transform parent)
    {
        GameObject pointObject = new GameObject(objectName);
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;
        pointObject.transform.localScale = Vector3.one;

        CircleCollider2D collider = pointObject.AddComponent<CircleCollider2D>();
        collider.radius = colliderRadius;
        collider.isTrigger = true;

        OrigamiFoldPoint point = pointObject.AddComponent<OrigamiFoldPoint>();
        point.pointId = objectName;
        point.normalColor = color;
        point.highlightColor = Color.yellow;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(pointObject.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = visualScale;

        Collider visualCollider = visual.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = sortingOrder;
        point.visualRenderer = renderer;

        return point;
    }

    private static OrigamiFoldLink CreateFoldLink(
        string objectName,
        OrigamiFoldPoint a,
        OrigamiFoldPoint b,
        GameObject executeIndicator,
        Transform parent)
    {
        GameObject linkObject = new GameObject(objectName);
        linkObject.transform.SetParent(parent);

        OrigamiFoldLink link = linkObject.AddComponent<OrigamiFoldLink>();
        link.pointA = a;
        link.pointB = b;
        link.bidirectional = true;
        link.enableOnExecute = new[] { executeIndicator };
        link.disableOnExecute = new GameObject[0];

        return link;
    }

    private static void ConfigureFoldMoveLink(
        OrigamiFoldLink link,
        OrigamiFoldMoveAction action,
        bool activeStateOnExecute)
    {
        if (link == null)
        {
            return;
        }

        link.bidirectional = false;
        link.targetMoveAction = action;
        link.activeStateOnExecute = activeStateOnExecute;
    }

    private static GameObject CreateExecuteIndicator(Transform parent)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.name = "ExecuteIndicator";
        indicator.transform.SetParent(parent);
        indicator.transform.position = new Vector3(0f, -2.1f, 0f);
        indicator.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

        Collider collider = indicator.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = indicator.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(Color.green);
        renderer.sortingOrder = 40;
        indicator.SetActive(false);

        return indicator;
    }

    private static void CreateInstructionText(Transform parent, WorkbenchStep step)
    {
        GameObject textObject = new GameObject("InstructionText");
        textObject.transform.SetParent(parent);
        textObject.transform.position = step == WorkbenchStep.Step3_6
            ? new Vector3(-4f, 4.12f, 0f)
            : step == WorkbenchStep.Step3_5
            ? new Vector3(-3.55f, 4.05f, 0f)
            : step == WorkbenchStep.Step3_4
            ? new Vector3(-5.55f, 3.9f, 0f)
            : step == WorkbenchStep.Step3_3
            ? new Vector3(-3.3f, 3.35f, 0f)
            : step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2
            ? new Vector3(-2.65f, 2.65f, 0f)
            : new Vector3(-2.8f, 2.85f, 0f);

        TextMesh text = textObject.AddComponent<TextMesh>();

        if (step == WorkbenchStep.Step3_6)
        {
            text.text = "Whole-strip fold: vertical drag collapses row, horizontal drag collapses column.";
        }
        else if (step == WorkbenchStep.Step3_5)
        {
            text.text = "Unified map: fold H and V zones independently. Cyan points unfold.";
        }
        else if (step == WorkbenchStep.Step3_4)
        {
            text.text = "Symmetric squeeze: drag white edge points. Click cyan points to unfold.";
        }
        else if (step == WorkbenchStep.Step3_3)
        {
            text.text = "Drag left or right edge vertically to fold. Click cyan point to unfold.";
        }
        else if (step == WorkbenchStep.Step3_2)
        {
            text.text = "Drag top or bottom edge to fold. Click cyan point to unfold.";
        }
        else if (step == WorkbenchStep.Step3_1)
        {
            text.text = "Drag TL -> TR to fold. Click cyan point to unfold.";
        }
        else if (step == WorkbenchStep.Step3)
        {
            text.text = "Drag TL -> TR to fold. Click merged point to unfold.";
        }
        else if (step == WorkbenchStep.Step2)
        {
            text.text = "Drag TopLeft -> TopRight to fold. Drag TopRight -> TopLeft to unfold.";
        }
        else
        {
            text.text = "Drag neighboring points. Diagonals do not work.";
        }

        text.characterSize = step == WorkbenchStep.Step3_6
            ? 0.10f
            : step == WorkbenchStep.Step3_5
            ? 0.105f
            : step == WorkbenchStep.Step3_4
            ? 0.105f
            : step == WorkbenchStep.Step3_3
            ? 0.095f
            : step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2 ? 0.11f : 0.13f;
        text.fontSize = step == WorkbenchStep.Step3_6
            ? 22
            : step == WorkbenchStep.Step3_5
            ? 23
            : step == WorkbenchStep.Step3_4
            ? 23
            : step == WorkbenchStep.Step3_3
            ? 22
            : step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2 ? 24 : 28;
        text.anchor = TextAnchor.UpperLeft;
        text.color = Color.white;

        Renderer renderer = textObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 50;
        }
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = FindVisualShader();

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static Shader FindVisualShader()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Unlit/Color");
    }
}
