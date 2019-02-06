using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// patrol scheme object
public class RouteScheme
{
    private List<SimpleRoute> _routes;

    private float _fitness;

    public List<SimpleRoute> Routes
    {
        get
        {
            return _routes;
        }

        set
        {
            _routes = value;
        }
    }

    public float Fitness
    {
        get
        {
            return _fitness;
        }

        set
        {
            _fitness = value;
        }
    }

    public RouteScheme()
    {
        _routes = new List<SimpleRoute>();
        _fitness = -1f;
    }

    public RouteScheme(List<SimpleRoute> routes)
    {
        _routes = routes;
        _fitness = -1f;
    }

    public RouteScheme Mutate(float routeMutProb, Map map)
    {
        for (int i = 0; i < _routes.Count; i++)
        {
            if (Random.value <= routeMutProb)
            {
                _routes[i] = RouteOptimizer3.RandomRoute(_routes[i].PatrolRouteGen.Count, map);
            }
        }

        return this;
    }
}
