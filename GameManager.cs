using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;
using System;
using Cinemachine;
using UnityEngine.Rendering.Universal;
using Cinemachine.PostFX;
using Image = UnityEngine.UI.Image;

[System.Serializable]

public enum Mod
{
    Addition,
    Subtraction
}

public class GameManager : MonoBehaviour
{
    #region Variables

    //Camera
    public static Camera MainCamera;
    private static CinemachineVirtualCamera _cinemachineController;
    private static Image _fadeOutImage;
    public static System.Random RandomSystem;
    public static UIManager UImanager;
    public static OverworldManager OverworldManagerInstance;
    public static EventSystemController EventManager;
    private StreamWriter writer;
    private static string _dataFilePath;

    //SaveState Variables
    public static Inventory PlayerInventory = new Inventory();
    public static List<BeastState> PartyBeastStates = new List<BeastState>();
    public static List<BeastState> StoredMons = new List<BeastState>();

    //campaign tracking
    private static int _morale; 
    public static int Morale
    {
        get { return _morale; }
        set { _morale = Math.Clamp(value, 0, 3); } // Clamp the value between 0 and 4
    }

    //booleans
    public static bool PlayingCampaign = false;
    public static bool Playing { get { return (CurrentScene.name != "Main Menu"); } }
    public static Scene CurrentScene { get { return (SceneManager.GetActiveScene()); } }
    public static bool AcceptingInput {  get { return (_fadeOutImage != null && !_fadeOutImage.gameObject.activeInHierarchy); } }

    private static GameObjectPool _particleEmitterPool;
    private static int _particleEmitterMax = 25;

    private static GameObjectPool _projectilePool;
    private static int _projectileMax = 30;

    //static prefab references
    public static GameObject ParticlePrefab;

    #endregion

    public static GameManager GetInstance() { return GameObject.Find("GameManager").GetComponent<GameManager>(); }

