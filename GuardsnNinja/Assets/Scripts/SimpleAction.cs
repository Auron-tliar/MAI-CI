using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// action for the routes
public class SimpleAction
{
    public Node.Dir Direction;

    public SimpleAction(Node.Dir direction = Node.Dir.North)
    {
        Direction = direction;
    }

    public SimpleAction Mutate()
    {
        Node.Dir prev = Direction;
        do
        {
            Direction = (Node.Dir)(Random.Range(0, 4));
        }
        while (Direction == prev);

        return this;
    }

    public SimpleAction Copy()
    {
        return new SimpleAction(Direction);
    }
}
