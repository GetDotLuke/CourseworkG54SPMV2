﻿using UnityEngine;
using System.Collections;

public class MoveSheep : MonoBehaviour
{

    public float speed = 3.0F;
    public float rotationSpeed = 2.0F;
    public int forceConst = 1;

    private bool canJump;
    private Rigidbody selfRigidbody;

    void Start()
    {
        selfRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (canJump)
        {
            canJump = false;
            selfRigidbody.AddForce(0, forceConst, 0, ForceMode.Impulse);
        }
    }
    

    void Update()
    {
        float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        transform.Translate(0, 0, translation);
        transform.Rotate(0, rotation, 0);
        if (Input.GetKeyUp(KeyCode.Space))
        {
            canJump = true;
        }

    }

}