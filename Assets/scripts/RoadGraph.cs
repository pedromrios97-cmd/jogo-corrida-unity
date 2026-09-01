using System.Collections.Generic;
using UnityEngine;

public class RoadGraph : MonoBehaviour
{
    [SerializeField] private List<RoadNode> nodes = new List<RoadNode>();
    [Tooltip("Tamanho das esferas dos nós no Gizmo (ajuste conforme a escala da cidade).")]
    [SerializeField] private float gizmoRadius = 0.5f;

    public IReadOnlyList<RoadNode> Nodes => nodes;

    /// <summary>Recolhe todos os RoadNode filhos. Usado pela ferramenta de edição.</summary>
    public void CollectNodes()
    {
        nodes.Clear();
        nodes.AddRange(GetComponentsInChildren<RoadNode>());
    }

    private void Awake()
    {
        if (nodes == null || nodes.Count == 0)
            CollectNodes();
        RepairConnections(); // garante mão dupla em runtime, independente de como foi salvo
    }

    /// <summary>
    /// Garante que toda conexão seja de mão dupla (se A liga a B, B passa a
    /// ligar a A) e remove referências nulas. Importante para o pathfinding,
    /// que só anda pelos vizinhos de cada nó.
    /// </summary>
    public void RepairConnections()
    {
        foreach (var node in nodes)
        {
            if (node == null) continue;
            node.CleanUp();
            // Cópia da lista porque Connect altera os vizinhos durante o laço.
            foreach (var neighbor in new List<RoadNode>(node.Neighbors))
                node.Connect(neighbor); // simétrico: adiciona o lado que faltava
        }
    }

    /// <summary>Nó mais próximo de uma posição no mundo (ex.: onde o carro está).</summary>
    public RoadNode GetNearestNode(Vector3 position)
    {
        RoadNode best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (var node in nodes)
        {
            if (node == null) continue;
            float d = (node.Position - position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = node;
            }
        }
        return best;
    }

    /// <summary>
    /// Menor caminho de <paramref name="from"/> até <paramref name="to"/> (Dijkstra).
    /// Retorna a lista de nós na ordem a percorrer, ou null se não houver caminho.
    /// </summary>
    public List<RoadNode> FindPath(RoadNode from, RoadNode to)
    {
        if (from == null || to == null) return null;
        if (from == to) return new List<RoadNode> { from };

        var dist = new Dictionary<RoadNode, float>();
        var prev = new Dictionary<RoadNode, RoadNode>();
        var unvisited = new List<RoadNode>();

        foreach (var node in nodes)
        {
            if (node == null) continue;
            dist[node] = float.PositiveInfinity;
            unvisited.Add(node);
        }
        if (!dist.ContainsKey(from) || !dist.ContainsKey(to)) return null;
        dist[from] = 0f;

        while (unvisited.Count > 0)
        {
            // Pega o não-visitado mais próximo (busca linear: simples e suficiente).
            RoadNode current = null;
            float best = float.PositiveInfinity;
            foreach (var node in unvisited)
            {
                if (dist[node] < best)
                {
                    best = dist[node];
                    current = node;
                }
            }

            if (current == null) break;      // o resto é inatingível
            unvisited.Remove(current);
            if (current == to) break;

            foreach (var neighbor in current.Neighbors)
            {
                if (neighbor == null || !dist.ContainsKey(neighbor)) continue;
                float alt = dist[current] + Vector3.Distance(current.Position, neighbor.Position);
                if (alt < dist[neighbor])
                {
                    dist[neighbor] = alt;
                    prev[neighbor] = current;
                }
            }
        }

        // Reconstrói o caminho de trás para frente.
        var path = new List<RoadNode>();
        var step = to;
        while (step != null)
        {
            path.Add(step);
            if (step == from) break;
            if (!prev.TryGetValue(step, out step)) break;
        }

        if (path.Count == 0 || path[path.Count - 1] != from)
            return null;   // não chegou até a origem: sem caminho

        path.Reverse();
        return path;
    }

    private void OnDrawGizmos()
    {
        if (nodes == null) return;

        foreach (var node in nodes)
        {
            if (node == null) continue;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
            Gizmos.DrawSphere(node.Position, gizmoRadius);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            foreach (var neighbor in node.Neighbors)
                if (neighbor != null)
                    Gizmos.DrawLine(node.Position, neighbor.Position);
        }
    }
}
