using Assets.Plugins.Smart2DWaypoints.Scripts;
using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomSceneManager : MonoBehaviour
{
    public const int MAX_LIVES = 9;
    public const int UNIT_SIZE = 36;
    public const int SEPARATION = 200;

    public AgentMemory currentMemory = new AgentMemory();
    public int currentLives = MAX_LIVES;

    [SerializeField] LevelEditorManager levelEditorManager;


    //[SerializeField] Path currentPath;
    [SerializeField] PathMaker currentPath;
    [SerializeField] GameObject agentPrefab;
    [SerializeField] GameObject shovelPrefab;
    [SerializeField] List<GameObject> obstaclesPrefabs = new List<GameObject>();
    [SerializeField] List<GameObject> currentObstaclesSelection = new List<GameObject>();
    [SerializeField] int obstaclesCount;
    [SerializeField] GameObject startWaypoint;
    [SerializeField] GameObject endWaypoint;
    [SerializeField] GameObject floor;
    [SerializeField] bool isPrefabAgent;
    [SerializeField] bool isInConstruction;

    CustomWaypoint[] waypoints;

    #region Singleton
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    // Singleton
    private CustomSceneManager() { } //you never know, I never know
    public static CustomSceneManager Instance { get; private set; }

    private GameObject agentGameObject;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject); // you are never too careful

        currentMemory.Initialize();
        Instance = this;
    }

    // .:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:. //
    #endregion Singleton

    #region LevelManagement
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    void Start()
    {
        isInConstruction = true; 
        RestartLevel();
    }

    public void RestartLevel()
    {
        // TODO: set this visible for the UI
        if (currentLives == 0)
        {
            Debug.Log("Finished, you won");
            return;
        }

        SetRandomObstacleToChoose();

        //StartCoroutine(RebuildScene());
    }

    private void SetRandomObstacleToChoose(int optionCount = 3)
    {
        // Choose the 3 obstacles to put in the scene
        int buttonIndex = 0;
        int addedX = 0;
        levelEditorManager.buttonsArray = new ItemController[optionCount]; 
        levelEditorManager.toolsButtonArray = new ItemController[3];
        levelEditorManager.buttonBase.SetActive(true);
        do
        {
            System.Random random = new System.Random();
            int index = random.Next(obstaclesPrefabs.Count);
            if (!currentObstaclesSelection.Contains(obstaclesPrefabs[index]))
            {
                currentObstaclesSelection.Add(obstaclesPrefabs[index]);
                CreateNewButton(obstaclesPrefabs[index].name, obstaclesPrefabs[index].GetComponent<SpriteRenderer>().sprite, buttonIndex, addedX); 
                
                buttonIndex++;
                               
                addedX += SEPARATION;
            }
        } while (buttonIndex < 3);

        levelEditorManager.ItemImage = levelEditorManager.prefabsArray = currentObstaclesSelection.ToArray();

        // Add the shovel option
        buttonIndex = 0;
        CreateNewButton("Shovel", shovelPrefab.GetComponent<SpriteRenderer>().sprite, buttonIndex, addedX, true);
        buttonIndex++; addedX += SEPARATION;

        // Set the done and clear button
        CreateNewButton("ClearAll", shovelPrefab.GetComponent<SpriteRenderer>().sprite, buttonIndex, addedX, true);
        buttonIndex++; addedX += SEPARATION;
        CreateNewButton("StartGame", shovelPrefab.GetComponent<SpriteRenderer>().sprite, buttonIndex, addedX, true);

        // make invisible the first iteml button
        levelEditorManager.buttonBase.SetActive(false);
    }

    private void CreateNewButton(string goName, Sprite sprite, int index, int addedX, bool isTool = false)
    {
        GameObject go = Instantiate(levelEditorManager.buttonBase, levelEditorManager.buttonBase.transform.position, levelEditorManager.buttonBase.transform.rotation, levelEditorManager.buttonBase.transform.parent);
        go.name = "Button_" + goName;

        Button button = go.GetComponent<Button>();
        button.image.sprite = sprite;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + addedX, rect.anchoredPosition.y);

        SetButtonIntemController(index, go, button, isTool);
    }

    private void SetButtonIntemController(int i, GameObject temp, Button button, bool isTool = false)
    {
        temp.AddComponent<ItemController>();
        ItemController b = temp.GetComponent<ItemController>();
        b.id = i;
        b.quantity = 1;
        b.editor = levelEditorManager;
        b.quantityText = temp.GetComponentInChildren<TextMeshProUGUI>();
        b.AddListener(button);
        if (!isTool)
            levelEditorManager.buttonsArray[i] = b;
        else
            levelEditorManager.toolsButtonArray[i] = b;
    }

    IEnumerator RebuildScene()
    {
        //DestroyAllObjects();
        yield return new WaitForSeconds(2);
        
        GenerateRandomObstacles();
        CreateWaypoints();
        if (isPrefabAgent)
        {
            agentGameObject = GameObject.Instantiate(agentPrefab);
        }
        else
        {
            agentGameObject = agentPrefab;
        }
        Vector3 position = startWaypoint.transform.position;
        position.z = 2;
        agentGameObject.transform.position = position;
        Follower f = agentGameObject.AddComponent<Follower>();
        f.SetPathCreator(currentPath.GetComponent<PathCreator>());
        //f.FollowerPathCreator.bezierPath.AutoControlLength = 0;
    }

    private void DestroyAllObjects()
    {
        if (agentGameObject != null)
        {
            if (isPrefabAgent)
            {
                Destroy(agentGameObject);
            }
            else
            {
                foreach (Follower f in agentGameObject.GetComponents<Follower>())
                {
                    Destroy(f);
                }
            }
        }

        foreach (Obstacle obs in FindObjectsOfType<Obstacle>())
        {
            obs.gameObject.SetActive(false);
            Destroy(obs.gameObject);
        }
    }

    private void GenerateRandomObstacles()
    {
        /*int generated = 0;
        // TODO: this 0.5 needs to be an actual calculation
        float borderEast = endWaypoint.transform.position.x - 0.5f;
        float borderWest = startWaypoint.transform.position.x + 0.5f;
        do
        {
            GameObject choosenPrefab = obstaclesPrefabs[UnityEngine.Random.Range(0, obstaclesPrefabs.Length)];
            Obstacle currentObstacle = (Obstacle) GameObject.Instantiate(choosenPrefab).GetComponent("Obstacle");
            float x = UnityEngine.Random.Range(borderWest, borderEast);
            float y = floor.transform.position.y;
            if (!currentObstacle.IsFloored)
            {
                y = Camera.main.ScreenToWorldPoint(new Vector3(y, UnityEngine.Random.Range(0, Screen.height), 0)).y;
            }

            currentObstacle.gameObject.transform.position = new Vector3(x, y, floor.transform.position.z);
            generated++;
        } while (generated < obstaclesCount);*/
    }
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    #endregion LevelManagement

    #region Waypoints
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    public void CreateWaypoints()
    {
        /*string randomCode = GetRandomString();
        waypoints = GameObject.FindObjectsOfType<CustomWaypoint>();
        Array.Sort(waypoints, delegate (CustomWaypoint x, CustomWaypoint y) {
            return x.WaypointX.CompareTo(y.WaypointX);
        });*/

        List<Transform> tempWaypoints = new List<Transform>();
        tempWaypoints.Add(startWaypoint.transform);
        /*int order = 1;
        for (int i=0; i < waypoints.Length; i++)
        {
            if (waypoints[i].gameObject.Equals(startWaypoint) || waypoints[i].gameObject.Equals(endWaypoint))
                continue;
            waypoints[i].Order = order;
            tempWaypoints.Add(waypoints[i].WaypointTransform);
            order++;
            SolveOrDie sod;
            if (waypoints[i].gameObject.TryGetComponent<SolveOrDie>(out sod))
            {
                sod.SetCode(randomCode);
            }
                
        }*/
        tempWaypoints.Add(endWaypoint.transform);
        currentPath.CreatePath(tempWaypoints.ToArray());
    }

    public void ChangeWaypoints(Vector3 newPos, int position)
    {
        Follower f = agentGameObject.GetComponent<Follower>();

        List<Transform> tempWaypoints = new List<Transform>();
        for (int i = 0; i < waypoints.Length; i++)
        {
            GameObject go = new GameObject();
            go.transform.position = waypoints[i].WaypointTransform.position;
            tempWaypoints.Add(go.transform);
        }

        Vector3 oldPosition = waypoints[position].WaypointTransform.position;
        //tempWaypoints[position].position = oldPosition + newPos;
        tempWaypoints[position].position = newPos;

        currentPath.CreatePath(tempWaypoints.ToArray());
        f.FollowerPathCreator.bezierPath.AutoControlLength = 0.3f;
        f.FollowerPathCreator.bezierPath.NotifyPathModified();
    }
    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    #endregion Waypoints

    //.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:._.:*~*:.
    public string GetRandomString(int stringLength = 2)
    {
        int _stringLength = stringLength - 1;
        string randomString = "";
        string[] characters = new string[] { "a", "b", "c"};
        for (int i = 0; i <= _stringLength; i++)
        {
            randomString = randomString + characters[UnityEngine.Random.Range(0, characters.Length)];
        }
        return randomString;
    }
}
