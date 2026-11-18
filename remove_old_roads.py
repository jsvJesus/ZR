from pathlib import Path
import subprocess

root = Path(__file__).resolve().parent

def edit(name, operation):
    path = root / name
    if not path.exists():
        raise FileNotFoundError(name)
    original = path.read_text(encoding="utf-8-sig")
    changed = operation(original)
    if changed == original:
        raise RuntimeError(f"Не внесены изменения: {name}")
    path.write_text(changed, encoding="utf-8")

def replace(text, old, new=""):
    if old not in text:
        raise RuntimeError(f"Не найден участок:\n{old[:150]}")
    return text.replace(old, new)

def cut(text, start, end):
    first = text.find(start)
    if first < 0:
        raise RuntimeError(f"Не найдено начало: {start}")
    last = text.find(end, first + len(start))
    if last < 0:
        raise RuntimeError(f"Не найден конец: {end}")
    return text[:first] + text[last:]

def lines_without(text, fragments):
    return "".join(
        line for line in text.splitlines(keepends=True)
        if not any(fragment in line for fragment in fragments)
    )

def vegetation_collision(text):
    text = replace(text, "using ZoneReborn.World.Roads;\n")
    text = replace(
        text,
        "    private const int MaximumRoadCollisionTriangles = 2_000_000;\n"
    )
    text = cut(
        text,
        "        var clearance = RoadClearance.Build(",
        "        foreach (var entry"
    )
    text = cut(
        text,
        "            if (entry.Generated)\n",
        "            if (!VegetationCategory.HasBarkCollision"
    )
    text = cut(
        text,
        "        int collisionTriangles = 0;",
        "        return new SceneData("
    )
    return text

edit(
    "src/World/Vegetation/VegetationCollision.cs",
    vegetation_collision
)

def vegetation_scatter(text):
    text = replace(text, "using ZoneReborn.World.Roads;\n")
    text = cut(
        text,
        "        float maximumRoadClearance = rules",
        "        foreach (var rule in rules)"
    )
    text = cut(
        text,
        "                if (roads.Intersects(",
        "                if (IntersectsObject("
    )
    return text

edit(
    "src/World/Vegetation/VegetationScatterGenerator.cs",
    vegetation_scatter
)

def level_workspace(text):
    text = replace(text, "using ZoneReborn.World.Roads;\n")
    text = lines_without(text, [
        "var roads = await RoadStorage.LoadAsync",
        "AttachRoadNetwork(roads,"
    ])
    text = replace(
        text,
        "level = level with { Roads = roads, Vegetation = vegetation, Objects = objects };",
        "level = level with { Vegetation = vegetation, Objects = objects };"
    )
    return text

edit("src/Editor/Levels/LevelWorkspace.cs", level_workspace)

def tree_renderer(text):
    text = replace(text, "using ZoneReborn.World.Roads;\n")
    text = cut(
        text,
        "        private readonly RoadNetwork _roadNetwork;",
        "        private readonly Dictionary<string, ModelResources>"
    )
    text = replace(
        text,
        "            TerrainRenderer terrain,\n            RoadNetwork roads)",
        "            TerrainRenderer terrain)"
    )
    text = lines_without(text, [
        "_roadNetwork = roads;",
        "RefreshRoadClearance();"
    ])
    text = cut(
        text,
        "                if (entry.Generated)\n",
        "                var world = World(tree, _terrain);"
    )
    text = cut(
        text,
        "        private void RefreshRoadClearance()",
        "        private TerrainTexture Texture("
    )
    return text

edit(
    "src/Editor/Viewport/Vegetation/TreeRenderer.cs",
    tree_renderer
)

def renderer(text):
    text = replace(text, "using Editor.Viewport.Roads;\n")
    text = replace(text, "using ZoneReborn.World.Roads;\n")
    text = lines_without(text, [
        "private RoadRenderer? _roads;",
        "_roads?.Dispose();",
        "_roads = roads;"
    ])
    text = cut(
        text,
        "            RoadRenderer roads;",
        "            SceneRenderer? objects = null;"
    )
    text = replace(
        text,
        "            SceneRenderer? objects = null;",
        """            TreeRenderer trees;
            try
            {
                trees = new TreeRenderer(
                    _device,
                    _context,
                    assets,
                    level.Vegetation,
                    terrain);
            }
            catch
            {
                terrain.Dispose();
                throw;
            }

            SceneRenderer? objects = null;"""
    )
    text = replace(
        text,
        "catch { trees.Dispose(); roads.Dispose(); terrain.Dispose(); throw; }",
        "catch { trees.Dispose(); terrain.Dispose(); throw; }"
    )
    text = cut(
        text,
        "        public void SelectRoad(RoadDefinition? road, int nodeIndex)",
        "        public void SetCharacter("
    )
    text = cut(
        text,
        "            _roads?.Render(",
        "            _objects?.Render("
    )
    return text

edit("src/Editor/Viewport/Dx11Renderer.cs", renderer)

def renderer_environment(text):
    return cut(
        text,
        "            _roads?.AdvanceWeatherWetness(",
        "        }\n\n        private Vector3 UpdateEnvironmentLighting("
    )

edit(
    "src/Editor/Viewport/Dx11Renderer.Environment.cs",
    renderer_environment
)

