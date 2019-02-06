using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// set the camera to focus on the background so that the image fills all the screen
public class OptimizeBackgroundCamera : MonoBehaviour
{
    public List<Sprite> Sprites;
    public SpriteRenderer BackgroundImage;

    private Camera _camera;

    public void Optimize(float aspect)
    {
        _camera = GetComponent<Camera>();
        _camera.aspect = aspect;

        BackgroundImage.sprite = Sprites[Random.Range(0, Sprites.Count)];

        float imageAspect = BackgroundImage.sprite.bounds.extents.x / BackgroundImage.sprite.bounds.extents.y;

        if(imageAspect > aspect)
        {
            _camera.orthographicSize = BackgroundImage.sprite.bounds.extents.y;
        }
        else
        {
            _camera.orthographicSize = BackgroundImage.sprite.bounds.extents.x / aspect;
        }
    }
}
