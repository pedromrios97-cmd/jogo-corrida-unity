using System.Collections.Generic;
using UnityEngine;

public class RoadNode : MonoBehaviour
{
    [SerializeField] private List<RoadNode> neighbors = new List<RoadNode>();
    [SerializeField] private List<float> neighborWidths = new List<float>();

    public IReadOnlyList<RoadNode> Neighbors => neighbors;
    public Vector3 Position => transform.position;

    public bool IsConnectedTo(RoadNode other) => neighbors.Contains(other);

    /// <summary>Largura da conexão com <paramref name="other"/> (1 = padrão).</summary>
    public float GetWidthTo(RoadNode other)
    {
        int i = neighbors.IndexOf(other);
        if (i >= 0 && i < neighborWidths.Count) return neighborWidths[i];
        return 1f;
    }

    /// <summary>Define a largura do segmento nos dois sentidos.</summary>
    public void SetWidthTo(RoadNode other, float width)
    {
        width = Mathf.Max(0.01f, width);
        SetOneWay(this, other, width);
        if (other != null) SetOneWay(other, this, width);
    }

    /// <summary>Liga os dois nós nos dois sentidos (mão dupla), largura padrão.</summary>
    public void Connect(RoadNode other)
    {
        if (other == null || other == this) return;
        EnsureAligned();
        other.EnsureAligned();
        if (!neighbors.Contains(other)) { neighbors.Add(other); neighborWidths.Add(1f); }
        if (!other.neighbors.Contains(this)) { other.neighbors.Add(this); other.neighborWidths.Add(1f); }
    }

    /// <summary>Desfaz a conexão nos dois sentidos.</summary>
    public void Disconnect(RoadNode other)
    {
        if (other == null) return;
        EnsureAligned();
        other.EnsureAligned();
        int i = neighbors.IndexOf(other);
        if (i >= 0) { neighbors.RemoveAt(i); neighborWidths.RemoveAt(i); }
        int j = other.neighbors.IndexOf(this);
        if (j >= 0) { other.neighbors.RemoveAt(j); other.neighborWidths.RemoveAt(j); }
    }

    /// <summary>Remove vizinhos nulos (ex.: um nó que foi apagado).</summary>
    public void CleanUp()
    {
        EnsureAligned();
        for (int i = neighbors.Count - 1; i >= 0; i--)
            if (neighbors[i] == null) { neighbors.RemoveAt(i); neighborWidths.RemoveAt(i); }
    }

    // Mantém neighborWidths do mesmo tamanho de neighbors (sobra vira 1).
    private void EnsureAligned()
    {
        while (neighborWidths.Count < neighbors.Count) neighborWidths.Add(1f);
        while (neighborWidths.Count > neighbors.Count) neighborWidths.RemoveAt(neighborWidths.Count - 1);
    }

    private static void SetOneWay(RoadNode from, RoadNode to, float width)
    {
        from.EnsureAligned();
        int i = from.neighbors.IndexOf(to);
        if (i >= 0) from.neighborWidths[i] = width;
    }
}
