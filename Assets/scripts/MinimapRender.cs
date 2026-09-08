using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapRenderer : MaskableGraphic
{
    [Header("Dados")]
    [SerializeField] private RoadGraph graph;
    [Tooltip("O que fica no centro do mapa (normalmente o carro do jogador).")]
    [SerializeField] private Transform target;

    [Header("Aparência")]
    [Tooltip("Pixels por unidade de mundo (zoom do mapa).")]
    public float zoom = 2f;
    [SerializeField] private float lineWidth = 23f;
    [Tooltip("Gira o mapa com o alvo (frente do carro aponta para cima).")]
    [SerializeField] private bool rotateWithTarget = true;

    // Base de coordenadas usada para converter mundo -> mapa.
    private Vector3 _center;
    private Vector3 _right = Vector3.right;
    private Vector3 _forward = Vector3.forward;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (graph == null) return;

        UpdateBasis();
        _drawnEdges.Clear();

        // 1) Segmentos: cada aresta tem sua própria largura (uniforme).
        foreach (var node in graph.Nodes)
        {
            if (node == null) continue;
            Vector2 a = WorldToMap(node.Position);

            foreach (var neighbor in node.Neighbors)
            {
                if (neighbor == null) continue;
                // Desenha cada aresta uma vez, mesmo que a conexão exista só de um lado.
                if (!_drawnEdges.Add(EdgeKey(node, neighbor))) continue;

                float half = EdgeHalfWidth(node, neighbor);
                AddSegment(vh, a, WorldToMap(neighbor.Position), half, half, color);
            }
        }

        // 2) Disco em cada nó para arredondar/preencher juntas.
        foreach (var node in graph.Nodes)
        {
            if (node == null) continue;

            int degree = 0;
            float minW = float.MaxValue;
            float maxW = 0f;
            foreach (var neighbor in node.Neighbors)
            {
                if (neighbor == null) continue;
                degree++;
                float w = Mathf.Max(node.GetWidthTo(neighbor), neighbor.GetWidthTo(node));
                if (w < minW) minW = w;
                if (w > maxW) maxW = w;
            }
            if (degree == 0) continue;

            // Ponta/curva (1-2 ruas): cap do tamanho da rua (arredonda).
            // Interseção (3+ ruas): só o menor, para preencher o centro sem
            // estourar para fora das ruas mais estreitas.
            float w2 = degree >= 3 ? minW : maxW;
            AddDisc(vh, WorldToMap(node.Position), lineWidth * w2 * 0.5f, color);
        }
    }

    // Meia-largura de um segmento (usa o maior lado, caso as pontas difiram).
    private float EdgeHalfWidth(RoadNode a, RoadNode b)
    {
        float w = Mathf.Max(a.GetWidthTo(b), b.GetWidthTo(a));
        return lineWidth * w * 0.5f;
    }

    // Guarda as arestas já desenhadas (chave independente da ordem A-B / B-A).
    private readonly HashSet<long> _drawnEdges = new HashSet<long>();

    private static long EdgeKey(RoadNode a, RoadNode b)
    {
        int ia = a.GetInstanceID();
        int ib = b.GetInstanceID();
        int lo = Mathf.Min(ia, ib);
        int hi = Mathf.Max(ia, ib);
        return ((long)lo << 32) | (uint)hi;
    }

    // Define o centro (alvo) e os eixos do mapa (girando com o alvo, ou fixos).
    private void UpdateBasis()
    {
        if (target != null && rotateWithTarget)
        {
            _center = target.position;

            // Usa só a direção horizontal do carro (X,Z), ignorando Y.
            Vector3 flatForward = target.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 1e-4f)
            {
                _forward = flatForward.normalized;
                // "Direita" derivada do forward + up do mundo: sempre consistente,
                // então rolar/capotar o carro NÃO espelha o mapa. (Se o carro
                // aponta quase reto pra cima/baixo, mantém a última base válida.)
                _right = Vector3.Cross(Vector3.up, _forward);
            }
        }
        else
        {
            _center = target != null ? target.position : Vector3.zero;
            _right = Vector3.right;
            _forward = Vector3.forward;
        }
    }

    // Mundo (3D, plano XZ) -> coordenadas do mapa (2D). Direita = x, frente = y (cima).
    private Vector2 WorldToMap(Vector3 world)
    {
        Vector3 d = world - _center;
        float x = Vector3.Dot(d, _right);
        float y = Vector3.Dot(d, _forward);
        return new Vector2(x, y) * zoom;
    }

    // Segmento grosso (trapézio) de a até b, com meia-largura própria em cada ponta.
    private static void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float halfA, float halfB, Color32 col)
    {
        Vector2 dir = b - a;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();
        Vector2 perp = new Vector2(-dir.y, dir.x);
        Vector2 offsetA = perp * halfA;
        Vector2 offsetB = perp * halfB;

        int start = vh.currentVertCount;
        vh.AddVert(a - offsetA, col, Vector2.zero);
        vh.AddVert(a + offsetA, col, Vector2.zero);
        vh.AddVert(b + offsetB, col, Vector2.zero);
        vh.AddVert(b - offsetB, col, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    // Disco (leque de triângulos) para arredondar juntas e pontas.
    private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color32 col, int segments = 12)
    {
        if (radius <= 0f) return;

        int centerIndex = vh.currentVertCount;
        vh.AddVert(center, col, Vector2.zero);
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vh.AddVert(p, col, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
        {
            int current = centerIndex + 1 + i;
            int next = centerIndex + 1 + (i + 1) % segments;
            vh.AddTriangle(centerIndex, current, next);
        }
    }

    private void Update()
    {
        // Redesenha enquanto joga (o carro se move/gira).
        if (Application.isPlaying && target != null)
            SetVerticesDirty();
    }
}