    void Update()
    {
        // Existing input handling code
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
 
        if (CurrentScene.name.Contains("Map") && AcceptingInput)
        {
            if (Input.GetKeyUp(KeyCode.Tab) && !BeastSummaryController.Instance.CurrentlyEvolving) UImanager.ToggleInventoryUI();
            // New code for the developer backdoor
            if (Input.GetKeyDown(KeyCode.Alpha0)) GeneralDevTestingBoost();
            if (Input.GetKeyDown(KeyCode.Alpha9)) RestartGame();
            // Check for movement input
            if (vertical > 0) OverworldManager.MoveMons(OverworldManager.Direction.Up);
            else if (vertical < 0) OverworldManager.MoveMons(OverworldManager.Direction.Down);
            else if (horizontal < 0) OverworldManager.MoveMons(OverworldManager.Direction.Left);
            else if (horizontal > 0) OverworldManager.MoveMons(OverworldManager.Direction.Right);
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game!");
        StartCoroutine(FadeSceneOutAndLoadNext("Main Menu"));
        OverworldManager.usePreLoadedMapOnInit = false;
        OverworldManager.ProgressionLevel = 1;
        PlayingCampaign = false;
    }

    #region Setup / Initialization
    private void Awake()
    {
        InitCamera();
        UImanager = ScriptableObject.CreateInstance<UIManager>();
        UImanager.Init();
        RandomSystem = new System.Random();
        _dataFilePath = Path.Combine(Application.persistentDataPath, "playerData.json");
        SetUpDebugLogging();
        LoadGameDataToDexes();
        SetVolumeForScene(CurrentScene.name); //select appropriate post-processing stack based on current scene
        _fadeOutImage = MainCamera.transform.Find("Canvas/FadeOut").gameObject.GetComponent<Image>(); //get fader reference
        ParticlePrefab = Resources.Load<GameObject>("gameObjects/Particle");

        if (Playing)
        {
            LoadSavedData();
            InitParticleEmitterPool();
            if (CurrentScene.name.Contains("Map"))
            {
                Debug.Log("awake - Map");
                OverworldManager.Init();
                EventManager = MainCamera.transform.Find("Canvas/UI/EventUIHub").gameObject.GetComponent<EventSystemController>();
            }

            if (CurrentScene.name.Contains("Battle")) InitProjectilePool();
        }
    }

    private void Start() //handles scene fading -> move to visual handler
    {
        if (Playing)
        {
            if (CurrentScene.name.Contains("Map")) StartCoroutine(FadeSceneIn());
            if (CurrentScene.name.Contains("Battle")) StartCoroutine(FadeSceneIn());
        }
    }

    private static void SetVolumeForScene(string sceneName)
    {
        // Assuming _cinemachineController is already initialized elsewhere
        CinemachineVolumeSettings volumeSettings = _cinemachineController.GetComponent<CinemachineVolumeSettings>();

        if (volumeSettings != null && volumeSettings.m_Profile != null)
        {
            bool depthOfFieldActive = sceneName == "Main Menu";
            ToggleBlur(depthOfFieldActive);
            // Apply changes based on the scene name
            switch (sceneName)
            {
                case "Main Menu":
                    if (volumeSettings.m_Profile.TryGet<ChromaticAberration>(out var chromaticAberrationMainMenu))
                    {
                        chromaticAberrationMainMenu.intensity.value = 0.23f;
                    }
                    if (volumeSettings.m_Profile.TryGet<Bloom>(out var bloomMainMenu))
                    {
                        bloomMainMenu.tint.value = new Color(0.9339f, 0.6027f, 0.4273f);
                    }
                    if (volumeSettings.m_Profile.TryGet<FilmGrain>(out var filmGrainMainMenu))
                    {
                        filmGrainMainMenu.intensity.value = 0.25f; // Example value
                    }
                    break;

                case "WildsMap":
                    if (volumeSettings.m_Profile.TryGet<ChromaticAberration>(out var chromaticAberrationWildsMap))
                    {
                        chromaticAberrationWildsMap.intensity.value = 0.05f;
                    }
                    if (volumeSettings.m_Profile.TryGet<Bloom>(out var bloomWildsMap))
                    {
                        bloomWildsMap.tint.value = new Color(0.9333f, 0.7437f, 0.6421f);
                    }
                    if (volumeSettings.m_Profile.TryGet<FilmGrain>(out var filmGrainWildsMap))
                    {
                        filmGrainWildsMap.intensity.value = 0.15f; // Example value
                    }
                    break;

                case "WildsBattle":
                    if (volumeSettings.m_Profile.TryGet<ChromaticAberration>(out var chromaticAberrationWildsBattle))
                    {
                        chromaticAberrationWildsBattle.intensity.value = 0.05f;
                    }
                    if (volumeSettings.m_Profile.TryGet<Bloom>(out var bloomWildsBattle))
                    {
                        bloomWildsBattle.tint.value = new Color(0.9333f, 0.7437f, 0.6421f);
                    }
                    if (volumeSettings.m_Profile.TryGet<FilmGrain>(out var filmGrainWildsBattle))
                    {
                        filmGrainWildsBattle.intensity.value = 0.15f; // Example value
                    }
                    break;

                default:
                    Debug.LogWarning("Scene name not recognized for volume settings adjustment.");
                    break;
            }
        }
        else
        {
            Debug.LogError("CinemachineVolumeSettings component or profile not found on the virtual camera.");
        }
    }

    public static void ToggleBlur(bool? specifiedState = null)
    {
        // Assuming _cinemachineController is already initialized elsewhere
        CinemachineVolumeSettings volumeSettings = _cinemachineController.GetComponent<CinemachineVolumeSettings>();

        if (volumeSettings != null && volumeSettings.m_Profile != null)
        {
            // Attempt to get the Depth Of Field override
            if (volumeSettings.m_Profile.TryGet<DepthOfField>(out var depthOfField)) depthOfField.active = specifiedState ?? !depthOfField.active;
            else Debug.LogWarning("DepthOfField is not part of the volume profile.");
        }
        else Debug.LogError("CinemachineVolumeSettings component or profile not found on the virtual camera.");
    }

    public static void SetCameraFollowGameObject(GameObject toFollow) { _cinemachineController.Follow = toFollow.transform; }

    private void LoadGameDataToDexes()
    {
        Ability.LoadDex();//AbilityDex must be loaded first
        //blueprint dexes
        BeastBlueprint.LoadDex();
        BeastBlueprint.LoadXPLadder();
        BeastBlueprint.LoadEvolutionLadder();
        ProjectileProfile.LoadDex();
        Item.LoadDex();
    }

    private void SetUpDebugLogging()
    {
        string path = Path.Combine(Application.persistentDataPath, "Log.txt");
        writer = new StreamWriter(path, append: true);
        // Register the HandleLog function to Unity's log events
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Write to the log file
        writer.WriteLine($"{DateTime.Now} - {type}: {logString}");
        writer.Flush(); // Immediately update the file
    }

    void OnDestroy()
    {
        // Unregister the HandleLog function from Unity's log events
        Application.logMessageReceived -= HandleLog;
        // Close the StreamWriter when the object is destroyed
        writer.Close();
    }

    void InitCamera()
    {
        MainCamera = Camera.main;
        _cinemachineController = MainCamera.transform.Find("CM vcam1").gameObject.GetComponent<CinemachineVirtualCamera>();
    }
    #endregion

    #region Scene Updating and Transitioning

    public IEnumerator OverworldToBattleTransition()
    {
        UImanager.EventUIOpen = true;
        CinemachineVolumeSettings volumeSettings = _cinemachineController.GetComponent<CinemachineVolumeSettings>();
        if (volumeSettings.m_Profile.TryGet<ChromaticAberration>(out var chromaticAberrationWildsMap))
        {
            chromaticAberrationWildsMap.active = true;
            chromaticAberrationWildsMap.intensity.value = 0;
        }
        if (volumeSettings.m_Profile.TryGet<LensDistortion>(out var lensDistortionWildsMap))
        {
            lensDistortionWildsMap.active = true;
            lensDistortionWildsMap.intensity.value = 0;
            lensDistortionWildsMap.scale.value = 1;
        }
        
        _fadeOutImage.gameObject.SetActive(true);

        const float transitionDuration = 1f; // Adjust the duration as needed
        float timeElapsed = 0f;

        while (timeElapsed < transitionDuration)
        {
            float progress = timeElapsed / transitionDuration;

            chromaticAberrationWildsMap.intensity.value = Mathf.Lerp(0, 1, progress);
            if (lensDistortionWildsMap.intensity.value>-1) lensDistortionWildsMap.intensity.value = Mathf.Lerp(0, -2, progress);
            else lensDistortionWildsMap.scale.value = Mathf.Lerp(2, 0.01f, progress);
            _fadeOutImage.color = new Color(0,0,0, Mathf.Lerp(0,1,progress));

            yield return null;
            timeElapsed += Time.deltaTime;
        }

        SceneManager.LoadScene("WildsBattle");

        chromaticAberrationWildsMap.intensity.value = 1;
        lensDistortionWildsMap.intensity.value = -1;
        lensDistortionWildsMap.scale.value = 0.01f;

        chromaticAberrationWildsMap.active = false;
        lensDistortionWildsMap.active = false;
        UImanager.EventUIOpen = false;
    }

    public IEnumerator FadeSceneIn()
    {
        Debug.Log("fading in");
        float transitionDuration = 0.45f; // Adjust the duration as needed
        if (CurrentScene.name=="WildsMap") transitionDuration = 0.65f;
        float timeElapsed = 0f;

        while (timeElapsed < transitionDuration)
        { 
            float progress = timeElapsed / transitionDuration;
            _fadeOutImage.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, progress));

            yield return null;
            timeElapsed += Time.deltaTime;
        }

