using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// GA calculations for the IWSB fitness
public static class RouteOptimizer3
{
    private static List<RouteScheme> _routes;
    private static RouteSchemeComparer _routeSchemeComparer = new RouteSchemeComparer();
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
    public static List<SimpleRoute> FindScheme(int populationSize, int numGuards, int crossoverSize, bool simpleCrossover,
        Map map, int maxIterations = 100, int routeLength = 10, float mutProb = 0.01f, float routeMutProb = 0.5f, 
        bool useMin = true, int radius = 10, int detDecayTurns = 10, int seed = -1)
    {
        if (maxIterations > 0 && crossoverSize * 2 > populationSize)
        {
            Debug.LogError("The next generation size is too large!");
        }

        _radius = radius;
        _detDecrease = 1f / _radius;
        _detDecay = 1f / detDecayTurns;

        RandomInitialize(populationSize, numGuards, routeLength, map, useMin);

        RouteScheme p1, p2;
        List<RouteScheme> children;

        StreamWriter swl = new StreamWriter("log.txt");
        swl.WriteLine("Number of Guards: " + numGuards);
        swl.WriteLine("Route genotype length: " + routeLength);
        swl.WriteLine("Crossover per stage: " + crossoverSize);
        swl.WriteLine("Detection Radius: " + radius);
        swl.WriteLine("Detection Decay Time: " + detDecayTurns);
        swl.WriteLine("Mutation Probability: " + mutProb);
        swl.WriteLine("Number of Iterations: " + maxIterations);
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

            children = new List<RouteScheme>();

            for (int j = 0; j < crossoverSize; j++)
            {
                p1 = Select(totalFitness);
                p2 = Select(totalFitness);

                children.AddRange(Crossover(p1, p2, routeLength, simpleCrossover));
            }

            children = Mutation(children, mutProb, routeMutProb, map);

            if (children.Count < _routes.Count)
            {
                int diff = _routes.Count - children.Count;
                children.AddRange(_routes.GetRange(_routes.Count - diff, diff));
            }

            _routes = children;

            CalculatePopFitness(map, useMin);

            _routes.Sort(_routeSchemeComparer);
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
        
        RouteScheme r;
        for (int g = 0; g < _routes.Count; g++)
        {
            r = _routes[g];
            for (int i = 0; i < _routes[g].Routes.Count; i++)
            {
                r.Routes[i].GeneratePhenotype(map);
            }
        }
        swl.Close();

        for (int i = 0; i < _routes[_routes.Count - 1].Routes.Count; i++)
        {
            _routes[_routes.Count - 1].Routes[i].Fitness = _routes[_routes.Count - 1].Fitness;
        }

        return _routes[_routes.Count - 1].Routes;
    }

    // randomly initialize the routes
    private static void RandomInitialize(int popSize, int numGuards, int routeLength, Map map, bool useMin)
    {
        int width = map.Width;
        int height = map.Height;

        int N = routeLength;

        _routes = new List<RouteScheme>();
        for (int p = 0; p < popSize; p++)
        {
            _routes.Add(new RouteScheme());
            for (int i = 0; i < numGuards; i++)
            {
                _routes[p].Routes.Add(RandomRoute(routeLength, map));
            }
        }

        CalculatePopFitness(map, useMin);
        _routes.Sort(_routeSchemeComparer);
    }

    // generates a random route
    public static SimpleRoute RandomRoute(int routeLength, Map map)
    {
        int width = map.Width;
        int height = map.Height;
        int N = routeLength;

        SimpleRoute route = new SimpleRoute(new Vector2((float)(Random.Range(0, width)), (float)(Random.Range(0, height))));

        for (int j = 0; j < N; j++)
        {
            route.Add(new SimpleAction((Node.Dir)(Random.Range(0, 4))));
        }

        return route;
    }