def viewport(text):
    text = lines_without(text, [
        "CancelRoadDrag();",
        "SelectRoad(null);",
        "PickRoadSegment(ReadX(",
        "if (MoveRoadDrag("
    ])
    text = cut(
        text,
        "                    if (IsRoadDragging)\n",
        "                    if (IsTreeDragging)"
    )
    text = replace(
        text,
        "                    if (IsRoadDragging) { CancelRoadDrag(); return nint.Zero; }\n"
    ) if "                    if (IsRoadDragging) { CancelRoadDrag(); return nint.Zero; }\n" in text else text
    text = replace(
        text,
        "&& !IsRoadDragging",
        ""
    )
    text = replace(
        text,
        " || IsRoadDragging",
        ""
    )
    text = replace(
        text,
        "HandleRoadKey((int)wParam, lParam) || ",
        ""
    )
    return text

edit("src/Editor/Viewport/DxViewportHost.cs", viewport)

def viewport_play(text):
    return lines_without(text, ["CancelRoadDrag();"])

edit("src/Editor/Viewport/DxViewportHost.Play.cs", viewport_play)

def main_window(text):
    text = lines_without(text, [
        "RoadSegmentsPanel.Attach(",
        "Viewport.RoadPicked +=",
        "RoadSegmentsPanel.BusyChanged +=",
        "await RoadSegmentsPanel.SavePendingAsync();"
    ])
    text = replace(
        text,
        "(!RoadSegmentsPanel.HasPendingChanges &&\n",
        "(" + "\n"
    )
    return text

edit("src/Editor/MainWindow.axaml.cs", main_window)

def open_level(text):
    text = lines_without(text, [
        "await RoadSegmentsPanel.SavePendingAsync();",
        "RoadSegmentsPanel.Bind("
    ])
    text = replace(text, "RoadSegmentsPanel.IsSaving || ", "")
    text = replace(
        text,
        """            StatusText.Text = $"Открыт {level.Manifest.Name} · дорог: {level.Roads.Roads.Count}"
                + $" · моделей: {level.Objects?.Instances.Count ?? 0}"
                + (level.Roads.Warnings.Count > 0 ? $" · замечаний по дорогам: {level.Roads.Warnings.Count}" : "")""",
        """            StatusText.Text = $"Открыт {level.Manifest.Name} · моделей: {level.Objects?.Instances.Count ?? 0}" """
    )
    return text

edit("src/Editor/Levels/OpenLevelCommands.cs", open_level)

def save_level(text):
    text = replace(text, "RoadSegmentsPanel.IsSaving || ", "")
    text = lines_without(
        text, ["await RoadSegmentsPanel.SavePendingAsync();"]
    )
    text = replace(
        text,
        "Terrain, камера, дороги, растительность и объекты сохранены.",
        "Terrain, камера, растительность и объекты сохранены."
    )
    return text

edit("src/Editor/Levels/SaveLevelCommands.cs", save_level)

def editor_modes(text):
    text = replace(text, "            bool showRoadSegments = false,\n")
    text = lines_without(text, [
        "RoadSegmentsPanel.IsVisible =",
        "RoadSegmentsPanel.RefreshHighlight();",
        "RoadSegmentsPanel.IsSaving ||"
    ])
    text = replace(
        text,
        '                : showRoadSegments ? "roads"\n',
        ""
    )
    text = cut(
        text,
        "        private void RoadSegments_Click(",
        "        private void TerrainMaterials_Click("
    )
    return text

edit("src/Editor/Panels/EditorModes.cs", editor_modes)

def workspace_layout(text):
    text = lines_without(text, [
        "AddBrowser(RoadSegmentsPanel,",
        'AddWorkspace("roads",'
    ])
    text = replace(text, "!RoadSegmentsPanel.IsSaving && ", "")
    return text

edit("src/Editor/Panels/WorkspaceLayout.cs", workspace_layout)

def level_commands(text):
    text = replace(text, "RoadSegmentsPanel.IsSaving || ", "")
    return lines_without(text, [
        "RoadSegmentsPanel.IsEnabled =",
        "RoadSegmentsPanel.RefreshHighlight();"
    ])

edit("src/Editor/Levels/LevelCommands.cs", level_commands)

def play_commands(text):
    return replace(text, "RoadSegmentsPanel.IsSaving || ", "")

edit("src/Editor/Play/PlayCommands.cs", play_commands)

def window_markup(text):
    text = lines_without(text, [
        'xmlns:roadSegments=',
        'Click="RoadSegments_Click"',
        '<roadSegments:RoadSegmentsPanel '
    ])
    return replace(
        text,
        'Header="Ландшафт и дороги"',
        'Header="Ландшафт"'
    )

edit("src/Editor/MainWindow.axaml", window_markup)

old_files = [
    "src/World/Roads/RoadAssetCatalog.cs",
    "src/World/Roads/RoadClearance.cs",
    "src/World/Roads/RoadMesh.cs",
    "src/World/Roads/RoadNetwork.cs",
    "src/World/Roads/RoadSpline.cs",
    "src/World/Roads/RoadStorage.cs",
    "src/World/Terrain/TerrainRoadModifiers.cs",
    "src/World/Terrain/TerrainDocument.Roads.cs",
    "src/World/Terrain/TerrainSurface.RoadWalking.cs",
    "src/Editor/RoadSegments/RoadSegmentsPanel.cs",
    "src/Editor/RoadSegments/RoadSegmentsPanel.Commands.cs",
    "src/Editor/Viewport/DxViewportHost.Roads.cs",
    "src/Editor/Viewport/Roads/RoadRenderer.cs",
    "src/Editor/Viewport/Roads/RoadRenderer.Materials.cs",
    "src/Editor/Viewport/Roads/RoadRenderer.Weather.cs"
]

for name in old_files:
    path = root / name
    if path.exists():
        path.unlink()
        print(f"Удалён: {name}")

print("Очистка старой системы Roads выполнена.")
print("Следующий шаг: проверить оставшиеся ссылки и сборку.")