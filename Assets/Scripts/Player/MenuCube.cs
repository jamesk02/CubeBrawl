using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuCube : MonoBehaviour
{
    [SerializeField]
    private Button cubeTouchButton;

    [SerializeField]
    private float moveSpeed = 25f;
    private float _sensitivity;
    private Vector3 _mouseReference;
    private Vector3 _mouseOffset;
    private Vector3 _rotation;
    private bool _isRotating;

    void Start()
    {
        _sensitivity = 0.4f;
        _rotation = Vector3.zero;
    }

    void Update()
    {
        // this section continuously rotates the object around the y axis
        transform.Rotate(Vector3.down * Time.deltaTime * moveSpeed, Space.World);

        // this section rotates the object when touched
        
        // offset
        _mouseOffset = (Input.mousePosition - _mouseReference);

        // apply rotation
        _rotation.y = -(_mouseOffset.x + _mouseOffset.y) * _sensitivity;

        // rotate
        transform.Rotate(_rotation);

        // store mouse
        _mouseReference = Input.mousePosition;
    }

}