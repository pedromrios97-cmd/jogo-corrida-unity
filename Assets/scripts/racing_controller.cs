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
    [SerializeField] private Vector3 arrowRotation0ffset = new Vector3(0, 0, 90f);

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

    }
}
