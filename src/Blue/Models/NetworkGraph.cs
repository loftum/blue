namespace Blue.Models;

public class NetworkGraph
{
    public List<GraphNode> Nodes { get; init; } = [];
    public List<GraphEdge> Edges { get; init; } = [];
}

public class GraphNode
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? Shape { get; init; }
    public string? Group { get; set; }
    public string? Title { get; set; }
}

public class GraphEdge
{
    public required string Id { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public GraphEdgeArrows Arrows { get; init; } = new();
    public string? Color { get; set; }
    public string? Title { get; set; }
}

public class GraphEdgeArrows
{
    public ArrowSettings From { get; init; } = new();
    public ArrowSettings Middle { get; init; } = new();
    public ArrowSettings To { get; init; } = new();
}

public class ArrowSettings
{
    public bool Enabled { get; set; }
    public ArrowType Type { get; set; } 
}

public enum ArrowType
{
    arrow,
    bar,
    circle,
    image
}
