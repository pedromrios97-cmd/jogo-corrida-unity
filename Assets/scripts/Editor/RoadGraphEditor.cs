using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadGraph))]
public class RoadGraphEditor : Editor
{
    private RoadNode _selected;
    private RoadNode _edgeA, _edgeB;   // segmento selecionado (para ajustar a largura)
    private bool _editing;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var graph = (RoadGraph)target;

        EditorGUILayout.Space();
        _editing = GUILayout.Toggle(_editing, "Editar mapa na Scene view", "Button");

        if (_editing)
        {
            EditorGUILayout.HelpBox(
                "Shift+Clique na rua: cria um nó (ligado ao selecionado).\n" +
                "Clique num nó: seleciona.\n" +
                "Ctrl+Clique num nó: liga/desliga do selecionado.\n" +
                "Clique no ponto do MEIO de um segmento: seleciona para ajustar a largura.",
                MessageType.Info);
        }

        if (GUILayout.Button("Recolher nós filhos"))
        {
            Undo.RecordObject(graph, "Collect Nodes");
            graph.CollectNodes();
            EditorUtility.SetDirty(graph);
        }

        if (GUILayout.Button("Reparar conexões (mão dupla)"))
        {
            Undo.RegisterFullObjectHierarchyUndo(graph.gameObject, "Repair Connections");
            graph.RepairConnections();
            foreach (var node in graph.Nodes)
                if (node != null) EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(graph);
        }

        // Largura do SEGMENTO selecionado (clique no ponto do meio da aresta).
        if (_editing && _edgeA != null && _edgeB != null && _edgeA.IsConnectedTo(_edgeB))
        {
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            float w = EditorGUILayout.Slider("Largura do segmento", _edgeA.GetWidthTo(_edgeB), 0.2f, 6f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_edgeA, "Set Segment Width");
                Undo.RecordObject(_edgeB, "Set Segment Width");
                _edgeA.SetWidthTo(_edgeB, w);
                EditorUtility.SetDirty(_edgeA);
                EditorUtility.SetDirty(_edgeB);
            }
        }
    }

    private void OnSceneGUI()
    {
        if (!_editing) return;
        var graph = (RoadGraph)target;
        Event e = Event.current;

        // Botões clicáveis em cada nó.
        foreach (var node in graph.Nodes)
        {
            if (node == null) continue;
            float size = HandleUtility.GetHandleSize(node.Position) * 0.15f;
            Handles.color = node == _selected ? Color.yellow : Color.cyan;
            if (Handles.Button(node.Position, Quaternion.identity, size, size * 1.3f, Handles.SphereHandleCap))
                OnNodeClicked(node, e);
        }

        // Botão no meio de cada segmento (seleciona a aresta para ajustar a largura).
        foreach (var node in graph.Nodes)
        {
            if (node == null) continue;
            foreach (var neighbor in node.Neighbors)
            {
                if (neighbor == null) continue;
                if (node.GetInstanceID() >= neighbor.GetInstanceID()) continue; // uma vez por aresta

                Vector3 mid = (node.Position + neighbor.Position) * 0.5f;
                float s = HandleUtility.GetHandleSize(mid) * 0.09f;
                bool selectedEdge = (node == _edgeA && neighbor == _edgeB) ||
                                    (node == _edgeB && neighbor == _edgeA);
                Handles.color = selectedEdge ? Color.yellow : new Color(1f, 0.6f, 0.1f);
                if (Handles.Button(mid, Quaternion.identity, s, s * 1.4f, Handles.DotHandleCap))
                {
                    _edgeA = node;
                    _edgeB = neighbor;
                    Repaint();
                }
            }
        }

        // Alça para mover o nó selecionado.
        if (_selected != null)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(_selected.Position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_selected.transform, "Move Road Node");
                _selected.transform.position = moved;
            }
        }

        // Shift+Clique em superfície cria um nó.
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.alt)
        {
            if (TryGetSurfacePoint(e.mousePosition, out Vector3 point))
            {
                CreateNode(graph, point);
                e.Use();
            }
        }

        // Mantém o RoadGraph selecionado (para a ferramenta continuar ativa).
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    private void OnNodeClicked(RoadNode node, Event e)
    {
        if (e.control && _selected != null && _selected != node)
        {
            Undo.RecordObject(_selected, "Toggle Connection");
            Undo.RecordObject(node, "Toggle Connection");
            if (_selected.IsConnectedTo(node)) _selected.Disconnect(node);
            else _selected.Connect(node);
            EditorUtility.SetDirty(_selected);
            EditorUtility.SetDirty(node);
        }
        else
        {
            _selected = (_selected == node) ? null : node;
        }
        Repaint();
    }

    private void CreateNode(RoadGraph graph, Vector3 point)
    {
        var go = new GameObject("RoadNode");
        Undo.RegisterCreatedObjectUndo(go, "Create Road Node");
        go.transform.position = point;
        go.transform.SetParent(graph.transform, worldPositionStays: true);
        var node = go.AddComponent<RoadNode>();

        Undo.RecordObject(graph, "Add Road Node");
        graph.CollectNodes();
        EditorUtility.SetDirty(graph);

        // Liga ao nó selecionado para formar a rua.
        if (_selected != null)
        {
            Undo.RecordObject(_selected, "Connect Road Node");
            Undo.RecordObject(node, "Connect Road Node");
            _selected.Connect(node);
            EditorUtility.SetDirty(_selected);
            EditorUtility.SetDirty(node);
        }

        _selected = node;   // continua a partir do novo nó
    }

    // Acha o ponto no mundo sob o mouse: bate em colliders (a cidade) ou, se
    // não houver, num plano horizontal em y=0.
    private static bool TryGetSurfacePoint(Vector2 mousePosition, out Vector3 point)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 5000f))
        {
            point = hit.point;
            return true;
        }

        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        point = default;
        return false;
    }
}
