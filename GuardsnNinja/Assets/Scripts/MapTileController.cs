using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// controller of the map tile GameObject
public class MapTileController : MonoBehaviour
{
    public Text TextField;
    public SpriteRenderer RouteNode;
    public SpriteRenderer EastRoute;
    public SpriteRenderer NorthRoute;
    public SpriteRenderer WestRoute;
    public SpriteRenderer SouthRoute;

    public SpriteRenderer EastBorder;
    public SpriteRenderer NorthBorder;
    public SpriteRenderer WestBorder;
    public SpriteRenderer SouthBorder;

    [HideInInspector]
    public bool IsStartingNode = false;


    private Dictionary<Node.Dir, SpriteRenderer> _routes = new Dictionary<Node.Dir, SpriteRenderer>();
    private Dictionary<Node.Dir, SpriteRenderer> _borders = new Dictionary<Node.Dir, SpriteRenderer>();

    public static Color impassColor = new Color(0.75f, 0f, 0f);
    public static Color percColor = new Color(0f, 0f, 0.75f);
    private GameManager _gm;

    public void Initialize()
    {
        _routes.Add(Node.Dir.East, EastRoute);
        _routes.Add(Node.Dir.North, NorthRoute);
        _routes.Add(Node.Dir.West, WestRoute);
        _routes.Add(Node.Dir.South, SouthRoute);

        _borders.Add(Node.Dir.East, EastBorder);
        _borders.Add(Node.Dir.North, NorthBorder);
        _borders.Add(Node.Dir.West, WestBorder);
        _borders.Add(Node.Dir.South, SouthBorder);

        _gm = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    public void ModifyText(string text)
    {
        if (TextField.text == "")
        {
            TextField.text = text;
        }
        else
        {
            TextField.text += text;
        }
    }

    public void ModifyRoute(Node.Dir direction, Color color)
    {
        if (_routes[direction].color != Color.white)
        {
            if (_routes[direction].color != color)
            {
                _routes[direction].color = Color.gray;
            }
        }
        else
        {
            _routes[direction].enabled = true;
            _routes[direction].color = color;
        }
    }

    public void ModifyPause(Color color)
    {
        if (RouteNode.color != Color.white)
        {
            if (RouteNode.color != color)
            {
                RouteNode.color = Color.gray;
            }
        }
        else
        {
            RouteNode.enabled = true;
            RouteNode.color = color;
        }
    }

    public void ModifyBorder(Node.Dir direction, bool isPass, bool isPerc)
    {
        if (isPass)
        {
            _borders[direction].color = Color.white;
            _borders[direction].enabled = false;
        }
        else if (isPerc)
        {
            _borders[direction].enabled = true;
            _borders[direction].color = percColor;
        }
        else
        {
            _borders[direction].enabled = true;
            _borders[direction].color = impassColor;
        }
        
    }

    public void ClearRoutes()
    {
        for (int i = 0; i < 4; i++)
        {
            _routes[(Node.Dir)(i)].enabled = false;
        }
        RouteNode.enabled = false;
        TextField.gameObject.SetActive(false);
    }

    public void OnMouseDown()
    {
        if (IsStartingNode)
        {
            _gm.SelectStartCell(this);
        }
    }

    public void ModifyNoise(bool activate, float intencity = 0f)
    {
        if (activate)
        {
            RouteNode.enabled = true;
            RouteNode.color = Color.gray;
            RouteNode.gameObject.transform.localScale = new Vector3(intencity * 4f, intencity * 4f, 1f);
        }
        else
        {
            RouteNode.enabled = false;
        }
    }
}
