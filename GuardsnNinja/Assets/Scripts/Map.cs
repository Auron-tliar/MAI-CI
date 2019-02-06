using System.Collections.Generic;
using UnityEngine;
using System.IO;

// map object
public class Map
{
    private List<List<Node>> _grid;
    private List<Node> _startingNodes;
    private Node _goal;
    private int _width, _height;

    private GameManager.FitnessFunctions _fitnessFunction;

    private static Dictionary<Node.Dir, Vector2> dirModifyer = new Dictionary<Node.Dir, Vector2>() {
        { Node.Dir.West, Vector2.left }, { Node.Dir.North, Vector2.up }, {Node.Dir.East, Vector2.right },
        { Node.Dir.South, Vector2.down } };

    public int Width
    {
        get
        {
            return _width;
        }

        set
        {
            _width = value;
        }
    }

    public int Height
    {
        get
        {
            return _height;
        }

        set
        {
            _height = value;
        }
    }

    public Map(int width, int height = 0, GameManager.FitnessFunctions fit = GameManager.FitnessFunctions.IntersectionScheme)
        // generate a uniform map
    {
        if (height == 0)
        {
            height = width;
        }
        this._width = width;
        this._height = height;

        _fitnessFunction = fit;

        _startingNodes = new List<Node>();

        Node tempNode;
        Edge tempEdge;
        _grid = new List<List<Node>>(width);

        _grid.Add(new List<Node>(height));
        tempNode = new Node(0, 0);
        _grid[0].Add(tempNode);
        tempEdge = new Edge(null, _grid[0][0], true, false, false);
        tempEdge = new Edge(null, _grid[0][0], false, false, false);
        for (int y = 1; y < height - 1; y++)
        {
            tempNode = new Node(0, y);
            _grid[0].Add(tempNode);
            tempEdge = new Edge(_grid[0][y - 1], _grid[0][y], true);
            tempEdge = new Edge(null, _grid[0][y], false, false, false);
        }
        tempNode = new Node(0, height - 1);
        _grid[0].Add(tempNode);
        tempEdge = new Edge(_grid[0][height - 2], _grid[0][height - 1], true);
        tempEdge = new Edge(_grid[0][height - 1], null, true, false, false);
        tempEdge = new Edge(null, _grid[0][height - 1], false, false, false);

        for (int x = 1; x < width; x++)
        {
            _grid.Add(new List<Node>(height));
            tempNode = new Node(x, 0);
            _grid[x].Add(tempNode);
            tempEdge = new Edge(null, _grid[x][0], true, false, false);
            tempEdge = new Edge(_grid[x - 1][0], _grid[x][0], false);
            for (int y = 1; y < height - 1; y++)
            {
                tempNode = new Node(x, y);
                _grid[x].Add(tempNode);
                tempEdge = new Edge(_grid[x][y - 1], _grid[x][y], true);
                tempEdge = new Edge(_grid[x - 1][y], _grid[x][y], false);
            }
            tempNode = new Node(x, height - 1);
            _grid[x].Add(tempNode);
            tempEdge = new Edge(_grid[x][height - 2], _grid[x][height - 1], true);
            tempEdge = new Edge(_grid[x][height - 1], null, true, false, false);
            tempEdge = new Edge(_grid[x - 1][height - 1], _grid[x][height - 1], false);
        }
            
        for (int y = 0; y < height; y++)
        {
            tempEdge = new Edge(_grid[width - 1][y], null, false, false, false);
        }

        for (int y = 0; y < _height; y++)
        {
            _startingNodes.Add(_grid[0][y]);
        }

        _goal = _grid[_width - 1][Mathf.RoundToInt(_height / 2)];
    }

