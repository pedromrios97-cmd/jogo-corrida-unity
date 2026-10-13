using UnityEditor;
using UnityEngine;

namespace Mynds.Race.EditorTools
{
    /// <summary>
    /// Ferramenta para montar a ROTA da corrida clicando nos nós na Scene view.
    ///
    /// Com "Escolher rota" ligado: clique nos RoadNodes na ordem desejada — cada
    /// clique adiciona à rota. Os números mostram a ordem; a linha laranja mostra
    /// o percurso. Botões para remover o último e limpar.
    /// </summary>
    [CustomEditor(typeof(racing_controller))]
    public class RaceControllerEditor : Editor
    {
        private bool _picking;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var race = (racing_controller)target;

            EditorGUILayout.Space();
            _picking = GUILayout.Toggle(_picking, "Escolher rota (clicar nós)", "Button");

            if (_picking)
                EditorGUILayout.HelpBox(
                    "Clique nos nós na ordem da corrida. O primeiro é a largada/chegada.",
                    MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Remover último"))
                {
                    Undo.RecordObject(race, "Remove Checkpoint");
                    race.RemoveLastCheckpoint();
                    EditorUtility.SetDirty(race);
                }
                if (GUILayout.Button("Limpar rota"))
                {
                    Undo.RecordObject(race, "Clear Route");
                    race.ClearRoute();
                    EditorUtility.SetDirty(race);
                }
            }
        }

        private void OnSceneGUI()
        {
            var race = (racing_controller)target;

            // Números + linhas da rota atual.
            var route = race.Route;
            Handles.color = new Color(1f, 0.6f, 0.1f);
            for (int i = 0; i < route.Count; i++)
            {
                if (route[i] == null) continue;
                Handles.Label(route[i].Position + Vector3.up * 2f, (i + 1).ToString());
            }

            if (!_picking) return;

            // Botões em cada nó do grafo para adicionar à rota.
            RoadNode[] nodes = Object.FindObjectsByType<RoadNode>(FindObjectsSortMode.None);
            foreach (var node in nodes)
            {
                if (node == null) continue;
                float size = HandleUtility.GetHandleSize(node.Position) * 0.18f;
                Handles.color = Color.cyan;
                if (Handles.Button(node.Position, Quaternion.identity, size, size * 1.3f, Handles.SphereHandleCap))
                {
                    Undo.RecordObject(race, "Add Checkpoint");
                    race.AddCheckpoint(node);
                    EditorUtility.SetDirty(race);
                }
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }
}
