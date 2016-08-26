#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;
using Random = UnityEngine.Random;

[MoonSharpUserData]
public class WorldController : MonoBehaviour
{
    SoundController soundController;
    TileSpriteController tileSpriteController;
    CharacterSpriteController characterSpriteController;
    JobSpriteController jobSpriteController;
    InventorySpriteController inventorySpriteController;
    FurnitureSpriteController furnitureSpriteController;
    QuestController questController;

    public BuildModeController buildModeController;
    public MouseController mouseController;
    public KeyboardController keyboardController;
    public CameraController cameraController;
    public SpawnInventoryController spawnInventoryController;
    public ModsManager modsManager;

    public static WorldController Instance { get; protected set; }

    // The world and tile data
    public World world { get; protected set; }

    static string loadWorldFromFile = null;

    private bool _isPaused = false;

    public bool IsPaused
    {
        get
        {
            return _isPaused || IsModal;
        }
        set
        {
            _isPaused = value;
        }
    }

    // If true, a modal dialog box is open so normal inputs should be ignored.
    public bool IsModal;

    // Multiplier of Time.deltaTime.
    private float timeScale = 1f;

    public float TimeScale
    {
        get { return timeScale; }
    }

    public bool devMode = false;

    public GameObject inventoryUI;
    public GameObject circleCursorPrefab;

    // Use this for initialization
    void OnEnable()
    {
        string dataPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Data");
        modsManager = new ModsManager(dataPath);

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        if (loadWorldFromFile != null)
        {
            CreateWorldFromSaveFile();
            loadWorldFromFile = null;
        }
        else
        {
            CreateEmptyWorld();
        }

        soundController = new SoundController(world);
    }

    void Start()
    {
        //create gameobject so we can have access to a tranform thats position is Vector3.zero
        GameObject goMat = new GameObject("VisualPath", typeof(VisualPath));
        GameObject go;

        tileSpriteController = new TileSpriteController(world);
        tileSpriteController.Render();
        characterSpriteController = new CharacterSpriteController(world);
        furnitureSpriteController = new FurnitureSpriteController(world);
        jobSpriteController = new JobSpriteController(world, furnitureSpriteController);
        inventorySpriteController = new InventorySpriteController(world, inventoryUI);
        buildModeController = new BuildModeController();
        if(Settings.getSettingAsBool("DevTools_enabled", false))
        {
            spawnInventoryController = new SpawnInventoryController();
        }
        mouseController = new MouseController(buildModeController, furnitureSpriteController, circleCursorPrefab);
        keyboardController = new KeyboardController(buildModeController, Instance);
        questController = new QuestController();
        cameraController = new CameraController();

        //Initialising controllers
        GameObject Controllers = GameObject.Find("Controllers");
        Instantiate(Resources.Load("UIController"), Controllers.transform);

        GameObject Canvas = GameObject.Find("Canvas");
        go = Instantiate(Resources.Load("UI/ContextMenu"),Canvas.transform.position, Canvas.transform.rotation, Canvas.transform) as GameObject;
        go.name = "ContextMenu";



    }

    void Update()
    {
        mouseController.Update(IsModal);
        keyboardController.Update(IsModal);
        cameraController.Update(IsModal);

        if (IsPaused == false)
        {
            world.Update(Time.deltaTime * timeScale);
        }

        questController.Update(Time.deltaTime);
        soundController.Update(Time.deltaTime);
    }

    /// <summary>
    /// Set's game speed (it's a multiplier so 1 == normal game speed).
    /// </summary>
    /// <param name="timeScale">Desired time scale</param>
    public void SetTimeScale(float timeScale)
    {
        this.timeScale = timeScale;
        Debug.Log("Game speed set to " + timeScale + "x");
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates
    /// </summary>
    /// <returns>The tile at world coordinate.</returns>
    /// <param name="coord">Unity World-Space coordinates.</param>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return world.GetTileAt(x, y);
    }

    public void NewWorld()
    {
        Debug.Log("NewWorld button was clicked.");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public string FileSaveBasePath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "Saves");
    }

    public void LoadWorld(string fileName)
    {
        Debug.Log("LoadWorld button was clicked.");

        // Reload the scene to reset all data (and purge old references)
        loadWorldFromFile = fileName;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CreateEmptyWorld()
    {
        // get world size from settings
        int width = Settings.getSettingAsInt("worldWidth", 100);
        int height = Settings.getSettingAsInt("worldHeight", 100);

        // Create a world with Empty tiles
        world = new World(width, height);

        // Center the Camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    void CreateWorldFromSaveFile()
    {
        Debug.Log("CreateWorldFromSaveFile");
        // Create a world from our save file data.

        XmlSerializer serializer = new XmlSerializer(typeof(World));

        // This can throw an exception.
        // TODO: Show a error message to the user.
        string saveGameText = File.ReadAllText(loadWorldFromFile);

        TextReader reader = new StringReader(saveGameText);


        Debug.Log(reader.ToString());
        world = (World)serializer.Deserialize(reader);
        reader.Close();


        // Center the Camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    public void CallTradeShipTest(Furniture landingPad)
    {
        //Currently not using any logic to select a trader
        TraderPrototype prototype = world.traderPrototypes[
            world.traderPrototypes.Keys.ToList()[
                Random.Range(0, world.traderPrototypes.Keys.Count - 1)]];
        Trader trader = prototype.CreateTrader();

        GameObject go = new GameObject(trader.Name);
        go.transform.parent = transform;
        TraderShipController controller = go.AddComponent<TraderShipController>();
        controller.Trader = trader;
        controller.Speed = 5f;
        go.transform.position = new Vector3(-10, 50, 0);
        controller.LandingCoordinates = new Vector3(landingPad.tile.X + 1, landingPad.tile.Y + 1, 0);
        controller.LeavingCoordinates= new Vector3(100, 50, 0);
        go.transform.localScale = new Vector3(1, 1, 1);
        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteManager.current.GetSprite("Trader", "BasicHaulShip");
        spriteRenderer.sortingLayerName = "TradeShip";
    }
}
