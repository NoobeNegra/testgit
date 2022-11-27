using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{
    [SerializeField] float speed = 1;

    PathCreator myPathCreator;
    float distanceTravelled;

    public PathCreator FollowerPathCreator => myPathCreator;

    void Start()
    {
        if (myPathCreator != null)
        {
            myPathCreator.pathUpdated += OnPathChanged;
        }

    }

    void Update()
    {
        if (myPathCreator != null)
        {
            distanceTravelled += speed * Time.deltaTime;
            transform.position = myPathCreator.path.GetPointAtDistance(distanceTravelled, EndOfPathInstruction.Stop);
        }
    }

    void OnPathChanged()
    {
        distanceTravelled = myPathCreator.path.GetClosestDistanceAlongPath(transform.position);
    }

    internal void SetPathCreator(PathCreator pathCreator)
    {
        myPathCreator = pathCreator;
    }

    internal void SlowDown()
    {
        speed = 0.5f;
    }

    internal void SpeedUp()
    {
        speed *= 2;
    }
}