    // wheel selection for the crossover
    private static RouteScheme Select(float totalFitness)
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
    private static List<RouteScheme> Crossover(RouteScheme p1, RouteScheme p2, int routeLength, bool simple)
    {
        List<RouteScheme> children = new List<RouteScheme>(2) { new RouteScheme(), new RouteScheme() };

        if (simple)
        {
            for (int i = 0; i < p1.Routes.Count; i++)
            {
                if (Random.value <= 0.5)
                {
                    children[0].Routes.Add(new SimpleRoute(p1.Routes[i]));
                    children[1].Routes.Add(new SimpleRoute(p2.Routes[i]));
                }
                else
                {
                    children[0].Routes.Add(new SimpleRoute(p2.Routes[i]));
                    children[1].Routes.Add(new SimpleRoute(p1.Routes[i]));
                }
            }
        }
        else
        {
            List<SimpleRoute> routeChildren;
            for (int i = 0; i < p1.Routes.Count; i++)
            {
                if (Random.value <= 0.5)
                {
                    children[0].Routes.Add(new SimpleRoute(p1.Routes[i]));
                    children[1].Routes.Add(new SimpleRoute(p2.Routes[i]));
                }
                else
                {
                    routeChildren = RouteOptimizer.Crossover(p1.Routes[i], p2.Routes[i], routeLength);
                    children[0].Routes.Add(routeChildren[0]);
                    children[1].Routes.Add(routeChildren[1]);
                }
            }
        }

        return children;
    }

    // mutations
    private static List<RouteScheme> Mutation(List<RouteScheme> population, float mutProb, float routeMutProb, Map map)
    {
        for (int i = 0; i < population.Count; i++)
        {
            if (Random.value <= mutProb)
            {
                population[i].Mutate(routeMutProb, map);
            }
        }

        return population;
    }

    // calculate the fitness of the scheme
    private static List<float> CalculatePopFitness(Map map, bool useMin)
    {
        List<float> fitness = new List<float>();
        List<float> curFit;

        if (useMin)
        {
            for (int p = 0; p < _routes.Count; p++)
            {
                curFit = CalculateFitness(p, map);

                fitness.Add(curFit[0]);
                for (int i = 1; i < curFit.Count; i++)
                {
                    if (curFit[i] < fitness[p])
                    {
                        fitness[p] = curFit[i];
                    }
                }
                _routes[p].Fitness = fitness[p];
            }
        }
        else
        {
            for (int p = 0; p < _routes.Count; p++)
            {
                curFit = CalculateFitness(p, map);

                fitness.Add(0f);
                for (int i = 0; i < _routes[p].Routes.Count; i++)
                {
                    fitness[p] += curFit[i];
                }
                fitness[p] /= _routes[p].Routes.Count;
                _routes[p].Fitness = fitness[p];
            }
        }

        return fitness;
    }

    // calculate the fitness of the route
    private static List<float> CalculateFitness(int index, Map map)
    {
        List<float> fitness = new List<float>();
        List<List<HashSet<int>>> routeCheck = new List<List<HashSet<int>>>();
        Dictionary<int, List<Vector2>> checkedCells = new Dictionary<int, List<Vector2>>();
        
        int w = map.Width, h = map.Height;

        List<Vector2> Positions = new List<Vector2>();
        int T = 0;
        foreach (SimpleRoute r in _routes[index].Routes)
        {
            Positions.Add(r.StartNode);
            r.GeneratePhenotype(map);

            if (r.PatrolRoutePhen.Count > T)
            {
                T = r.PatrolRoutePhen.Count;
            }
        }
        for (int i = 0; i < _routes[index].Routes.Count; i++)
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

            for (int i = 0; i < _routes[index].Routes.Count; i++)
            {
                Positions[i] = map.Move(Positions[i], 
                    _routes[index].Routes[i].PatrolRoutePhen[t % _routes[index].Routes[i].PatrolRoutePhen.Count].Direction);

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

        for (int i = 0; i < _routes[index].Routes.Count; i++)
        {
            _routes[index].Routes[i].Fitness = fitness[i];
            _routes[index].Routes[i].CalcFitness = false;
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

// comparer for the schemes
public class RouteSchemeComparer : IComparer<RouteScheme>
{
    public int Compare(RouteScheme x, RouteScheme y)
    {
        if (x.Fitness < y.Fitness)
        {
            return -1;
        }
        else if (x.Fitness > y.Fitness)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
