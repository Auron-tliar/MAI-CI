using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// controller for the Guard GameObjects
public class GuardController : MonoBehaviour
{
    public Text NameField;
    public Transform Direction;

    private static Dictionary<Node.Dir, float> _dirToAngle = new Dictionary<Node.Dir, float>() { { Node.Dir.East, 0f },
        { Node.Dir.North, 90f }, { Node.Dir.West, 180f }, { Node.Dir.South, 270f } };

    [HideInInspector]
    public int X, Y;

    [HideInInspector]
    public Color color;

    private Guard _behavior;
    private int _step = 0;
    private int _checkStep = 0;
    private GameManager _gm;
    private float _detDecrease;
    private Node.Dir _orientation;

    private float _checkMin;
    private float _alarmMin;

    private ZeroOrderTSK.GuardInferenceSystem _fuzzySystem = new ZeroOrderTSK.GuardInferenceSystem();

    public Guard Behavior
    {
        get
        {
            return _behavior;
        }

        set
        {
            _behavior = value;
            NameField.text = "Guard " + value.Id.ToString();
        }
    }

    // Use this for initialization
    private void Start ()
    {
        _gm = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        _detDecrease = 1f / _gm.DetectionRadius;
        GameManager.OnMoveTime += Move;
        GameManager.OnRandomMoveTime += RandomModeMove;
	}

    // used by Game Manager to set the alarm and check thresholds
    public void SetAlarmLimits(float checkMin, float alarmMin)
    {
        _checkMin = checkMin;
        _alarmMin = alarmMin;
    }

