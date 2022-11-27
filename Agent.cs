using Assets.Plugins.Smart2DWaypoints.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Behaviours
{
    public Behaviours(string name, Vector3 direction, float units)
    {
        Name = name;
        Direction = direction;
        Units = units;
    }

    public string Name { get; }
    public Vector3 Direction { get; }
    public float Units { get; }
    public override string ToString() => $"{Name} goes {Direction} for {Units} units";
}

public class Agent : MonoBehaviour
{
    List<Behaviours> behaviourList;
    CustomSceneManager csm;

    private void Start()
    {
        behaviourList = new List<Behaviours>();
        //behaviourList.Add(new Behaviours("Jump", Vector3.up, 25));
        //behaviourList.Add(new Behaviours("Crouch", Vector3.down, 10));
        behaviourList.Add(new Behaviours("SolveCode", Vector3.zero, 0));
        csm = CustomSceneManager.Instance;
    }

    // For debug only
    [ContextMenu("Run Tree Game Over")]
    void ContextRunTreeOver()
    {
        GameObject enemy = FindObjectOfType<Obstacle>().gameObject;
        string oname = enemy.name;

        Debug.Log($"Checked {CheckObstacle(oname)}") ;
        ApplyPenalty(oname, enemy, true);
        Debug.Log($"Second Checked {CheckObstacle(oname)}");
        Debug.Log($"Is solved? {IsAlreadySolved(oname)}");
        TrySolution(oname, enemy);
        ApplyPenalty(oname, enemy, false);
        Debug.Log($"Third Checked {CheckObstacle(oname)}");
        Debug.Log($"Is solved? {IsAlreadySolved(oname)}");
        TrySolution(oname, enemy);
    }

    [ContextMenu("Run Tree found solution")]
    void ContextRunTreeSolution()
    {
        GameObject enemy = FindObjectOfType<Obstacle>().gameObject;
        string oname = enemy.name;

        Debug.Log($"First Checked {CheckObstacle(oname)}");
        ApplyPenalty(oname, enemy, true);
        Debug.Log($"Second Checked {CheckObstacle(oname)}");
        Debug.Log($"Is solved? {IsAlreadySolved(oname)}");
        TrySolution(oname, enemy);
        RegisterSuccess(enemy, oname);
        Debug.Log($"Third Checked {CheckObstacle(oname)}");
        Debug.Log($"Is solved? {IsAlreadySolved(oname)}");
        if (IsAlreadySolved(oname))
        {
            Debug.Log("I WIN");
        }
        else
        {
            Debug.Log("Uh oh");
        }
    }

    #region PlayMaker_Checks
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    public bool CheckObstacle(string obstacleType)
    {
        return csm.currentMemory.IsKnownObstacle(obstacleType);
    }

    public bool IsAlreadySolved(string obstacleType)
    {
        return csm.currentMemory.IsAlreadySolved(obstacleType);
    }

    public bool IsObstacleCleared(GameObject obstacle)
    {
        SolveOrDie sod;
        if (obstacle.TryGetComponent<SolveOrDie>(out sod))
        {
            return sod.IsSolved;
        }
        return transform.position.x > obstacle.transform.position.x;
    }
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    #endregion PlayMaker_Checks

    #region PlayMaker_Methods
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    public bool ApplyPenalty(string obstacleType, GameObject obstacle, bool isNew = true)
    {
        if (isNew)
        {
            csm.currentMemory.AddUnsolvedKiller(obstacleType);
        }

        Obstacle obs = (Obstacle) obstacle.GetComponent("Obstacle");
        ExecutePenalty(obs.GetPenalty(), obs);
        return obs.GetPenalty() == Obstacle.PENALTIES.DEATH;
    }

    public void RegisterDeath()
    {
        csm.RestartLevel();
    }

    public void TrySolution(string obstacleType, GameObject obstacle)
    {
        Behaviours currentBehaviour = null;

        string[] alreadyTested = csm.currentMemory.GetAttemptedSolutions(obstacleType);
        foreach (Behaviours b in behaviourList)
        {
            if (alreadyTested.Contains(b.Name))
            {
                Debug.Log($"Already tried {b.Name} to solve this");
                continue;
            }

            currentBehaviour = b;
            break;
        }

        if (currentBehaviour != null)
        {
            Action(obstacleType, currentBehaviour, obstacle);
        }
        else
        {
            Debug.Log($"GAME OVER");
            csm.currentLives = 0;
        }
    }

    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    #endregion PlayMaker_Methods

    #region Private_Methods
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    private void ExecutePenalty(Obstacle.PENALTIES penalty, Obstacle obstacle)
    {
        switch (penalty)
        {
            case Obstacle.PENALTIES.DEATH:
                csm.currentLives--;
                // TODO show message in UI that the agent died
            break;
            case Obstacle.PENALTIES.PUSHED:
                Vector3 screenPosition      = Camera.main.WorldToScreenPoint(obstacle.transform.position);
                Vector3 displacedPosition   = obstacle.GetDirection() * (obstacle.UnitAction() * CustomSceneManager.UNIT_SIZE);
                screenPosition              += displacedPosition; // screen position of the new point
                displacedPosition           = Camera.main.ScreenToWorldPoint(screenPosition); // transform to screen point
                csm.ChangeWaypoints(displacedPosition, obstacle.GetComponent<CustomWaypoint>().Order);
                break;
        }
    }
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    #endregion Private_Methods

    public void RegisterSuccess(GameObject obstacle, string obstacleType)
    {
        string directionName = GetDirectionName(obstacle.transform);
        SolveOrDie sod;
        if (obstacle.TryGetComponent<SolveOrDie>(out sod))
        {
            csm.currentMemory.AddSolution(obstacleType, directionName, sod.SecretCode);
        }
        else
        {
            string behaviour = csm.currentMemory.GetLasAttemptedSolution(obstacleType);
            csm.currentMemory.AddSolution(obstacleType, directionName, behaviour);
        }
    }

    private string GetDirectionName(Transform obstacleTransform)
    {
        switch (obstacleTransform.localEulerAngles.z)
        {
            case 0:
                return "UP";
            case 90:
                return "LEFT";
            case -90:
                return "RIGHT";

        }
        return "DOWN";
    }
    

    public void Action(string obstacleType, Behaviours b, GameObject obstacle)
    {
        // If action is a movement
        if (b.Direction != Vector3.zero)
        {
            float units     = (b.Units * (Camera.main.orthographicSize * 2)) / 100; // units % of the screen
            Vector3 result  = obstacle.transform.position + (b.Direction * units);
            if (obstacle.transform.position.y <= (Camera.main.orthographicSize * b.Units * 2) / 100
                && obstacle.transform.position.y > Camera.main.orthographicSize * -1)
            {
                csm.ChangeWaypoints(result + obstacle.transform.position, obstacle.GetComponent<CustomWaypoint>().Order);
            }

            //TODO set message in UI that crouch or jump were not possible
        }
        // otherwise this baby is a solver
        else
        {
            // slow down the agent 
            gameObject.GetComponent<Follower>().SlowDown();
            string test;
            SolveOrDie currentObstacle = obstacle.GetComponent<SolveOrDie>();
            if (currentObstacle != null)
            {
                while (!currentObstacle.IsSolved)
                {
                    test = csm.GetRandomString();
                    currentObstacle.TryCode(test);
                }
                if (!currentObstacle.IsSolved)
                {
                    csm.currentMemory.AddAttemptedSolution(obstacleType, b.Name);
                }
                gameObject.GetComponent<Follower>().SpeedUp();
            }            
        }        
    }
}