    public Map(string file, GameManager.FitnessFunctions fit = GameManager.FitnessFunctions.IntersectionScheme)
        // read map from file
    {
        _startingNodes = new List<Node>();

        _fitnessFunction = fit;

        StreamReader sr = new StreamReader(file);

        try
        {
            string cur = sr.ReadLine();
            char[] splits = new char[] { '\t', ' ', 'O', 'S' };
            string[] lineSplit = cur.Split(splits);
            Edge tempEdge;

            _width = int.Parse(lineSplit[0]);
            _height = int.Parse(lineSplit[1]);

            _grid = new List<List<Node>>();
            for (int x = 0; x < _width; x++)
            {
                _grid.Add(new List<Node>());
                for (int y = 0; y < _height; y++)
                {
                    _grid[x].Add(new Node(x, y));
                }
            }

            cur = sr.ReadLine();
            lineSplit = cur.Split(splits);

            _goal = _grid[int.Parse(lineSplit[0])][int.Parse(lineSplit[1])];

            cur = sr.ReadLine();
            lineSplit = cur.Split(splits);
            for (int x = 0; x < _width; x++)
            {
                tempEdge = new Edge(_grid[x][_height - 1], null, true, SymbToPass(lineSplit[x + 1]), SymbToPerc(lineSplit[x + 1]));
            }

            for (int y = _height - 1; y > 0; y--)
            {
                cur = sr.ReadLine();
                lineSplit = cur.Split(splits);
                tempEdge = new Edge(null, _grid[0][y], false, SymbToPass(lineSplit[0]), SymbToPerc(lineSplit[0]));
                for (int x = 1; x < _width; x++)
                {
                    tempEdge = new Edge(_grid[x - 1][y], _grid[x][y], false, SymbToPass(lineSplit[x]), SymbToPerc(lineSplit[x]));
                    if (cur.Substring((x - 1) * 2 + 1, 1) == "S")
                    {
                        _startingNodes.Add(_grid[x - 1][y]);
                    }
                }
                tempEdge = new Edge(_grid[_width - 1][y], null, false, SymbToPass(lineSplit[_width]), SymbToPerc(lineSplit[_width]));

                cur = sr.ReadLine();
                lineSplit = cur.Split(splits);

                for (int x = 0; x < _width; x++)
                {
                    tempEdge = new Edge(_grid[x][y - 1], _grid[x][y], true, SymbToPass(lineSplit[x + 1]), SymbToPerc(lineSplit[x + 1]));
                }
            }

            cur = sr.ReadLine();
            lineSplit = cur.Split(splits);
            tempEdge = new Edge(null, _grid[0][0], false, SymbToPass(lineSplit[0]), SymbToPerc(lineSplit[0]));
            for (int x = 1; x < _width; x++)
            {
                tempEdge = new Edge(_grid[x - 1][0], _grid[x][0], false, SymbToPass(lineSplit[x]), SymbToPerc(lineSplit[x]));
                if (cur.Substring((x - 1) * 2 + 1, 1) == "S")
                {
                    _startingNodes.Add(_grid[x - 1][0]);
                }
            }
            tempEdge = new Edge(_grid[_width - 1][0], null, false, SymbToPass(lineSplit[_width]), SymbToPerc(lineSplit[_width]));

            cur = sr.ReadLine();
            lineSplit = cur.Split(splits);

            for (int x = 0; x < _width; x++)
            {
                tempEdge = new Edge(null, _grid[x][0], true, SymbToPass(lineSplit[x + 1]), SymbToPerc(lineSplit[x + 1]));
            }
        }
        catch(System.Exception ex)
        {
            throw new MapFileException(ex.Message, sr);
        }
    }

