namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    private Change? _sculptStroke;

    private readonly HashSet<(int X, int Z)>
        _sculptHeightsTouched = [];

    private readonly HashSet<(int X, int Z)>
        _sculptHolesTouched = [];

    public void BeginSculptStroke()
    {
        EndSculptStroke();

        _sculptStroke = new Change();

        _sculptHeightsTouched.Clear();
        _sculptHolesTouched.Clear();
    }

    public void EndSculptStroke()
    {
        if (_sculptStroke == null)
        {
            return;
        }

        var change = _sculptStroke;
        _sculptStroke = null;

        if (change.Heights.Count > 0 || change.Holes.Count > 0)
        {
            if (_undo.Count >= 64)
            {
                _undo.RemoveAt(0);
            }

            _undo.Add(change);
        }

        _sculptHeightsTouched.Clear();
        _sculptHolesTouched.Clear();
    }
}
