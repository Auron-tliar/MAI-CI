using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// GA calculations for the IB fitness
public static class RouteOptimizer2
{
    private static List<SimpleRoute> _routes;
    private static SimpleRouteComparer _routeComparer = new SimpleRouteComparer();
    private static SimpleRouteDistributionComparer _routeDistribComparer = new SimpleRouteDistributionComparer();
    private static int _radius;
    private static float _detDecrease; // for getting farther from the target
    private static float _detDecay; // for time decay

    public static int Radius
    {
        get
        {
            return _radius;
        }
    }

    // GA
    public static List<SimpleRoute> FindScheme(int numGuards, int crossoverSize, Map map, int maxIterations = 100,
        int routeLength = 10, float mutProb = 0.01f, int radius = 10, int detDecayTurns = 10, int fitBinsX = 5,
        int fitBinsY = 3, int seed = -1)
    {
        if (maxIterations > 0 && crossoverSize * 2 > numGuards)
        {
            Debug.LogError("The next generation size is too large!");
        }

        _radius = radius;
        _detDecrease = 1f / _radius;
        _detDecay = 1f / detDecayTurns;

        RandomInitialize(numGuards, routeLength, map);

        SimpleRoute p1, p2;
        List<SimpleRoute> children;

        StreamWriter swl = new StreamWriter("log.txt");
        swl.WriteLine("Number of Guards: " + numGuards);
        swl.WriteLine("Route genotype length: " + routeLength);
        swl.WriteLine("Crossover per stage: " + crossoverSize);
        swl.WriteLine("Detection Radius: " + radius);
        swl.WriteLine("Detection Decay Time: " + detDecayTurns);
        swl.WriteLine("Mutation Probability: " + mutProb);
        swl.WriteLine("Number of Iterations: " + maxIterations);
        swl.WriteLine("Fitness bins# for X: " + fitBinsX);
        swl.WriteLine("Fitness bins# for Y: " + fitBinsY);
        swl.WriteLine("Random seed: " + Random.seed + " (" + seed + ")");
        swl.WriteLine("-------------------------------------------");

        float totalFitness = 0f;

        for (int i = 0; i < _routes.Count; i++)
        {
            totalFitness += _routes[i].Fitness;
        }
        totalFitness /= _routes.Count;

        for (int i = 0; i < maxIterations; i++)
        {
            swl.WriteLine("Iteration " + i);

            children = new List<SimpleRoute>();

            for (int j = 0; j < crossoverSize; j++)
            {
                p1 = Select(totalFitness);
                p2 = Select(totalFitness);

                children.AddRange(Crossover(p1, p2, routeLength));
            }

            children = Mutation(children, mutProb, map.Width, map.Height);

            if (children.Count < _routes.Count)
            {
                int diff = _routes.Count - children.Count;
                children.AddRange(_routes.GetRange(_routes.Count - diff, diff));
            }

            _routes = children;

            CalculateFitness(map);

            _routes.Sort(_routeComparer);
            totalFitness = 0f;

            for (int j = 0; j < _routes.Count; j++)
            {
                swl.WriteLine(j + ": " + _routes[j].Fitness);
                totalFitness += _routes[j].Fitness;
            }
            totalFitness /= _routes.Count;
            swl.WriteLine("Total fitness: " + totalFitness);
            swl.Flush();
        }
        
        SimpleRoute r;
        for (int g = 0; g < _routes.Count; g++)
        {
            r = _routes[g];
            r.GeneratePhenotype(map);
        }
        swl.Close();

        return _routes;
    }

    // randomly initialize the routes
    private static void RandomInitialize(int numGuards, int routeLength, Map map)
    {
        int width = map.Width;
        int height = map.Height;

        int N = routeLength;
        SimpleAction action;

        _routes = new List<SimpleRoute>();
        for (int i = 0; i < numGuards; i++)
        {
            _routes.Add(new SimpleRoute(new Vector2((float)(Random.Range(0, width)), (float)(Random.Range(0, height)))));
            for (int j = 0; j < N; j++)
            {
                action = new SimpleAction((Node.Dir)(Random.Range(0, 4)));
                _routes[i].Add(action);
            }
        }

        CalculateFitness(map);
        _routes.Sort(_routeComparer);
    }

    // wheel selection for the crossover
    private static SimpleRoute Select(float totalFitness)
    {
        float[] probability = new float[_routes.Count - 1];

        probability[0] = _routes[0].Fitness / totalFitness;
        for (int i = 1; i < _routes.Count - 1; i++)
        {
            probability[i] = (probability[i - 1] + _routes[i].Fitness) / totalFitness;
        }

        float rnd = Random.value;

        for (int i = 0; i < _routes.Count - 1; i++)
        {
            if (rnd <= probability[i])
            {
                return _routes[i];
            }
        }
        return _routes[_routes.Count - 1];
    }

