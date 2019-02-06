using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Calculation of the camera boundaries and updated camera movements to stay within them
public class CameraBoundaries : MonoBehaviour
{

    public float BottomBoundary;
    public float TopBoundary;
    public float LeftBoundary;
    public float RightBoundary;

    public Camera BackgroundCamera;

    private GameManager _gameManager;

    private Camera _camera;
    private float _verticalExtent;
    private float _horizontalExtent;

    private float _leftBound;
    private float _rightBound;
    private float _topBound;
    private float _bottomBound;

    private float x, y;

    private void Awake()
    {
        _gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        _camera = GetComponent<Camera>();
        _verticalExtent = _camera.orthographicSize;
        _horizontalExtent = _camera.aspect * _verticalExtent;
        
        Vector4 temp = CalculateBoundaries(_verticalExtent, _horizontalExtent);
        _leftBound = temp.x;
        _rightBound = temp.y;
        _bottomBound = temp.z;
        _topBound = temp.w;

        BackgroundCamera.GetComponent<OptimizeBackgroundCamera>().Optimize(_camera.aspect);
    }

    public void Reinitialize()
    {
        _gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        _camera = GetComponent<Camera>();
        _verticalExtent = _camera.orthographicSize;
        _horizontalExtent = _camera.aspect * _verticalExtent;
        
        Vector4 temp = CalculateBoundaries(_verticalExtent, _horizontalExtent);
        _leftBound = temp.x;
        _rightBound = temp.y;
        _bottomBound = temp.z;
        _topBound = temp.w;
    }

    private void LateUpdate()
    {
        
        x = Mathf.Clamp(_camera.transform.position.x, _leftBound, _rightBound);
        y = Mathf.Clamp(_camera.transform.position.y, _bottomBound, _topBound);
        _camera.transform.position = new Vector3(x, y, _camera.transform.position.z);
    }

    private Vector4 CalculateBoundaries(float vExtent, float hExtent)
    {
        float left = LeftBoundary + hExtent;
        float right = RightBoundary - hExtent;
        float top = TopBoundary - vExtent;
        float bottom = BottomBoundary + vExtent;

        if (RightBoundary - LeftBoundary < hExtent * 2)
        {
            left = (LeftBoundary + RightBoundary) / 2;
            right = (LeftBoundary + RightBoundary) / 2;
        }

        if (TopBoundary - BottomBoundary < vExtent * 2)
        {
            top = (TopBoundary + BottomBoundary) / 2;
            bottom = (TopBoundary + BottomBoundary) / 2;
        }

        return new Vector4(left, right, bottom, top);
    }
}