    // transform a character from file to whether the edge is passable
    private bool SymbToPass(string s)
    {
        if (s == "+")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // transform a character from file to whether the edge is perceivable
    private bool SymbToPerc(string s)
    {
        if (s == "+" || s == "=")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // uploads the map to the file for checking
    public bool CheckContingency()
    {
        StreamWriter sw = new StreamWriter("MapCheck.txt");
        

        for (int y = _height - 1; y >= 0; y--)
        {
            for (int x = 0; x < _width; x++)
            {
                sw.Write(" ");
                sw.Write(_grid[x][y].Edges[Node.Dir.North].ToString());
                sw.Write(" ");
            }
            sw.WriteLine();
            for (int x = 0; x < _width; x++)
            {
                sw.Write(_grid[x][y].Edges[Node.Dir.West].ToString() + "O" + _grid[x][y].Edges[Node.Dir.East].ToString());
            }
            sw.WriteLine();
            for (int x = 0; x < _width; x++)
            {
                sw.Write(" ");
                sw.Write(_grid[x][y].Edges[Node.Dir.South].ToString());
                sw.Write(" ");
            }
            sw.WriteLine();
        }

        sw.Close();

        return true;
    }

    // get the results of moving from the position pos in the direction direction
    public Vector2 Move(Vector2 pos, Node.Dir direction)
    {
        if (direction == Node.Dir.None)
        {
            return pos;
        }
        else if (_grid[(int)(pos.x)][(int)(pos.y)].Edges[direction].IsPassable)
        {
            return pos + dirModifyer[direction];
        }
        else
        {
            return pos;
        }
        
    }

    // return the node with the specified coordinates
    public Node GetNode(int x, int y)
    {
        return _grid[x][y];
    }

    // modify the node text
    public void ModifyNodeText(Vector2 node, string text)
    {
        
        _grid[(int)(node.x)][(int)(node.y)].Tile.GetComponent<MapTileController>().ModifyText(text);
    }

    // modify the tile route diplay
    public void ModifyRouteDisplay(Vector2 origin, Vector2 dest, Node.Dir direction, Color color)
    {
        if (direction != Node.Dir.None)
        {
            _grid[(int)(origin.x)][(int)(origin.y)].Tile.GetComponent<MapTileController>().ModifyRoute(direction, color);
        }
        switch (direction)
        {
            case Node.Dir.East:
                _grid[(int)(dest.x)][(int)(dest.y)].Tile.GetComponent<MapTileController>().ModifyRoute(Node.Dir.West, color);
                break;
            case Node.Dir.North:
                _grid[(int)(dest.x)][(int)(dest.y)].Tile.GetComponent<MapTileController>().ModifyRoute(Node.Dir.South, color);
                break;
            case Node.Dir.West:
                _grid[(int)(dest.x)][(int)(dest.y)].Tile.GetComponent<MapTileController>().ModifyRoute(Node.Dir.East, color);
                break;
            case Node.Dir.South:
                _grid[(int)(dest.x)][(int)(dest.y)].Tile.GetComponent<MapTileController>().ModifyRoute(Node.Dir.North, color);
                break;
            case Node.Dir.None:
                _grid[(int)(dest.x)][(int)(dest.y)].Tile.GetComponent<MapTileController>().ModifyPause(color);
                break;
        }
        
    }

    // neighbors that can be reached
    public List<Vector2> PassNeighbors(Vector2 pos)
    {
        List<Vector2> neighbors = new List<Vector2>();

        Node node = _grid[(int)(pos.x)][(int)(pos.y)];

        Node.Dir dir;
        for (int i = 0; i < 4; i++)
        {
            dir = (Node.Dir)(i);
            if (node.Edges[dir].IsPassable)
            {
                neighbors.Add(node.Edges[dir].Node2.ToVector2());
            }
        }

        return neighbors;
    }

    // neighbors that can be perceived
    public List<Vector2> PercNeighbors(Vector2 pos)
    {
        List<Vector2> neighbors = new List<Vector2>();

        Node node = _grid[(int)(pos.x)][(int)(pos.y)];

        Node.Dir dir;
        for (int i = 0; i < 4; i++)
        {
            dir = (Node.Dir)(i);
            if (node.Edges[dir].IsPerceptible)
            {
                neighbors.Add(node.Edges[dir].Node2.ToVector2());
            }
        }

        return neighbors;
    }

    // clear the routes indicators from the map
    public void ClearRoutes()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _grid[x][y].Tile.GetComponent<MapTileController>().ClearRoutes();
            }
        }
    }

    //highlight the goal
    public void SetGoal()
    {
        _goal.Tile.GetComponent<SpriteRenderer>().color = Color.green;
    }

    //highlight the starting node and set them to react to clicks
    public void SetStartingNodes(bool activate)
    {
        if (activate)
        {
            foreach (Node n in _startingNodes)
            {
                n.Tile.GetComponent<SpriteRenderer>().color = Color.cyan;
                n.Tile.GetComponent<MapTileController>().IsStartingNode = true;
            }
        }
        else
        {
            foreach (Node n in _startingNodes)
            {
                n.Tile.GetComponent<SpriteRenderer>().color = Color.white;
                n.Tile.GetComponent<MapTileController>().IsStartingNode = false;
            }
        }
    }

    // check whether the position corresponds to the goal
    public bool IsAtGoal(Vector2 pos)
    {
        return (pos == _goal.ToVector2());
    }

    // set detection lists for the nodes
    public void SetDetections(int numGuards)
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int i = 0; i < numGuards; i++)
                {
                    _grid[x][y].Detection.Add(0f);
                }
            }
        }
    }

    // spread the detection values from the pos
    public void SpreadDetection(int id, Vector3 pos)
    {
        float[][] detection = new float[0][];
        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        switch (_fitnessFunction)
        {
            case GameManager.FitnessFunctions.DetectionBased:
                detection = RouteOptimizer.SpreadDetection(new Vector2(pos.x, pos.y), this);
                minX = Mathf.Clamp((int)(pos.x) - RouteOptimizer.Radius, 0, _width - 1);
                maxX = Mathf.Clamp((int)(pos.x) + RouteOptimizer.Radius, 0, _width - 1);
                minY = Mathf.Clamp((int)(pos.y) - RouteOptimizer.Radius, 0, _height - 1);
                maxY = Mathf.Clamp((int)(pos.y) + RouteOptimizer.Radius, 0, _height - 1);
                break;
            case GameManager.FitnessFunctions.IntersectionBased:
                detection = RouteOptimizer2.SpreadDetection(new Vector2(pos.x, pos.y), this);
                minX = Mathf.Clamp((int)(pos.x) - RouteOptimizer2.Radius, 0, _width - 1);
                maxX = Mathf.Clamp((int)(pos.x) + RouteOptimizer2.Radius, 0, _width - 1);
                minY = Mathf.Clamp((int)(pos.y) - RouteOptimizer2.Radius, 0, _height - 1);
                maxY = Mathf.Clamp((int)(pos.y) + RouteOptimizer2.Radius, 0, _height - 1);
                break;
            case GameManager.FitnessFunctions.IntersectionScheme:
                detection = RouteOptimizer3.SpreadDetection(new Vector2(pos.x, pos.y), this);
                minX = Mathf.Clamp((int)(pos.x) - RouteOptimizer3.Radius, 0, _width - 1);
                maxX = Mathf.Clamp((int)(pos.x) + RouteOptimizer3.Radius, 0, _width - 1);
                minY = Mathf.Clamp((int)(pos.y) - RouteOptimizer3.Radius, 0, _height - 1);
                maxY = Mathf.Clamp((int)(pos.y) + RouteOptimizer3.Radius, 0, _height - 1);
                break;
        }
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                _grid[x][y].Detection[id] = detection[x][y];
            }
        }
    }

    // rewrite the spreaded detection values with zeroes
    public void SpreadZeroes(int id, Vector3 pos)
    {
        float[][] detection = new float[0][];
        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        switch (_fitnessFunction)
        {
            case GameManager.FitnessFunctions.DetectionBased:
                detection = RouteOptimizer.SpreadDetection(new Vector2(pos.x, pos.y), this);
                minX = Mathf.Clamp((int)(pos.x) - RouteOptimizer.Radius, 0, _width - 1);
                maxX = Mathf.Clamp((int)(pos.x) + RouteOptimizer.Radius, 0, _width - 1);
                minY = Mathf.Clamp((int)(pos.y) - RouteOptimizer.Radius, 0, _height - 1);
                maxY = Mathf.Clamp((int)(pos.y) + RouteOptimizer.Radius, 0, _height - 1);
                break;
            case GameManager.FitnessFunctions.IntersectionBased:
                detection = RouteOptimizer2.SpreadDetection(new Vector2(pos.x, pos.y), this);
                minX = Mathf.Clamp((int)(pos.x) - RouteOptimizer2.Radius, 0, _width - 1);
                maxX = Mathf.Clamp((int)(pos.x) + RouteOptimizer2.Radius, 0, _width - 1);
                minY = Mathf.Clamp((int)(pos.y) - RouteOptimizer2.Radius, 0, _height - 1);
                maxY = Mathf.Clamp((int)(pos.y) + RouteOptimizer2.Radius, 0, _height - 1);
                break;
            case GameManager.FitnessFunctions.IntersectionScheme:
                detection = RouteOptimizer3.SpreadDetection(new Vector2(pos.x, pos.y), this);
                minX = Mathf.Clamp((int)(pos.x) - RouteOptimizer3.Radius, 0, _width - 1);
                maxX = Mathf.Clamp((int)(pos.x) + RouteOptimizer3.Radius, 0, _width - 1);
                minY = Mathf.Clamp((int)(pos.y) - RouteOptimizer3.Radius, 0, _height - 1);
                maxY = Mathf.Clamp((int)(pos.y) + RouteOptimizer3.Radius, 0, _height - 1);
                break;
        }
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                _grid[x][y].Detection[id] = 0f;
            }
        }
    }

    // get the detection value for the node for the guard ID
    public float GetDetection(int id, Vector2 pos)
    {
        return _grid[(int)(pos.x)][(int)(pos.y)].Detection[id];
    }

    // show the noise
    public void SetNoise(bool activate, Noise noise)
    {
        _grid[(int)(noise.pos.x)][(int)(noise.pos.y)].Tile.GetComponent<MapTileController>().ModifyNoise(activate, noise.intencity);
    }
}

// exception when the map file format is wrong
[System.Serializable]
public class MapFileException : System.Exception
{
    public StreamReader streamReader;
    public MapFileException(StreamReader sr)
    {
        this.streamReader = sr;
    }
    public MapFileException(string message, StreamReader sr) : base(message)
    {
        this.streamReader = sr;
    }
    public MapFileException(string message, System.Exception inner, StreamReader sr) : base(message, inner)
    {
        this.streamReader = sr;
    }
    protected MapFileException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