    // moving in the normal game. Responce to the OnMove event
    private void Move(Map map, List<Noise> noises, Vector3 ninjaPos)
    {
        CheckDetection(map, noises, ninjaPos);

        if(_checkStep == Behavior.RouteLength)
        {
            Behavior.GuardState = Guard.GuardStates.Patrol;
            _checkStep = 0;
        }

        if (Behavior.GuardState == Guard.GuardStates.Patrol)
        {
            switch (Behavior.Patrol.PatrolRoutePhen[_step].Direction)
            {
                case Node.Dir.South:
                    transform.position += Vector3.down;
                    _orientation = Node.Dir.South;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.South]);
                    break;
                case Node.Dir.North:
                    transform.position += Vector3.up;
                    _orientation = Node.Dir.North;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.North]);
                    break;
                case Node.Dir.West:
                    transform.position += Vector3.left;
                    _orientation = Node.Dir.West;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.West]);
                    break;
                case Node.Dir.East:
                    transform.position += Vector3.right;
                    _orientation = Node.Dir.East;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.East]);
                    break;
                case Node.Dir.None:
                    break;
            }
            _step = (_step + 1) % Behavior.RouteLength;
        }
        else
        {
            switch (Behavior.CheckPath[_checkStep].Direction)
            {
                case Node.Dir.South:
                    transform.position += Vector3.down;
                    _orientation = Node.Dir.South;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.South]);
                    break;
                case Node.Dir.North:
                    transform.position += Vector3.up;
                    _orientation = Node.Dir.North;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.North]);
                    break;
                case Node.Dir.West:
                    transform.position += Vector3.left;
                    _orientation = Node.Dir.West;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.West]);
                    break;
                case Node.Dir.East:
                    transform.position += Vector3.right;
                    _orientation = Node.Dir.East;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.East]);
                    break;
                case Node.Dir.None:
                    break;
            }
            _checkStep++;
        }

        if (ninjaPos == transform.position)
        {
            RaiseAlarm(ninjaPos, ninjaPos);
        }
    }

    //reset the guard (move to the starting position, reset the counters, etc.)
    public void ResetGuard()
    {
        _step = 0;
        _checkStep = 0;
        Behavior.GuardState = Guard.GuardStates.Patrol;

        gameObject.transform.position = new Vector3(Behavior.Patrol.StartNode.x, Behavior.Patrol.StartNode.y);
        Direction.gameObject.SetActive(true);
        _orientation = Node.Dir.None;
        for (int i = 1; i <= Behavior.RouteLength; i++)
        {
            _orientation =
                Behavior.Patrol.PatrolRoutePhen[(int)(Mathf.Repeat(Behavior.RouteLength - 1, Behavior.RouteLength))].Direction;
            if (_orientation != Node.Dir.None)
            {
                break;
            }
        }
        if (_orientation == Node.Dir.None)
        {
            _orientation = Node.Dir.North;
        }
        Direction.eulerAngles = new Vector3(0f, 0f, 
            _dirToAngle[_orientation]);
    }

    // process going to check the noise
    private void CheckNoise(Vector2 pos, Map map)
    {
        List<SimpleAction> path = AStar(new Vector2(transform.position.x, transform.position.y), pos, map);
        int halfLen = Behavior.RouteLength / 2;
        if (path.Count < halfLen)
        {
            int diff = halfLen - path.Count;
            for (int i = 0; i < diff; i++)
            {
                path.Add(new SimpleAction(Node.Dir.None));
            }
        }
        else
        {
            path = path.GetRange(0, halfLen);
        }

        for (int i = path.Count - 1; i >= 0; i--)
        {
            path.Add(path[i]);
        }

        Behavior.CheckPath = path;
        Behavior.GuardState = Guard.GuardStates.Check;
    }

    // get the results for raising the alarm
    private void RaiseAlarm(Vector3 noisePos, Vector3 ninjaPos)
    {
        if (noisePos == ninjaPos)
        {
            _gm.EndGame("You were caught!");
        }
        else
        {
            _gm.StunGuards();
        }
    }

    // run the fuzzy system and process the results
    private void CheckDetection(Map map, List<Noise> noises, Vector3 ninjaPos)
    {
        if (ninjaPos == transform.position)
        {
            RaiseAlarm(ninjaPos, ninjaPos);
        }
        else
        {
            map.SpreadDetection(Behavior.Id, gameObject.transform.position);

            float maxAlarm = 0f;
            float curAlarm;
            int maxNoise = -1;
            Vector2 percepts;

            for (int i = 0; i < noises.Count; i++)
            {
                percepts = GetPercepts(noises[i], map);

                curAlarm = (float)(_fuzzySystem.getAlarm(percepts.x, percepts.y));
                if (curAlarm > maxAlarm)
                {
                    maxAlarm = curAlarm;
                    maxNoise = i;
                }
            }

            if (maxAlarm >= _alarmMin)
            {
                RaiseAlarm(noises[maxNoise].pos, ninjaPos);
            }
            else if (maxAlarm >= _checkMin && Behavior.GuardState == Guard.GuardStates.Patrol)
            {
                CheckNoise(noises[maxNoise].pos, map);
            }

            map.SpreadZeroes(Behavior.Id, gameObject.transform.position);
        }
    }

    // get the hearing and vision values
    private Vector2 GetPercepts(Noise noise, Map map)
    {
        Vector2 percepts = new Vector2();

        Vector2 pos = new Vector2(transform.position.x, transform.position.y);
        float detection = map.GetDetection(Behavior.Id, noise.pos);

        if (Random.value <= detection)
        {
            percepts.x = noise.intencity * 100f;
        }
        else
        {
            percepts.x = 0f;
        }

        Node.Dir orientation = Node.Dir.None;
        for (int i = 1; i <= Behavior.RouteLength; i++)
        {
            orientation =
                Behavior.Patrol.PatrolRoutePhen[(int)(Mathf.Repeat(_step - 1, Behavior.RouteLength))].Direction;
            if (orientation != Node.Dir.None)
            {
                break;
            }
        }
        if (orientation == Node.Dir.None)
        {
            orientation = Node.Dir.North;
        }

        float trueDet = Mathf.Max(0f, 1f - (Mathf.Abs(pos.x - noise.pos.x) + Mathf.Abs(pos.y - noise.pos.y) - 1) * _detDecrease);
        switch (orientation)
        {
            case Node.Dir.South:
                if (noise.pos.y < pos.y && 
                    Mathf.Atan2(Mathf.Abs(noise.pos.x - pos.x), pos.y - noise.pos.y) <= (Mathf.PI / 4 + Mathf.Epsilon) &&
                    Mathf.Abs(detection - trueDet) < Mathf.Epsilon)
                {
                    percepts.y = Mathf.Min(1f, detection * 2) * 100f;
                }
                else
                {
                    percepts.y = 0f;
                }
                break;
            case Node.Dir.North:
                if (noise.pos.y > pos.x &&
                    Mathf.Atan2(Mathf.Abs(noise.pos.x - pos.x), noise.pos.y - pos.y) <= (Mathf.PI / 4 + Mathf.Epsilon) &&
                    Mathf.Abs(detection - trueDet) < Mathf.Epsilon)
                {
                    percepts.y = Mathf.Min(1f, detection * 2) * 100f;
                }
                else
                {
                    percepts.y = 0f;
                }
                break;
            case Node.Dir.West:
                if (noise.pos.x < pos.x &&
                    Mathf.Atan2(Mathf.Abs(noise.pos.y - pos.y), pos.x - noise.pos.x) <= (Mathf.PI / 4 + Mathf.Epsilon) &&
                    Mathf.Abs(detection - trueDet) < Mathf.Epsilon)
                {
                    percepts.y = Mathf.Min(1f, detection * 2) * 100f;
                }
                else
                {
                    percepts.y = 0f;
                }
                break;
            case Node.Dir.East:
                if (noise.pos.x > pos.x &&
                    Mathf.Atan2(Mathf.Abs(noise.pos.y - pos.y), noise.pos.x - pos.x) <= (Mathf.PI / 4 + Mathf.Epsilon) &&
                    Mathf.Abs(detection - trueDet) < Mathf.Epsilon)
                {
                    percepts.y = Mathf.Min(1f, detection * 2) * 100f;
                }
                else
                {
                    percepts.y = 0f;
                }
                break;
        }


        return percepts;
    }

    // move and react in case of the random ninjas mode
    private void RandomModeMove(Map map, List<Noise> noises, List<Vector2> ninjaPos)
    {
        CheckDetectionRandomMode(map, noises, ninjaPos);

        if (_checkStep == Behavior.RouteLength)
        {
            Behavior.GuardState = Guard.GuardStates.Patrol;
            _checkStep = 0;
        }

        if (Behavior.GuardState == Guard.GuardStates.Patrol)
        {
            switch (Behavior.Patrol.PatrolRoutePhen[_step].Direction)
            {
                case Node.Dir.South:
                    transform.position += Vector3.down;
                    _orientation = Node.Dir.South;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.South]);
                    break;
                case Node.Dir.North:
                    transform.position += Vector3.up;
                    _orientation = Node.Dir.North;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.North]);
                    break;
                case Node.Dir.West:
                    transform.position += Vector3.left;
                    _orientation = Node.Dir.West;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.West]);
                    break;
                case Node.Dir.East:
                    transform.position += Vector3.right;
                    _orientation = Node.Dir.East;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.East]);
                    break;
                case Node.Dir.None:
                    break;
            }
            _step = (_step + 1) % Behavior.RouteLength;
        }
        else
        {
            switch (Behavior.CheckPath[_checkStep].Direction)
            {
                case Node.Dir.South:
                    transform.position += Vector3.down;
                    _orientation = Node.Dir.South;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.South]);
                    break;
                case Node.Dir.North:
                    transform.position += Vector3.up;
                    _orientation = Node.Dir.North;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.North]);
                    break;
                case Node.Dir.West:
                    transform.position += Vector3.left;
                    _orientation = Node.Dir.West;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.West]);
                    break;
                case Node.Dir.East:
                    transform.position += Vector3.right;
                    _orientation = Node.Dir.East;
                    Direction.eulerAngles = new Vector3(0f, 0f, _dirToAngle[Node.Dir.East]);
                    break;
                case Node.Dir.None:
                    break;
            }
            _checkStep++;
        }
    }

    // check detection in case of several ninjas
    private void CheckDetectionRandomMode(Map map, List<Noise> noises, List<Vector2> ninjaPos)
    {
        for (int i = 0; i < ninjaPos.Count; i++)
        {
            if (ninjaPos[i].x == transform.position.x && ninjaPos[i].y == transform.position.y)
            {
                _gm.ReportAlarm(true);
                noises.RemoveAt(noises.Count - ninjaPos.Count + i);
            }
        }
        
        map.SpreadDetection(Behavior.Id, gameObject.transform.position);

        float maxAlarm = 0f;
        float curAlarm;
        int maxNoise = -1;
        Vector2 percepts;

        for (int i = 0; i < noises.Count; i++)
        {
            if (noises[i].pos.x == transform.position.x && noises[i].pos.y == transform.position.y)
            {
                continue;
            }

            percepts = GetPercepts(noises[i], map);

            curAlarm = (float)(_fuzzySystem.getAlarm(percepts.x, percepts.y));
            if (curAlarm > maxAlarm)
            {
                maxAlarm = curAlarm;
                maxNoise = i;
            }
        }

        if (maxAlarm >= _alarmMin)
        {
            RaiseAlarmRandomMode(noises[maxNoise].pos, ninjaPos);
        }
        else if (maxAlarm >= _checkMin && Behavior.GuardState == Guard.GuardStates.Patrol)
        {
            CheckNoise(noises[maxNoise].pos, map);
        }

        map.SpreadZeroes(Behavior.Id, gameObject.transform.position);
    }

    // raise alarm in case of random ninjas mode
    private void RaiseAlarmRandomMode(Vector2 noisePos, List<Vector2> ninjaPos)
    {
        for (int i = 0; i < ninjaPos.Count; i++)
        {
            if (noisePos == ninjaPos[i])
            {
                _gm.ReportAlarm(true);
                return;
            }
        }
        _gm.ReportAlarm(false);
    }

    // A* algorithm
    public static List<SimpleAction> AStar(Vector2 pos, Vector2 goal, Map map)
    {
        if (pos == goal)
        {
            return new List<SimpleAction>();
        }
        List<SimpleAction> path = new List<SimpleAction>();

        HashSet<Vector2> closedSet = new HashSet<Vector2>();
        List<Vector2> openSet = new List<Vector2>() { pos };

        Dictionary<Vector2, Vector2> cameFrom = new Dictionary<Vector2, Vector2>();

        Dictionary<Vector2, float> gScore = new Dictionary<Vector2, float>();

        gScore.Add(pos, 0f);

        Dictionary<Vector2, float> fScore = new Dictionary<Vector2, float>();

        fScore.Add(pos, Vector2.Distance(pos, goal));

        Vector2 current;
        int curI;
        float tempG;

        while (openSet.Count > 0)
        {
            curI = 0;
            current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore[openSet[i]] < fScore[current])
                {
                    current = openSet[i];
                    curI = i;
                }
            }

            if (current == goal)
            {
                Vector2 curNode = current;
                Vector2 prevNode;
                do
                {                    
                    prevNode = cameFrom[curNode];
                    path.Add(new SimpleAction(VecToDir(curNode - prevNode)));
                    curNode = prevNode;
                }
                while (curNode != pos);

                return path;
            }

            openSet.RemoveAt(curI);
            closedSet.Add(current);

            List<Vector2> neighbors = map.PassNeighbors(current);
            foreach (Vector2 neighbor in neighbors)
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }

                tempG = gScore[current] + 1f;
                if (gScore.ContainsKey(neighbor))
                {
                    if (gScore[neighbor] <= tempG)
                    {
                        continue;
                    }
                    else
                    {
                        gScore[neighbor] = tempG;
                        fScore[neighbor] = tempG + Vector2.Distance(neighbor, goal);
                    }
                }
                else
                {
                    gScore.Add(neighbor, tempG);
                    fScore.Add(neighbor, tempG + Vector2.Distance(neighbor, goal));
                }

                if (cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                }
                else
                {
                    cameFrom.Add(neighbor, current);
                }
            }
        }


        return path;
    }

    // transfrom 4 basic unit vectors to Node.Dir direction object
    public static Node.Dir VecToDir(Vector2 v)
    {
        if (v == Vector2.up)
        {
            return Node.Dir.North;
        }
        else if (v == Vector2.left)
        {
            return Node.Dir.West;
        }
        else if (v == Vector2.down)
        {
            return Node.Dir.South;
        }
        else if (v == Vector2.right)
        {
            return Node.Dir.East;
        }
        else
        {
            return Node.Dir.None;
        }
    }
}
