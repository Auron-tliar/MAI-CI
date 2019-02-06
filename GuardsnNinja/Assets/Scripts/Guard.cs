using System;
using System.Collections.Generic;

// guard behaviour class to contain the routes for patrol and for noise checks. Also contains their IDs
public class Guard
{
    public enum GuardStates { Patrol, Check }

    private static int _lastId = -1;
    private int _id;

    private SimpleRoute _patrol;
    private List<SimpleAction> _checkPath;
    private int _routeLength;
    private GuardStates _guardState;

    public int Id
    {
        get
        {
            return _id;
        }
        set
        {
            _id = value;
        }
    }

    public SimpleRoute Patrol
    {
        get
        {
            return _patrol;
        }
        set
        {
            _patrol = value;
        }
    }

    public int RouteLength
    {
        get
        {
            return _routeLength;
        }
    }

    public GuardStates GuardState
    {
        get
        {
            return _guardState;
        }

        set
        {
            _guardState = value;
        }
    }

    public List<SimpleAction> CheckPath
    {
        get
        {
            return _checkPath;
        }

        set
        {
            _checkPath = value;
        }
    }

    public Guard(SimpleRoute route)
    {
        _lastId++;
        _id = _lastId;

        _patrol = route;
        _routeLength = route.PatrolRoutePhen.Count;
        _guardState = GuardStates.Patrol;
    }
}
