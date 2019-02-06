using System;
using System.Collections.Generic;
using UnityEngine;

// map node object
public class Node
{
    public enum Dir { South, North, West, East, None}

    // Cordinates in grid
    private int _x, _y;

    // Current detection value
    private List<float> _detection;

    private Dictionary<Dir, Edge> _edges; // north, south, east, west;

    private GameObject tile;

    public Node(int x, int y)
    {
        _x = x;
        _y = y;
        _detection = new List<float>();

        _edges = new Dictionary<Dir, Edge>();

        for (int i = 0; i < 4; i++)
        {
            _edges.Add((Dir)i, null);
        }
    }

    public GameObject Tile
    {
        get
        {
            return tile;
        }

        set
        {
            tile = value;
            tile.GetComponent<MapTileController>().Initialize();
        }
    }

    public int X
    {
        get
        {
            return _x;
        }
    }

    public int Y
    {
        get
        {
            return _y;
        }
    }

    public Dictionary<Dir, Edge> Edges
    {
        get
        {
            return _edges;
        }

        set
        {
            _edges = value;
        }
    }

    public List<float> Detection
    {
        get
        {
            return _detection;
        }

        set
        {
            _detection = value;
        }
    }

    public Vector2 ToVector2()
    {
        return new Vector2((float)_x, (float)_y);
    }
}