        _fadeOutImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeSceneOutAndLoadNext(string targetScene)
    {
        Debug.Log("attempting fade out!");
        //if (!_fadeOutImage) _fadeOutImage = MainCamera.transform.Find("Canvas/FadeOut").gameObject.GetComponent<Image>();
        _fadeOutImage.gameObject.SetActive(true);

        const float transitionDuration = 0.45f; // Adjust the duration as needed
        float timeElapsed = 0f;

        while (timeElapsed < transitionDuration)
        {
            float progress = timeElapsed / transitionDuration;
            _fadeOutImage.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, progress));

            yield return null;
            timeElapsed += Time.deltaTime;
        }

        SceneManager.LoadScene(targetScene);
    }

    public void UpdateScene(string sceneName)
    {
        //will need to update this to accommodate other wilds scenes
        SaveData();
        if (CurrentScene.name == "WildsMap" && sceneName == "WildsBattle") StartCoroutine(OverworldToBattleTransition());
        else StartCoroutine(FadeSceneOutAndLoadNext(sceneName));
    }
    #endregion

    #region Main Menu - Beast Selector
    public static void SetStartingBeast(BeastState startingBeast)
    {
        PartyBeastStates = new List<BeastState>() { startingBeast };
    }

    public static void ClearStartingBeast()
    {
        PartyBeastStates = new List<BeastState>() { };
    }
    #endregion

    #region pooling functions

    private static void InitParticleEmitterPool()
    {
        _particleEmitterPool = ScriptableObject.CreateInstance<GameObjectPool>();
        _particleEmitterPool.Init(Resources.Load<GameObject>("gameObjects/ParticleEmitter"), _particleEmitterMax);
    }

    public static GameObject GetParticleEmitter() { return _particleEmitterPool.GetPooledObject(); }

    public static void RequeueParticleEmitterToPool(GameObject particleEmitter) { _particleEmitterPool.RequeuePooledObject(particleEmitter); }

    private static void InitProjectilePool()
    {
        _projectilePool = ScriptableObject.CreateInstance<GameObjectPool>();
        _projectilePool.Init(Resources.Load<GameObject>("gameObjects/BattleEffects/2D_Projectile"), _projectileMax);
    }
    public static GameObject GetProjectile() { return _projectilePool.GetPooledObject(); }

    public static void RequeueProjectileToPool(GameObject projectile) { _projectilePool.RequeuePooledObject(projectile); }
    #endregion

    #region Saving and Loading
    public static void SaveData()
    {
        SaveState state = CreateSaveState();
        string jsonData = JsonConvert.SerializeObject(state, Formatting.None,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        Debug.Log(_dataFilePath);
        File.WriteAllText(_dataFilePath, jsonData);
        Debug.Log("Saved Data.");
    }

    static void LoadSavedData()
    {
        SaveState saveState;
        if (File.Exists(_dataFilePath))
        {
            string jsonData = File.ReadAllText(_dataFilePath);
            saveState = JsonConvert.DeserializeObject<SaveState>(jsonData);

        }
        else saveState = new SaveState();

        PartyBeastStates = saveState.PartyMons;
        StoredMons = saveState.StoredMons;
        PlayerInventory = saveState.PlayerInventory;
        Morale = saveState.Morale;
    }

    public static SaveState CreateSaveState()
    {
        SaveState saveState = new SaveState();
        saveState.PartyMons = PartyBeastStates;
        saveState.StoredMons = StoredMons;
        saveState.PlayerInventory = PlayerInventory;
        saveState.Morale = Morale;
        return saveState; 
    }
    #endregion

    #region Misc Use functions
    public static Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        float tx = v.x;
        float ty = v.y;
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    public static int CalculateRenderLayer(float Ypos)
    {
        float inputMin = -45f;
        float inputMax = 126f;
        int outputMin = -32768;
        int outputMax = 32767;

        // calculate scale factor and offset
        float m = (outputMax - outputMin) / (inputMax - inputMin);
        float c = outputMin - m * inputMin;

        // Now you can calculate the output based on an input
        float input = Ypos;
        int output = Mathf.RoundToInt(m * input + c);

        return -output;
    }

    public static bool IsVisible(GameObject gameObject)
    {
        Vector3 viewPos = GameManager.MainCamera.WorldToViewportPoint(gameObject.transform.position);
        return viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1 && viewPos.z >= 0;
    }

    public static Color CalculateAverageColor(Sprite sprite, int targetWidth, int targetHeight)
    {
        Texture2D texture = sprite.texture;
        float alphaThreshold = 0.1f;

        // Make sure the texture is readable
        if (!texture.isReadable)
        {
            // Create a temporary RenderTexture
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            // Copy the texture to the RenderTexture
            Graphics.Blit(texture, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            texture = new Texture2D(texture.width, texture.height);
            texture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            texture.Apply();

            // Release the temporary RenderTexture
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
        }

        // Create a RenderTexture of the target size
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(texture, rt);
        RenderTexture previousRt = RenderTexture.active;
        RenderTexture.active = rt;

        // Create a new Texture2D of the target size
        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight);
        resizedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        resizedTexture.Apply();

        // Calculate average color
        Color[] pixels = resizedTexture.GetPixels();
        Color sum = Color.black;
        int nonTransparentPixelCount = 0;  // Counter for non-transparent pixels
        foreach (Color pixel in pixels)
        {
            if (pixel.a >= alphaThreshold) // Check if the pixel's alpha is above the threshold
            {
                sum += pixel;
                nonTransparentPixelCount++;
            }
        }

        // Cleanup
        RenderTexture.active = previousRt;
        RenderTexture.ReleaseTemporary(rt);
        if (!sprite.texture.isReadable) Destroy(texture);
        Destroy(resizedTexture);

        if (nonTransparentPixelCount == 0) return Color.clear;

        Color averageColor = sum / nonTransparentPixelCount;

        // If the average color's alpha is below a threshold, adjust it
        float alphaAdjustmentThreshold = 0.75f; // or 0.75f based on your needs
        if (averageColor.a < alphaAdjustmentThreshold)
        {
            averageColor.a += (1 - averageColor.a) / 2;
        }

        return averageColor;
    }

    public static Color GetColor(float varScale)
    {
        List<Color> colorLevels = new List<Color>()
    {
        new Color(83/255f, 253/255f, 76/255f),   // Green shades to red shades
        new Color(202/255f, 253/255f, 76/255f),
        new Color(253/255f, 245/255f, 76/255f),
        new Color(253/255f, 149/255f, 76/255f),
        new Color(253/255f, 83/255f, 76/255f)
    };

        // Whiff color for out-of-bounds low value
        Color whiffColor = new Color(155 / 255f, 90 / 255f, 245 / 255f);

        // Check for special cases
        if (varScale == -2)
        {
            return whiffColor;  // Return the whiff color for -2
        }
        else if (varScale == 2)
        {
            return colorLevels[colorLevels.Count - 1];  // Ensure the last color is returned for +2
        }

        // Normalize varScale from -1 to 1 to a scale of 0 to colorLevels.Count - 1
        int colorIndex = Mathf.Clamp((int)Mathf.Round((varScale + 1) / 2 * (colorLevels.Count - 1)), 0, colorLevels.Count - 1);

        return colorLevels[colorIndex];
    }
    #endregion

    #region InGameDevTools

    private void GeneralDevTestingBoost()
    {
        BoostPartyBeasts();
        StockPlayerInventory();
    }

    private void BoostPartyBeasts()
    {
        foreach (var beastState in PartyBeastStates) beastState.StatDict[Stat.Unspent] += 10;
        UImanager.RefreshProfilePanels();
    }

    private void StockPlayerInventory()
    {
        foreach (KeyValuePair<int,Item> entry in Item.FoodDex) PlayerInventory.DepositItem(entry.Value, 99);
        foreach (KeyValuePair<int, Item> entry in Item.HeldDex) PlayerInventory.DepositItem(entry.Value, 99);
        foreach (KeyValuePair<int, Item> entry in Item.ConsumableDex) PlayerInventory.DepositItem(entry.Value, 99);
    }
    #endregion

}