    // crossover
    private static List<SimpleRoute> Crossover(SimpleRoute p1, SimpleRoute p2, int routeLength)
    {
        int index = Random.Range(1, routeLength - 1);

        List<SimpleRoute> subroutes1 = p1.Divide(index);
        List<SimpleRoute> subroutes2 = p2.Divide(index);

        List<SimpleRoute> children = new List<SimpleRoute>() { new SimpleRoute(subroutes1[0], subroutes2[1]),
            new SimpleRoute(subroutes2[0], subroutes1[1]) };

        return children;
    }

    // mutations
    private static List<SimpleRoute> Mutation(List<SimpleRoute> population, float mutProb, int w, int h)
    {
        for (int i = 0; i < population.Count; i++)
        {
            if (Random.value <= mutProb)
            {
                population[i].Mutate(4, w, h);
            }
        }

        return population;
    }

    // fitness calcuations
    private static List<float> CalculateFitness(Map map)
    {
        List<float> fitness = new List<float>();
        List<List<HashSet<int>>> routeCheck = new List<List<HashSet<int>>>();
        Dictionary<int, List<Vector2>> checkedCells = new Dictionary<int, List<Vector2>>();
        
        int w = map.Width, h = map.Height;

        List<Vector2> Positions = new List<Vector2>();
        
        int T = 0;
        foreach (SimpleRoute r in _routes)
        {
            Positions.Add(r.StartNode);
            r.GeneratePhenotype(map);

            if (r.PatrolRoutePhen.Count > T)
            {
                T = r.PatrolRoutePhen.Count;
            }
        }
        for (int i = 0; i < _routes.Count; i++)
        {
            checkedCells.Add(i, new List<Vector2>());
            fitness.Add(1);
        }

        for (int x = 0; x < w; x++)
        {
            routeCheck.Add(new List<HashSet<int>>());
            for (int y = 0; y < h; y++)
            {
                routeCheck[x].Add(new HashSet<int>());
            }
        }
        

        for (int t = 0; t < T; t++)
        {

            for (int i = 0; i < _routes.Count; i++)
            {
                Positions[i] = map.Move(Positions[i], 
                    _routes[i].PatrolRoutePhen[t % _routes[i].PatrolRoutePhen.Count].Direction);

                foreach (int r in routeCheck[(int)(Positions[i].x)][(int)(Positions[i].y)])
                {
                    if (!checkedCells[i].Contains(Positions[i]))
                    {
                        checkedCells[i].Add(Positions[i]);
                        fitness[i] -= 1f / T;
                    }
                    if (r != i && !checkedCells[r].Contains(Positions[i]))
                    {
                        checkedCells[r].Add(Positions[i]);
                        fitness[r] -= 1f / T;
                    }
                }

                if (!routeCheck[(int)(Positions[i].x)][(int)(Positions[i].y)].Contains(i))
                {
                    routeCheck[(int)(Positions[i].x)][(int)(Positions[i].y)].Add(i);
                }
            }

        }

        for (int i = 0; i < _routes.Count; i++)
        {
            _routes[i].Fitness = fitness[i];
            _routes[i].CalcFitness = false;
        }

        return fitness;
    }

    // least common multiple calculator (not used)
    private static int LCM(List<int> nums)
    {
        List<int> current = new List<int>();
        for (int i = 0; i < nums.Count; i++)
        {
            current.Add(nums[i]);
        }

        bool finished = false;
        int k;
        while(!finished)
        {
            k = 0;
            for (int i = 1; i < current.Count; i++)
            {
                if (current[i] < current[k])
                {
                    k = i;
                }
            }
            current[k] += nums[k];

            finished = true;
            for (int i = 1; i < current.Count; i++)
            {
                if (current[i] != current[i - 1])
                {
                    finished = false;
                    break;
                }
            }
            
        }

        return current[0];
    }

    // spread detection calculator
    public static float[][] SpreadDetection(Vector2 pos, Map map)
    {
        float[][] detection = new float[map.Width][];
        for (int i = 0; i < map.Width; i++)
        {
            detection[i] = new float[map.Height];
        }
        List<Vector2> neighbors;
        List<Vector2> bordernodes = new List<Vector2>() { pos };
        List<Vector2> temp;

        detection[(int)(pos.x)][(int)(pos.y)] = 1f;
        float curDet = 1f;

        for (int r = 0; r < _radius; r++)
        {
            neighbors = new List<Vector2>();
            for (int i = 0; i < bordernodes.Count; i++)
            {
                temp = map.PercNeighbors(bordernodes[i]);
                for (int j = 0; j < temp.Count; j++)
                {
                    if (detection[(int)(temp[j].x)][(int)(temp[j].y)] == 0)
                    {
                        neighbors.Add(temp[j]);
                    }
                }
            }


            for (int i = 0; i < neighbors.Count; i++)
            {
                detection[(int)(neighbors[i].x)][(int)(neighbors[i].y)] = curDet;
            }
            curDet = Mathf.Max(0f, curDet - _detDecrease);

            bordernodes = neighbors;

        }


        return detection;
    }
}
