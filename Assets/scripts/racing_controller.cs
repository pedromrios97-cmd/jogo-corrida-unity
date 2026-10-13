using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Events;

public class racing_controller : MonoBehaviour
{
    [SerializeField] private List<RoadNode> route = new List<RoadNode>();
    [SerializeField] private bool circuit = false;
    [Min(1)]
    [SerializeField] private int laps = 3;

    [SerializeField] private Transform car;
    [SerializeField] private float reachRadius = 1f;

    [SerializeField] private Transform checkpointArrow;
    [SerializeField] private Transform finishMarket;
    [SerializeField] private float markerHeight;
    [SerializeField] private Vector3 arrowRotationOffset = new Vector3(0, 0, 90f);

    public UnityEvent<int> OnCheckpointPassed;
    public UnityEvent<int> OnLapCompleted;
    public UnityEvent OnFinished;

    private readonly List<RoadNode> _targets = new List<RoadNode>();
    private Transform _finish;
    private Transform _arrow;
    private int _index;
    private int _lap;
    private bool _Finished;

    public int CurrentLap => _lap;
    public int TotalLaps => circuit ? Mathf.Max(1,laps) : 1;
    public bool Finished => _finish;
    public bool  IsCircuit => circuit;

    public RoadNode CurrentTarget => _index >= 0 && _index < _targets.Count ? _targets[_index] : null;
    private int TargetsPerLap => Mathf.Max(1, route.Count);

    void Start()
    {
        BuildTargets();
        _index = 0;
        _lap = 1;
        _Finished = false;

        if (checkpointArrow != null) _arrow = Instantiate(checkpointArrow);
        if (finishMarket != null) _finish = Instantiate(finishMarket); 

        //UpdateMarkers()
    }

    private void BuildTargets()
    {
        _targets.Clear();
        if(route == null || route.Count < 2) return;

        if (!circuit)
        {
            for(int i = 1; i < route.Count; i++)
            {
                if (route[i] != null) _targets.Add(route[i]);
            }
        }
        else
        {
            int total = Mathf.Max(1, laps);

            for(int lap = 0; lap < total; lap ++)
            {
                for (int i = 1; i < route.Count; i++)
                {
                    if (route[i] != null) _targets.Add(route[i]);
                }

                if (route[0] != null) _targets.Add(route[0]);
            }
        }
    }

    void Update()
    {
        if (_Finished || car == null || _index >= _targets.Count)
        {
            return;
        }

        RoadNode target = _targets[_index];
        if (target == null) { Advance(); return; }

        Vector3 delta = target.Position - car.position;
        delta.y = 0f;
        if (delta.sqrMagnitude <= reachRadius * reachRadius) Advance();
    }

    public void Advance()
    {
        OnCheckpointPassed?.Invoke(_index);

        bool clossedLap = circuit && (_index + 1) % TargetsPerLap == 0;
        _index++;

        if(_index >= _targets.Count)
        {
            //Finish();
            return;
        }

        if (clossedLap)
        {
            _lap++;
            OnLapCompleted?.Invoke(_lap - 1);
        }

        //UpdateMarkers();
    }

    public void Finish()
    {
        _Finished = true;
        if (_arrow != null) _arrow.gameObject.SetActive(false);
        if (_finish != null) _finish.gameObject.SetActive(false);
        OnFinished?.Invoke();
    }

    private void UpdateMarkers()
    {
        if (_index >= _targets.Count) return;

        RoadNode current = _targets[_index];
        if (current == null) return;

        bool isFinish = _index == _targets.Count - 1;

        if (_arrow != null)
        {
            _arrow.gameObject.SetActive(!isFinish);
            if (!isFinish)
            {
                _arrow.position = current.Position + Vector3.up * markerHeight;
                Vector3 dir = _targets[_index + 1].Position - current.Position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    _arrow.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(arrowRotationOffset);
            }
        }

        if (_finish != null)
        {
            _finish.gameObject.SetActive(isFinish);
            if (isFinish)
            {
                _finish.position = current.Position;
                // Orienta a linha na direção de chegada (fica atravessada na pista).
                Vector3 approach = _index > 0 && _targets[_index - 1] != null
                    ? current.Position - _targets[_index - 1].Position
                    : current.Position - car.position;
                approach.y = 0f;
                if (approach.sqrMagnitude > 0.001f)
                    _finish.rotation = Quaternion.LookRotation(approach);
            }
        }
    }

    // ---- Usados pela ferramenta de editor (escolher rota clicando) ----
    public IReadOnlyList<RoadNode> Route => route;
    public void AddCheckpoint(RoadNode node) { if (node != null) route.Add(node); }
    public void RemoveLastCheckpoint() { if (route.Count > 0) route.RemoveAt(route.Count - 1); }
    public void ClearRoute() { route.Clear(); }

    private void OnDrawGizmos()
    {
        if (route == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        for (int i = 0; i < route.Count; i++)
        {
            if (route[i] == null) continue;
            Gizmos.DrawSphere(route[i].Position + Vector3.up * 0.5f, 0.7f);
            if (i + 1 < route.Count && route[i + 1] != null)
                Gizmos.DrawLine(route[i].Position, route[i + 1].Position);
        }
        // Fecha o circuito visualmente (último → primeiro).
        if (circuit && route.Count > 1 && route[0] != null && route[route.Count - 1] != null)
            Gizmos.DrawLine(route[route.Count - 1].Position, route[0].Position);
    }
}

