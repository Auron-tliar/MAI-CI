using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// GA calculations for the DB fitness
public static class RouteOptimizer
{
    private static List<SimpleRoute> _routes;
    private static SimpleRouteComparer _routeComparer = new SimpleRouteComparer();
    private static SimpleRouteDistributionComparer _routeDistribComparer = new SimpleRouteDistributionComparer();
    private static int _radius;
    private static float _detDecrease; // for getting farther from the target
    private static float _detDecay; // for time decay

    private static float[] binsX, binsY;

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

        binsX = new float[fitBinsX - 1];
        binsY = new float[fitBinsY - 1];

        for (int x = 1; x < fitBinsX; x++)
        {
            binsX[x - 1] = Mathf.Round(x * map.Width / (float)(fitBinsX));
        }

        for (int y = 1; y < fitBinsY; y++)
        {
            binsY[y - 1] = Mathf.Round(y * map.Height / (float)(fitBinsY));
        }

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
        float totalFullFitness = 0f;

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

                for (int c = 0; c < children.Count; c++)
                {
                    children[c].GeneratePhenotype(map, binsX, binsY);
                }

                float distDiff = 0f;
                float fitMin = 1f, fitMax = 0f, distMin = 1f, distMax = 0f;

                for (int j = 0; j < _routes.Count; j++)
                {
                    distDiff = 0f;
                    for (int c = 0; c < children.Count; c++)
                    {
                        distDiff += DifferenceMeasure(_routes[j], children[c]);
                    }
                    _routes[j].DistributionComp = distDiff / children.Count;
                }

                for (int j = 0; j < _routes.Count; j++)
                {
                    if (_routes[j].Fitness < fitMin)
                    {
                        fitMin = _routes[j].Fitness;
                    }
                    if (_routes[j].Fitness > fitMax)
                    {
                        fitMax = _routes[j].Fitness;
                    }
                    if (_routes[j].DistributionComp < distMin)
                    {
                        distMin = _routes[j].DistributionComp;
                    }
                    if (_routes[j].DistributionComp > distMax)
                    {
                        distMax = _routes[j].DistributionComp;
                    }
                }
                for (int j = 0; j < _routes.Count; j++)
                {
                    _routes[j].Fitness = (_routes[j].Fitness - fitMin) / (fitMax - fitMin);
                    _routes[j].DistributionComp = (_routes[j].DistributionComp - distMin) / (distMax - distMin);
                }

                _routes.Sort(_routeDistribComparer);
                children.AddRange(_routes.GetRange(_routes.Count - diff, diff));
            }

            _routes = children;

            CalculateFitness(map);

            _routes.Sort(_routeComparer);
            totalFitness = 0f;
            totalFullFitness = 0f;

            for (int j = 0; j < _routes.Count; j++)
            {
                swl.WriteLine(j + "(new): " + _routes[j].Fitness);
                swl.WriteLine(j + "(old): " + _routes[j].FullFitness);
                totalFitness += _routes[j].Fitness;
                totalFullFitness += _routes[j].FullFitness;
            }
            totalFitness /= _routes.Count;
            swl.WriteLine("Total fitness: " + totalFitness);
            swl.WriteLine("Total fitness (old): " + totalFullFitness);
            swl.Flush();
        }
        
        SimpleRoute r;
        for (int g = 0; g < _routes.Count; g++)
        {
            r = _routes[g];
            r.GeneratePhenotype(map, binsX, binsY);
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
    public static List<SimpleRoute> Crossover(SimpleRoute p1, SimpleRoute p2, int routeLength)
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
    private static List<float> CalculateFitness(Map map, bool calcOldFitness = true)
    {
        List<float> fitness = new List<float>();
        float bestDetSum = 0f;
        int w = map.Width, h = map.Height;

        List<Vector2> Positions = new List<Vector2>();
        
        int T = 0;
        foreach (SimpleRoute r in _routes)
        {
            Positions.Add(r.StartNode);
            r.GeneratePhenotype(map, binsX, binsY);

            if (r.PatrolRoutePhen.Count > T)
            {
                T = r.PatrolRoutePhen.Count;
            }
            fitness.Add(0f);
        }

        T++;
        
        float[][] currentDet = new float[w][];
        float[][] prevDet = new float[w][];
        float[][] bestDet = new float[w][];

        for (int x = 0; x < w; x++)
        {
            currentDet[x] = new float[h];
            bestDet[x] = new float[h];
        }

        for (int t = 0; t < T; t++)
        {
            if (calcOldFitness)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        bestDet[x][y] = 0f;
                    }
                }
            }

            for (int i = 0; i < _routes.Count; i++)
            {
                prevDet = currentDet;
                currentDet = SpreadDetection(Positions[i], map);

                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if ((prevDet[x][y] - _detDecay) > currentDet[x][y])
                        {
                            currentDet[x][y] = prevDet[x][y] - _detDecay;
                        }

                        if (calcOldFitness && currentDet[x][y] > bestDet[x][y])
                        {
                            bestDet[x][y] = currentDet[x][y];
                        }

                        fitness[i] += currentDet[x][y];
                    }
                }
            }

            if (calcOldFitness)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        bestDetSum += bestDet[x][y];
                    }
                }
            }

            for (int i = 0; i < _routes.Count; i++)
            {
                Positions[i] = map.Move(Positions[i], _routes[i].PatrolRoutePhen[t % _routes[i].PatrolRoutePhen.Count].Direction);
            }
        }

        if (calcOldFitness)
        {
            bestDetSum /= ((float)(T) * w * h);
        }
        for (int i = 0; i < _routes.Count; i++)
        {
            fitness[i] /= ((float)(T) * w * h);
            _routes[i].FullFitness = fitness[i];

            if (calcOldFitness)
            {
                _routes[i].FullFitness = fitness[i] * bestDetSum / _routes[i].PatrolRoutePhen.Count;
            }
            _routes[i].CalcFitness = false;
            _routes[i].Fitness = fitness[i];
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

    // bins matrices difference calculator
    private static float DifferenceMeasure(SimpleRoute r1, SimpleRoute r2)
    {
        float measure = 0f;

        for (int x = 0; x < r1.Distribution.Length; x++)
        {
            for (int y = 0; y < r1.Distribution[0].Length; y++)
            {
                measure += Mathf.Abs(r1.Distribution[x][y] - r2.Distribution[x][y]);
            }
        }

        measure /= 2;

        return measure;
    }
}

// comparer for the SimpleRoutes
public class SimpleRouteComparer : IComparer<SimpleRoute>
{
    public int Compare(SimpleRoute x, SimpleRoute y)
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

// comparer for the complex replacement
public class SimpleRouteDistributionComparer : IComparer<SimpleRoute>
{
    public int Compare(SimpleRoute x, SimpleRoute y)
    {
        float xx = x.DistributionComp * x.Fitness;
        float yy = y.DistributionComp * y.Fitness;
        if (xx < yy)
        {
            return -1;
        }
        else if (xx > yy)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
