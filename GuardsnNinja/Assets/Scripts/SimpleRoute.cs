using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// route object, consisting of a list of simple actions and a starting node
public class SimpleRoute
{
    private Vector2 _startNode;
    private List<SimpleAction> _patrolRouteGen;
    private List<SimpleAction> _patrolRoutePhen;

    private float _fitness;
    private float _fullFitness;
    private bool _calcFitness = true;
    private bool _calcPhen = true;

    private float[][] _distribution;
    private float _distributionComp;

    public List<SimpleAction> PatrolRouteGen
    {
        get
        {
            return _patrolRouteGen;
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

    public bool CalcFitness
    {
        get
        {
            return _calcFitness;
        }

        set
        {
            _calcFitness = value;
        }
    }

    public Vector2 StartNode
    {
        get
        {
            return _startNode;
        }

        set
        {
            _startNode = value;
        }
    }

    public List<SimpleAction> PatrolRoutePhen
    {
        get
        {
            return _patrolRoutePhen;
        }
    }

    public float[][] Distribution
    {
        get
        {
            return _distribution;
        }

        set
        {
            _distribution = value;
        }
    }

    public float DistributionComp
    {
        get
        {
            return _distributionComp;
        }

        set
        {
            _distributionComp = value;
        }
    }

    public float FullFitness
    {
        get
        {
            return _fullFitness;
        }

        set
        {
            _fullFitness = value;
        }
    }

    public SimpleRoute()
    {
        _patrolRouteGen = new List<SimpleAction>();
        _startNode = new Vector2();
    }

    public SimpleRoute(Vector2 startNode)
    {
        _patrolRouteGen = new List<SimpleAction>();
        _startNode = startNode;
    }

    public SimpleRoute(SimpleRoute route)
    {
        _patrolRouteGen = new List<SimpleAction>();

        for (int i = 0; i < route.PatrolRouteGen.Count; i++)
        {
            _patrolRouteGen.Add(route.PatrolRouteGen[i].Copy());
        }

        _startNode = route.StartNode;
    }

    public SimpleRoute(SimpleRoute route, Vector2 startNode, int start = 0, int end = -1)
    {
        _patrolRouteGen = new List<SimpleAction>();

        if (end == -1)
        {
            end = route.PatrolRouteGen.Count;
        }

        if (start <= end)
        {
            for (int i = start; i < end; i++)
            {
                _patrolRouteGen.Add(route.PatrolRouteGen[i].Copy());
            }
        }
        else
        {
            for (int i = start; i < route.PatrolRouteGen.Count; i++)
            {
                _patrolRouteGen.Add(route.PatrolRouteGen[i].Copy());
            }

            for (int i = 0; i < end; i++)
            {
                _patrolRouteGen.Add(route.PatrolRouteGen[i].Copy());
            }
        }

        _startNode = startNode;
    }

    public SimpleRoute(SimpleRoute route1, SimpleRoute route2)
    {
        _patrolRouteGen = new List<SimpleAction>();

        for (int i = 0; i < route1.PatrolRouteGen.Count; i++)
        {
            _patrolRouteGen.Add(route1.PatrolRouteGen[i].Copy());
        }

        for (int i = 0; i < route2.PatrolRouteGen.Count; i++)
        {
            _patrolRouteGen.Add(route2.PatrolRouteGen[i].Copy());
        }

        _startNode = new Vector2();
        if (Random.value <= 0.5)
        {
            _startNode.x = route1.StartNode.x;
        }
        else
        {
            _startNode.x = route2.StartNode.x;
        }

        if (Random.value <= 0.5)
        {
            _startNode.y = route1.StartNode.y;
        }
        else
        {
            _startNode.y = route2.StartNode.y;
        }
    }

    public void Add(SimpleAction action)
    {
        _patrolRouteGen.Add(action);
    }

    public SimpleRoute Mutate(int num, int w, int h)
    {
        int rnd;
        for (int i = 0; i < num; i++)
        {
            rnd = Random.Range(0, _patrolRouteGen.Count + 2);
            if (rnd >= _patrolRouteGen.Count)
            {
                _startNode = new Vector2(Random.Range(0, w), Random.Range(0, h));
            }
            else
            {
                _patrolRouteGen[rnd].Mutate();
            }
        }

        _calcFitness = true;
        _calcPhen = true;

        return this;
    }

    public List<SimpleRoute> Divide()
    {
        List<SimpleRoute> output = new List<SimpleRoute>();

        int ind1 = Random.Range(0, _patrolRouteGen.Count), ind2 = Random.Range(0, _patrolRouteGen.Count);

        output.Add(new SimpleRoute(this, _startNode, ind1, ind2));
        output.Add(new SimpleRoute(this, _startNode, ind2, ind1));

        return output;
    }

    public List<SimpleRoute> Divide(int index)
    {
        return new List<SimpleRoute>() { new SimpleRoute(this, _startNode, 0, index),
            new SimpleRoute(this, _startNode, index, PatrolRouteGen.Count) };
    }

    public void GeneratePhenotype(Map map, float[] binsX = null, float[] binsY = null)
    {
        if (_calcPhen)
        {
            _patrolRoutePhen = new List<SimpleAction>();
            
            Vector2 curNode = _startNode;

            int fitBinsX = 0;
            int fitBinsY = 0;
            IVector2 bin = new IVector2();
            if (binsX != null && binsY != null)
            {
                fitBinsX = binsX.Length + 1;
                fitBinsY = binsY.Length + 1;

                ResetDistribution(fitBinsX, fitBinsY);
                bin = FindBin(_startNode, binsX, binsY);
                Distribution[bin.x][bin.y]++;
            }

            Vector2 destNode;
            for (int i = 0; i < _patrolRouteGen.Count; i++)
            {
                destNode = map.Move(curNode, _patrolRouteGen[i].Direction);
                if (binsX != null && binsY != null)
                {
                    bin = FindBin(destNode, binsX, binsY);
                    _distribution[bin.x][bin.y] += 2;
                }
                if (destNode != curNode)
                {
                    _patrolRoutePhen.Add(_patrolRouteGen[i].Copy());
                    curNode = destNode;
                }
                else
                {
                    _patrolRoutePhen.Add(new SimpleAction(Node.Dir.None));
                }
            }
            if (binsX != null && binsY != null)
            {
                _distribution[bin.x][bin.y]--;

                for (int x = 0; x < fitBinsX; x++)
                {
                    for (int y = 0; y < fitBinsY; y++)
                    {
                        _distribution[x][y] /= (_patrolRouteGen.Count * 2);
                    }
                }
            }

            for (int i = _patrolRoutePhen.Count - 1; i >= 0; i--)
            {
                switch(_patrolRoutePhen[i].Direction)
                {
                    case Node.Dir.East:
                        _patrolRoutePhen.Add(new SimpleAction(Node.Dir.West));
                        break;
                    case Node.Dir.North:
                        _patrolRoutePhen.Add(new SimpleAction(Node.Dir.South));
                        break;
                    case Node.Dir.South:
                        _patrolRoutePhen.Add(new SimpleAction(Node.Dir.North));
                        break;
                    case Node.Dir.West:
                        _patrolRoutePhen.Add(new SimpleAction(Node.Dir.East));
                        break;
                    case Node.Dir.None:
                        _patrolRoutePhen.Add(new SimpleAction(Node.Dir.None));
                        break;
                }
            }
            _calcPhen = false;
        }
    }

    public string ToString(bool phenotype)
    {
        string output = _startNode.ToString() + ": ";

        if (phenotype)
        {
            output += _patrolRoutePhen[0].Direction.ToString();
            for (int i = 1; i < _patrolRoutePhen.Count; i++)
            {
                output += ("," + _patrolRoutePhen[i].Direction.ToString());
            }
        }
        else
        {
            output += _patrolRouteGen[0].Direction.ToString();
            for (int i = 1; i < _patrolRouteGen.Count; i++)
            {
                output += ("," + _patrolRouteGen[i].Direction.ToString());
            }
        }
        return output;
    }

    public void ResetDistribution(int binsX, int binsY)
    {
        _distribution = new float[binsX][];

        for (int i = 0; i < binsX; i++)
        {
            _distribution[i] = new float[binsY];
        }

        for (int x = 0; x < binsX; x++)
        {
            for (int y = 0; y < binsY; y++)
            {
                _distribution[x][y] = 0f;
            }
        }
    }

    private IVector2 FindBin(Vector2 pos, float[] binsX, float[] binsY)
    {
        IVector2 bin = new IVector2(-1, -1);
        for (int x = 0; x < binsX.Length; x++)
        {
            if (pos.x <= binsX[x])
            {
                bin.x = x;
                break;
            }
        }
        if (bin.x == -1)
        {
            bin.x = binsX.Length;
        }

        for (int y = 0; y < binsY.Length; y++)
        {
            if (pos.y <= binsY[y])
            {
                bin.y = y;
                break;
            }
        }
        if (bin.y == -1)
        {
            bin.y = binsY.Length;
        }

        return bin;
    }
}
