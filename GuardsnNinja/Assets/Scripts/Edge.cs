using System;
using System.Collections.Generic;

// graph edge
public class Edge
{
    private Node _node1, _node2;

    private bool _isPassable, _isPerceptible;

    public bool _check = false;

    public Edge(Node n1, Node n2, bool southToNorth = true, bool isPass = true, bool isPerc = true)
    // south to north or west to east
    {
        _node1 = n1;
        _node2 = n2;

        if (southToNorth)
        {
            if (n1 != null)
            {
                n1.Edges[Node.Dir.North] = this;
            }
            if (n2 != null)
            {
                n2.Edges[Node.Dir.South] = this;
            }
                
        }
        else
        {
            if (n1 != null)
            {
                n1.Edges[Node.Dir.East] = this;
            }
            if (n2 != null)
            {
                n2.Edges[Node.Dir.West] = this;
            }
        }

        _isPassable = isPass;
        _isPerceptible = isPerc;
    }

    public Node Node1 { get { return _node1; } }
    public Node Node2 { get { return _node2; } }
    public bool IsPassable { get { return _isPassable; } }
    public bool IsPerceptible { get { return _isPerceptible; } }

    public override string ToString()
    {
        if (IsPassable)
        {
            return "+";
        }
        else if (IsPerceptible)
        {
            return "=";
        }
        else
        {
            return "x";
        }
    }
}
