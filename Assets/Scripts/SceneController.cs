using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RendererUtils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MIConvexHull;
using TMPro;
using SFB;

public class SceneController : MonoBehaviour
{
    [HideInInspector] public TextureMode activeViewMode = TextureMode.Real;
    private Vector2 lastMousePosition;

    [Header("Model Shaders")]
    public Shader shd_Scene;
    public Shader shd_Anomaly;
    public Shader shd_Fog;
    public Shader shd_Depth;

    [Header("Compute Shaders")]
    public ComputeShader createFrame;
    public ComputeShader frameDiff;
    public ComputeShader anomalyMask;
    public ComputeShader packEventBits;
    private int kernelHandleCreateFrame, kernelHandleDiff, kernelHandleAnom, kernelHandlePrepareData;

    [Header("Rendering Logic")]
    public GameObject renderQuadScene;
    public GameObject renderQuadAnomaly;
    public Camera mainCamera, renderCamera, depthCamera, anomalyCamera, skyboxCamera;
    public SpawnableObj spawnableObj;
    public TMP_InputField seedInput;
    public TMP_InputField spikeThresholdInput;
    public TMP_InputField fuzzinessInput;
    public Slider fuzzinessSlider;
    public Toggle useGPUToggle;
    public Toggle useAAToggle;
    public Toggle cleanClippingToggle;
    public TMP_InputField minNumObjectsInput;
    public TMP_InputField maxNumObjectsInput;
    [HideInInspector] public float fuzziness;
    [HideInInspector] public bool cleanClipping;
    private bool useGPU, useAA;

    [Header("Objects")]
    public GameObject mdl_Cube;
    public GameObject mdl_Sphere;
    public GameObject mdl_Icosphere;
    public GameObject mdl_Cylinder;
    public GameObject mdl_Pyramid;
    public GameObject mdl_Cone;
    public GameObject mdl_Capsule;
    public GameObject mdl_Torus;
    public GameObject mdl_LBlock;
    public GameObject mdl_TBlock;
    public GameObject mdl_Teapot;
    public GameObject mdl_Suzanne;
    [HideInInspector] public Mesh[] meshes;
    private ObjectPool<SpawnableObj> objectPool;
    private List<SpawnableObj> spawnedObjects;

    [Header("Object Weights")]
    public Slider[] weightSliders;
    public TMP_InputField[] weightInputs;
    [HideInInspector] public float[] shapeWeights;

    [Header("Object Parameters")]
    public Canvas uiCanvas;
    public GameObject uiObjectParamMenu;
    public RectTransform lhPanelRect;
    public RectTransform rhPanelRect;
    private GameObject[] editParamMenus;
    [HideInInspector] public ShapeParamData[] shapeParamData, shapeParamDataBuffer;
    [HideInInspector] public ShapeParamData shapeParamDataClipboard;
    [HideInInspector] public bool dataInClipboard;
    [HideInInspector] public RectTransform selectedMenuRect;
    [HideInInspector] public int numMenusOpen = 0;
    [HideInInspector] public int menuSortCount = 0;

    [Header("Simulation Settings")]
    public TMP_InputField durationInput;
    public TMP_InputField timeScaleInput;
    public TMP_InputField textureWidthInput;
    public TMP_InputField textureHeightInput;
    public Slider fovSlider;
    public TMP_InputField fovInput;
    private float fov;
    private int seed;
    [HideInInspector] public Vector3 skyboxSeed;
    private int frame = 0;
    private int numFrames;
    private int textureWidth, textureHeight, pixelCount;
    private float duration, spikeThreshold;
    [HideInInspector] public float timeScale;
    private int timestamp;
    private float elapsedTime;
    private readonly int maxNumObjectsHard = 1024;
    private int minNumObjects, maxNumObjects;
    private int numObjectsInScene;
    [HideInInspector] public bool[] reboundOnEdges;

    [Header("Depth Settings")]
    public Slider sceneDepthSlider;
    public TMP_InputField sceneDepthInput;
    public Slider fogDepthSlider;
    public TMP_InputField fogDepthInput;
    public Slider bgNoiseSlider;
    public TMP_InputField bgNoiseInput;
    public Toggle reboundNegXToggle;
    public Toggle reboundPosXToggle;
    public Toggle reboundNegYToggle;
    public Toggle reboundPosYToggle;
    public Toggle reboundNegZToggle;
    public Toggle reboundPosZToggle;
    public float zCloseSpawnBuffer = 5;  // Set in inspector
    public float zFarSpawnBuffer = 25;  // Set in inspector
    public float minSceneDepth = 25;  // Set in inspector
    public float maxSceneDepth = 100;  // Set in inspector
    public float cameraClipDistance = -0.5f;  // Set in inspector
    public float cameraCloseDissolveDistance = 3;  // Set in inspector
    [HideInInspector] public float sceneDepth;
    [HideInInspector] public float fogDepthNorm, fogDepth, fogStart;
    private float bgNoiseAmt;
    [HideInInspector] public float zCameraClip, zCameraDissolve, zCloseBoundary, zFarBoundary, zSpawnBoundary;

    [Header("Default Buttons")]
    public Button timeDefButton;
    public Button cameraDefButton;
    public Button renderingDefButton;
    public Button depthDefButton;
    public Button reboundDefButton;
    public Button anomaliesDefButton;

    [Header("Other Buttons")]
    public Button weightsZeroButton;
    public Button weightsRandomButton;
    public Button realButton;
    public Button spikesButton;
    public Button applyButton;
    public Button restartButton;
    public List<Button> editObjectParamButtons;
    public Button closeAllMenusBtn;
    private bool changesMade = false;

    [Header("Include Toggles")]
    public List<Toggle> includeRotationTgls;
    public List<Toggle> includeTranslationTgls;
    public List<Toggle> includeScaleTgls;
    public List<Toggle> includeInitRotationTgls;
    public List<Toggle> includeInitPositionTgls;
    public Toggle includeSurfaceNoiseTgl;
    [HideInInspector] public bool[] includeShapeParam;

    [Header("UI Readouts")]
    public TMP_Text fpsReadout;
    public TMP_Text timeReadout;
    public TMP_Text frameReadout;
    public TMP_Text numObjectsReadout;

    [Header("Anomaly Handling")]
    public TMP_InputField pThresholdInput;
    public Slider pThresholdSlider;
    [HideInInspector] public float pThreshold;

    [Header("Export")]
    public Button exportButton;
    public Button cancelExportButton;
    public TMP_InputField numSimulationsInput;
    public Toggle includeRealViewToggle;
    public Toggle includeSpikesViewToggle;
    public Toggle includeAnomalyMaskToggle;
    public GameObject exportPopup;
    public TMP_Text exportPopupText;
    private bool isExporting = false;
    private int exportCount;
    private List<(uint, uint)> events, anomalies;
    private RenderTexture depthTexture, fogDepthTexture, rawViewTexture, rawViewAATexture, frameTexture, lastFrameTexture, differenceTexture, skyboxTexture;
    private RenderTexture anomalyTexture, lastAnomalyTexture, anomalyMaskTexture;
    private DirectoryInfo rootFolder;
    [HideInInspector] public System.Random rng;
    [HideInInspector] public System.Random rngObjectCount;
    [HideInInspector] public System.Random rngTransform;

    void Start()
    {
        Application.targetFrameRate = 60;
        Input.simulateMouseWithTouches = true; // For touch devices

        kernelHandleCreateFrame = createFrame.FindKernel("CSMain");
        kernelHandleDiff = frameDiff.FindKernel("CSMain");
        kernelHandleAnom = anomalyMask.FindKernel("CSMain");
        kernelHandlePrepareData = packEventBits.FindKernel("CSMain");

        reboundOnEdges = new bool[6];

        // Init shape parameters
        int count = (int)ShapeParamLabels.COUNT;
        shapeParamData = new ShapeParamData[count];
        shapeParamDataBuffer = new ShapeParamData[count];
        for (int i = 0; i < count; i++)
        {
            shapeParamData[i] = new ShapeParamData(i == (int)ShapeParamLabels.GLOBAL);
            shapeParamDataBuffer[i] = new ShapeParamData(i == (int)ShapeParamLabels.GLOBAL);
        }
        shapeParamDataClipboard = new ShapeParamData();
        editParamMenus = new GameObject[count];

        count = (int)ShapeParams.COUNT;
        includeShapeParam = new bool[count];
        for (int i = 0; i < count; i++)
        {
            includeShapeParam[i] = false;
        }

        InitializeUIButtons();
        InitializeUIWeightControls();
        AddChangesMadeListener();

        minNumObjectsInput.text = "1";
        maxNumObjectsInput.text = "1";

        sceneDepthSlider.minValue = minSceneDepth;
        sceneDepthSlider.maxValue = maxSceneDepth;
        Utils.BindSliderToInputField(pThresholdSlider, pThresholdInput);
        Utils.BindSliderToInputField(fuzzinessSlider, fuzzinessInput);
        Utils.BindSliderToInputField(sceneDepthSlider, sceneDepthInput);
        Utils.BindSliderToInputField(fogDepthSlider, fogDepthInput);
        Utils.BindSliderToInputField(bgNoiseSlider, bgNoiseInput);
        Utils.BindSliderToInputField(fovSlider, fovInput);

        Utils.SetupTextInputFormatting(seedInput, "", 8, -9999999, 9999999);
        Utils.SetupTextInputFormatting(timeScaleInput, "", 5, 0.001f, 100);
        Utils.SetupTextInputFormatting(durationInput, "", 6, 1, 999999);
        Utils.SetupTextInputFormatting(textureWidthInput, "", 4, 32, 4096);
        Utils.SetupTextInputFormatting(textureHeightInput, "", 4, 32, 4096);
        Utils.SetupTextInputFormatting(spikeThresholdInput, "", 6, 0, 0.5f);
        Utils.SetupTextInputFormatting(numSimulationsInput, "", 5, 1, 99999);
        Utils.SetupTextInputFormatting(minNumObjectsInput, "", 4, 1, maxNumObjectsHard);
        Utils.SetupTextInputFormatting(maxNumObjectsInput, "", 4, 1, maxNumObjectsHard);

        CreateMeshes();
        seedInput.text = "0";
        rngTransform = new System.Random(0);
        rngObjectCount = new System.Random(0);
        rng = new System.Random(0);
        objectPool = new ObjectPool<SpawnableObj>(spawnableObj, 1, this.gameObject.transform);  // Expand pool as more objects are added
        spawnedObjects = new List<SpawnableObj>();

        DefaultSettingsLHS();
        ApplySettings();
    }

    void InitializeUIButtons()
    {
        applyButton.onClick.AddListener(NewScene);
        restartButton.onClick.AddListener(ResetSimulation);
        realButton.onClick.AddListener(() => SetRenderQuadTextures(TextureMode.Real));
        spikesButton.onClick.AddListener(() => SetRenderQuadTextures(TextureMode.Spikes));
        weightsZeroButton.onClick.AddListener(ZeroShapeWeightSliders);
        weightsRandomButton.onClick.AddListener(RandomShapeWeightSliders);
        exportButton.onClick.AddListener(StartExport);
        cancelExportButton.onClick.AddListener(CancelExport);

        timeDefButton.onClick.AddListener(TimeDefaults);
        cameraDefButton.onClick.AddListener(CameraDefaults);
        renderingDefButton.onClick.AddListener(RenderingDefaults);
        depthDefButton.onClick.AddListener(DepthDefaults);
        reboundDefButton.onClick.AddListener(ReboundDefaults);
        anomaliesDefButton.onClick.AddListener(AnomalyDefaults);

        editObjectParamButtons[(int)ShapeParamLabels.CUBE].onClick.AddListener(BtnEditShapeParamsCube);
        editObjectParamButtons[(int)ShapeParamLabels.SPHERE].onClick.AddListener(BtnEditShapeParamsSphere);
        editObjectParamButtons[(int)ShapeParamLabels.ICOSPHERE].onClick.AddListener(BtnEditShapeParamsIcosphere);
        editObjectParamButtons[(int)ShapeParamLabels.CYLINDER].onClick.AddListener(BtnEditShapeParamsCylinder);
        editObjectParamButtons[(int)ShapeParamLabels.PYRAMID].onClick.AddListener(BtnEditShapeParamsPyramid);
        editObjectParamButtons[(int)ShapeParamLabels.CONE].onClick.AddListener(BtnEditShapeParamsCone);
        editObjectParamButtons[(int)ShapeParamLabels.CAPSULE].onClick.AddListener(BtnEditShapeParamsCapsule);
        editObjectParamButtons[(int)ShapeParamLabels.TORUS].onClick.AddListener(BtnEditShapeParamsTorus);
        editObjectParamButtons[(int)ShapeParamLabels.L_BLOCK].onClick.AddListener(BtnEditShapeParamsLBlock);
        editObjectParamButtons[(int)ShapeParamLabels.T_BLOCK].onClick.AddListener(BtnEditShapeParamsTBlock);
        editObjectParamButtons[(int)ShapeParamLabels.TEAPOT].onClick.AddListener(BtnEditShapeParamsTeapot);
        editObjectParamButtons[(int)ShapeParamLabels.SUZANNE].onClick.AddListener(BtnEditShapeParamsSuzanne);
        editObjectParamButtons[(int)ShapeParamLabels.GLOBAL].onClick.AddListener(BtnEditShapeParamsGlobal);
        closeAllMenusBtn.onClick.AddListener(BtnCloseAllEditMenus);
    }

    void InitializeUIWeightControls()
    {
        for (int i = 0; i < (int)Shapes.COUNT; i++)
        {
            Utils.BindSliderToInputField(weightSliders[i], weightInputs[i]);
        }
        ZeroShapeWeightSliders();
        weightSliders[(int)Shapes.CUBE].value = 1;
    }

    public void SetChangesMadeFlag()
    {
        changesMade = true;
    }

    void AddChangesMadeListener()
    {
        TMP_InputField[] inputFields = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_InputField inputField in inputFields)
        {
            if (inputField.gameObject.tag == "UI_IgnoreChangesMade")
                continue;
            inputField.onValueChanged.AddListener(delegate { SetChangesMadeFlag(); });
        }

        Toggle[] toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Toggle toggle in toggles)
        {
            if (toggle.gameObject.tag == "UI_IgnoreChangesMade")
                continue;
            toggle.onValueChanged.AddListener(delegate { SetChangesMadeFlag(); });
        }
    }

    void StartExport()
    {
        ExtensionFilter[] extensions = new[] {
            new ExtensionFilter("ANTShapes", "")
        };

        string rootPath = StandaloneFileBrowser.SaveFilePanel("Export Simulated Data", "", "", extensions);

        // Cancelled
        if (rootPath == "")
            return;

        isExporting = true;
        restartButton.interactable = false;
        exportButton.gameObject.SetActive(false);
        cancelExportButton.gameObject.SetActive(true);
        exportCount = 0;
        events = new List<(uint, uint)>();
        anomalies = new List<(uint, uint)>();

        // Force out of spike view (because the "exporting" GUI message is a bit unreadable otherwise.  That's it that's the reason)
        activeViewMode = TextureMode.Real;
        SetRenderQuadTextures(TextureMode.Real);

        seed = 0;
        rootFolder = TryCreateFolder(rootPath, "");
        TryCreateFolder(rootPath, $"{seed}");
        if (includeAnomalyMaskToggle.isOn)
            TryCreateFolder($"{rootPath}/{seed}/", "label");
        if (includeRealViewToggle.isOn)
            TryCreateFolder($"{rootPath}/{seed}/", "view_real");
        if (includeSpikesViewToggle.isOn)
            TryCreateFolder($"{rootPath}/{seed}/", "view_spikes");

        Application.targetFrameRate = -1;  // Unlimit FPS for rendering

        ResetSimulation();
        int numSimulations;
        int.TryParse(numSimulationsInput.text, out numSimulations);
        exportPopupText.text = $"Exporting Dataset\n1 / {numSimulations}";
        exportPopup.SetActive(true);
    }

    void FinishedExport()
    {
        Application.targetFrameRate = 60;  // Relimit FPS
        isExporting = false;
        exportButton.gameObject.SetActive(true);
        restartButton.interactable = true;
        cancelExportButton.gameObject.SetActive(false);
        int.TryParse(seedInput.text, out seed);
        exportPopup.SetActive(false);
    }

    void CancelExport()
    {
        FinishedExport();
        ResetSimulation();
    }

    DirectoryInfo TryCreateFolder(string rootPath, string folderName)
    {
        string fullPath = $"{rootPath}/{folderName}";

        DirectoryInfo folder = new DirectoryInfo(fullPath);
        if (!folder.Exists)
            folder.Create();

        return folder;
    }

    void SaveEvents()
    {
        SavePackedUInts(events, $"{rootFolder.FullName}/{seed}/events.bin");
    }

    void SaveAnomalies()
    {
        SavePackedUInts(anomalies, $"{rootFolder.FullName}/{seed}/anomalies.bin");
    }

    static void SavePackedUInts(List<(uint low, uint high)> entries, string filePath)
    {
        List<byte> output = new List<byte>();

        foreach (var (low, high) in entries)
        {
            // Pack two 32-bit uints into 56 bits (7 bytes)
            ulong packed = ((ulong)low | (ulong)(high & 0xFFFFFFFF) << 32);
            Debug.Log($"low {low.ToString("X")}, high {high.ToString("X")}, packed {packed.ToString("X")}");

            byte[] sevenBytes = new byte[7];

            // Big-endian: most significant byte first
            for (int i = 6; i >= 0; i--)
            {
                sevenBytes[i] = (byte)(packed & 0xFF);
                packed >>= 8;
            }

            output.AddRange(sevenBytes);
        }

        File.WriteAllBytes(filePath, output.ToArray());
    }

    void UpdateScene()
    {
        if (isExporting)
        {
            applyButton.interactable = false;
        }
        else
        {
            applyButton.interactable = changesMade;
        }

        skyboxCamera.Render();

        RenderCamera(depthCamera);
        RenderCamera(renderCamera);
        RenderCamera(anomalyCamera);

        UpdateAllObjects();

        if (useGPU)
        {
            DispatchCreateFrameComputeShader();
            DispatchFrameDiffComputeShader();
            DispatchAnomalyMaskComputeShader();
        }
        else
        {
            FrameDifferenceCPU.ComputeFrameDifference(frameTexture, lastFrameTexture, differenceTexture, spikeThreshold);
            AnomalyMaskCPU.CreateAnomalyMask(anomalyTexture, lastAnomalyTexture, anomalyMaskTexture);
        }
        Graphics.Blit(frameTexture, lastFrameTexture);
        Graphics.Blit(anomalyTexture, lastAnomalyTexture);

        if (isExporting)
        {
            DispatchPrepareDataComputeShader();
            if (includeAnomalyMaskToggle.isOn)
                ExportPNG(anomalyMaskTexture, $"{rootFolder}/{seed}/label/{frame}.png");
            if (includeRealViewToggle.isOn)
                ExportPNG(frameTexture, $"{rootFolder}/{seed}/view_real/{frame}.png");
            if (includeSpikesViewToggle.isOn)
                ExportPNG(differenceTexture, $"{rootFolder}/{seed}/view_spikes/{frame}.png");
        }

        frame++;
    }

    void ExportPNG(RenderTexture rt, string path)
    {
        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
        Destroy(tex);
    }

    void Update()
    {
        if (frame >= numFrames)
        {
            if (isExporting)
            {
                SaveEvents();
                SaveAnomalies();
                events = new List<(uint, uint)>();
                anomalies = new List<(uint, uint)>();

                int numSimulations;
                int.TryParse(numSimulationsInput.text, out numSimulations);

                exportCount++;
                exportPopupText.text = $"Exporting Dataset\n{exportCount + 1} / {numSimulations}";

                if (exportCount >= numSimulations)
                {
                    FinishedExport();
                }
                else
                {
                    seed++;
                    string rootPath = rootFolder.FullName;
                    TryCreateFolder(rootPath, $"{seed}");
                    if (includeAnomalyMaskToggle.isOn)
                        TryCreateFolder($"{rootPath}/{seed}/", "label");
                    if (includeRealViewToggle.isOn)
                        TryCreateFolder($"{rootPath}/{seed}/", "view_real");
                    if (includeSpikesViewToggle.isOn)
                        TryCreateFolder($"{rootPath}/{seed}/", "view_spikes");
                }
            }

            ResetSimulation();
        }

        UpdateScene();
        UpdateUIReadouts();
        UpdateDraggingMenu();
    }

    void RenderCamera(Camera camera)
    {
        Shader shaderToUse;

        if (camera.tag == "SceneCamera")
        {
            if (useAA)
            {
                SetObjectShadersForRender(shd_Scene);

                // Render AA texture
                SetAntiAliasing(true);
                camera.targetTexture = rawViewAATexture;
                camera.Render();

                // Render non-AA texture
                SetAntiAliasing(false);
                camera.targetTexture = rawViewTexture;
                camera.Render();

                return;
            }
            else
                shaderToUse = shd_Scene;
        }
        else if (camera.tag == "AnomalyCamera")
            shaderToUse = shd_Anomaly;
        else if (camera.tag == "DepthCamera")
        {
            // Render depth texture
            SetObjectShadersForRender(shd_Depth);
            camera.targetTexture = depthTexture;
            camera.Render();

            // Render fog depth texture
            SetObjectShadersForRender(shd_Fog);
            camera.targetTexture = fogDepthTexture;
            camera.Render();

            return;
        }
        else
            return;

        SetObjectShadersForRender(shaderToUse);

        camera.Render();
    }

    void SetObjectShadersForRender(Shader shaderToUse)
    {
        // Get all active objects
        SpawnableObj[] allSpawnableObjs = gameObject.transform.GetComponentsInChildren<SpawnableObj>();

        foreach (var o in allSpawnableObjs)
        {
            o.meshRenderer.material.shader = shaderToUse;
            o.SetShader();
        }
    }

    void SetAntiAliasing(bool state)
    {
        UniversalAdditionalCameraData uacm = renderCamera.GetComponent<UniversalAdditionalCameraData>();

        if (state)
        {
            uacm.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            uacm.antialiasingQuality = AntialiasingQuality.High;
        }
        else
            uacm.antialiasing = AntialiasingMode.None;
    }

    void UpdateAllObjects()
    {
        for (int i = 0; i < numObjectsInScene; i++)
        {
            spawnedObjects[i].UpdateFrame();
        }
    }

    void CreateMeshes()
    {
        int numShapes = (int)Shapes.COUNT;
        meshes = new Mesh[numShapes];

        for (int i = 0; i < numShapes; i++)
        {
            meshes[i] = RescaleMesh(GetPrimitiveMesh((Shapes)i));
        }
    }

    public float[] GetNextRandomValues(int count, System.Random random)
    {
        float[] results = new float[count];
        for (int i = 0; i < count; i++)
        {
            results[i] = (float)random.NextDouble(); // Generate value between 0 and 1
        }
        return results;
    }

    void DefaultSettingsLHS()
    {
        TimeDefaults();
        CameraDefaults();
        RenderingDefaults();
        DepthDefaults();
        ReboundDefaults();
        AnomalyDefaults();
        // Don't set seed, useGPU toggle or export settings here
    }

    void TimeDefaults()
    {
        timeScaleInput.text = "1";
        durationInput.text = "100";
    }

    void CameraDefaults()
    {
        textureWidthInput.text = "1280";
        textureHeightInput.text = "720";
        fovSlider.value = 70f;
    }

    void RenderingDefaults()
    {
        spikeThresholdInput.text = "0.0085";
        useAAToggle.isOn = true;
        cleanClippingToggle.isOn = false;
    }

    void DepthDefaults()
    {
        sceneDepthSlider.value = 25f;
        fogDepthSlider.value = 0;
        bgNoiseSlider.value = 0;
    }

    void ReboundDefaults()
    {
        reboundNegXToggle.isOn = false;
        reboundPosXToggle.isOn = false;
        reboundNegYToggle.isOn = false;
        reboundPosYToggle.isOn = false;
        reboundNegZToggle.isOn = true;
        reboundPosZToggle.isOn = false;
    }

    void AnomalyDefaults()
    {
        pThresholdSlider.value = 0.01f;
        fuzzinessSlider.value = 0.5f;

        for (int i = 0; i < 3; i++)
        {
            includeRotationTgls[i].isOn = true;
            includeTranslationTgls[i].isOn = true;
            includeScaleTgls[i].isOn = true;
            includeInitRotationTgls[i].isOn = false;
            includeInitPositionTgls[i].isOn = false;
        }
        includeSurfaceNoiseTgl.isOn = false;
    }

    public bool ShouldIncludeShapeParamInAnomaly(ShapeParams param)
    {
        return includeShapeParam[(int)param];
    }

    void ZeroShapeWeightSliders()
    {
        for (int i = 0; i < (int)Shapes.COUNT; i++)
        {
            weightSliders[i].value = 0;
        }
    }

    void RandomShapeWeightSliders()
    {
        for (int i = 0; i < (int)Shapes.COUNT; i++)
        {
            weightSliders[i].value = (float)rng.NextDouble();
        }
    }

    void BtnEditShapeParamsCube()
    {
        BtnEditShapeParams(ShapeParamLabels.CUBE);
    }

    void BtnEditShapeParamsSphere()
    {
        BtnEditShapeParams(ShapeParamLabels.SPHERE);
    }

    void BtnEditShapeParamsIcosphere()
    {
        BtnEditShapeParams(ShapeParamLabels.ICOSPHERE);
    }

    void BtnEditShapeParamsCylinder()
    {
        BtnEditShapeParams(ShapeParamLabels.CYLINDER);
    }

    void BtnEditShapeParamsPyramid()
    {
        BtnEditShapeParams(ShapeParamLabels.PYRAMID);
    }

    void BtnEditShapeParamsCone()
    {
        BtnEditShapeParams(ShapeParamLabels.CONE);
    }

    void BtnEditShapeParamsCapsule()
    {
        BtnEditShapeParams(ShapeParamLabels.CAPSULE);
    }

    void BtnEditShapeParamsTorus()
    {
        BtnEditShapeParams(ShapeParamLabels.TORUS);
    }

    void BtnEditShapeParamsLBlock()
    {
        BtnEditShapeParams(ShapeParamLabels.L_BLOCK);
    }

    void BtnEditShapeParamsTBlock()
    {
        BtnEditShapeParams(ShapeParamLabels.T_BLOCK);
    }

    void BtnEditShapeParamsTeapot()
    {
        BtnEditShapeParams(ShapeParamLabels.TEAPOT);
    }

    void BtnEditShapeParamsSuzanne()
    {
        BtnEditShapeParams(ShapeParamLabels.SUZANNE);
    }

    void BtnEditShapeParamsGlobal()
    {
        BtnEditShapeParams(ShapeParamLabels.GLOBAL);
    }

    void BtnEditShapeParams(ShapeParamLabels shapeID)
    {
        GameObject uiMenu = editParamMenus[(int)shapeID];

        // Close menu if already open
        if (uiMenu != null)
            uiMenu.GetComponent<UI_ObjectParams>().CloseMenu();
        // Open new menu
        else
        {
            uiMenu = Instantiate(uiObjectParamMenu);
            uiMenu.transform.SetParent(uiCanvas.gameObject.transform);
            uiMenu.GetComponent<UI_ObjectParams>().Init(this, shapeID);
            editParamMenus[(int)shapeID] = uiMenu;
            closeAllMenusBtn.interactable = true;
            numMenusOpen++;
            menuSortCount++;
        }
    }

    void BtnCloseAllEditMenus()
    {
        for (int i = 0; i < (int)ShapeParamLabels.COUNT; i++)
        {
            if (editParamMenus[i] != null)
                editParamMenus[i].GetComponent<UI_ObjectParams>().CloseMenu();
        }

        closeAllMenusBtn.interactable = false;
        numMenusOpen = 0;
        menuSortCount = 0;
    }

    void NewScene()
    {
        DestroyObjects();
        ApplySettings();
    }

    void ApplySettings()
    {
        TextureMode previousMode = activeViewMode; // Preserve current texture mode
        ReadValues();
        UpdateNumFrames();
        UpdateRenderTextures();
        UpdateSkybox();
        HandleNumObjectsChange();
        SpawnObjects();
        ResetSimulation();
        AdjustRenderQuads();
        SetRenderQuadTextures(previousMode); // Restore previous texture mode
        changesMade = false;
    }

    void UpdateSkybox()
    {
        RenderSettings.skybox.SetFloat("_noiseAmt", bgNoiseAmt);
    }

    void SetNewSkyboxSeed()
    {
        skyboxSeed = new Vector3(rng.Next(-100000, 100000), rng.Next(-100000, 100000), rng.Next(-100000, 100000));
        RenderSettings.skybox.SetVector("_noiseSeed", skyboxSeed);
    }

    void UpdateNumFrames()
    {
        numFrames = (int)Mathf.Round(duration / timeScale);
    }

    void HandleNumObjectsChange()
    {
        rngObjectCount = new System.Random(seed);

        // Clamp values
        if (maxNumObjects > maxNumObjectsHard)
        {
            maxNumObjects = maxNumObjectsHard;
            maxNumObjectsInput.text = $"{maxNumObjects}";
        }
        if (minNumObjects > maxNumObjects)
        {
            minNumObjects = maxNumObjects;
            minNumObjectsInput.text = $"{minNumObjects}";
        }
        if (maxNumObjects < 0)
        {
            maxNumObjects = 1;
            maxNumObjectsInput.text = "1";
        }
        if (minNumObjects < 0)
        {
            minNumObjects = 1;
            minNumObjectsInput.text = "1";
        }

        // Set number of objects in scene
        if (minNumObjects != maxNumObjects)
            numObjectsInScene = rngObjectCount.Next(minNumObjects, maxNumObjects + 1);
        else
            numObjectsInScene = minNumObjects;
    }

    void ResetSimulation()
    {
        rngTransform = new System.Random(seed);
        ResetAllObjects();
        SetNewSkyboxSeed();
        frame = 0;
        timestamp = 0;
    }

    void ResetAllObjects()
    {
        DestroyObjects();
        SpawnObjects();
    }

    void SpawnObjects()
    {
        SpawnableObj objToSpawn;

        for (int i = 0; i < numObjectsInScene; i++)
        {
            // Get an object from the pool
            objToSpawn = objectPool.Get();

            // Initialize and set its shape
            objToSpawn.Init(i, this);

            // Add to object list
            spawnedObjects.Add(objToSpawn);
        }
    }

    void DestroyObjects()
    {
        for (int i = 0; i < numObjectsInScene; i++)
        {
            objectPool.ReleaseToFront(spawnedObjects[i]);
        }
        spawnedObjects.Clear();
    }

    public int GetWeightedRandomShapeID()
    {
        float totalWeight = 0f;
        for (int i = 0; i < shapeWeights.Length; i++)
        {
            totalWeight += shapeWeights[i];
        }

        // Handle all-zero weights
        if (totalWeight == 0)
        {
            return rngTransform.Next(0, (int)Shapes.COUNT);
        }

        float randomValue = (float)rngTransform.NextDouble() * totalWeight;
        float cumulativeWeight = 0f;
        for (int i = 0; i < shapeWeights.Length; i++)
        {
            cumulativeWeight += shapeWeights[i];
            if (randomValue <= cumulativeWeight)
            {
                return i;
            }
        }
        return 0; // Fallback
    }

    Mesh GetPrimitiveMesh(Shapes shape)
    {
        switch (shape)
        {
            case Shapes.CUBE: return mdl_Cube.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.SPHERE: return mdl_Sphere.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.ICOSPHERE: return mdl_Icosphere.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.CYLINDER: return mdl_Cylinder.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.PYRAMID: return mdl_Pyramid.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.CONE: return mdl_Cone.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.CAPSULE: return mdl_Capsule.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.TORUS: return mdl_Torus.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.L_BLOCK: return mdl_LBlock.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.T_BLOCK: return mdl_TBlock.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.TEAPOT: return mdl_Teapot.GetComponent<MeshFilter>().sharedMesh;
            case Shapes.SUZANNE: return mdl_Suzanne.GetComponent<MeshFilter>().sharedMesh;
            default: return mdl_Cube.GetComponent<MeshFilter>().sharedMesh; // Fallback
        }
    }

    Mesh RescaleMesh(Mesh mesh)
    {
        float shapeVolume = CalculateMeshVolume(mesh);
        float sf = Mathf.Pow(1 / shapeVolume, 1f / 3f);
        Vector3 scaleFactor = new Vector3(sf, sf, sf);

        // Mesh simpleMesh = ConvexHullUtil.GenerateConvexHullMesh(mesh);
        // GameObject gO = new GameObject();
        // MeshCollider collider = gO.AddComponent<MeshCollider>();
        // collider.sharedMesh = simpleMesh;
        // collider.convex = true;

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Scale(vertices[i], scaleFactor);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();  // Update mesh bounding box
        mesh.RecalculateNormals(); // Fix lighting issues

        return mesh;
    }

    float CalculateMeshVolume(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float volume = 0f;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            volume += SignedVolumeOfTriangle(v1, v2, v3);
        }

        return Mathf.Abs(volume);
    }

    float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(Vector3.Cross(p1, p2), p3) / 6f;
    }

    void UpdateRenderTextures()
    {
        AdjustRenderQuads();
        CreateRenderTextures();
    }

    void AdjustRenderQuads()
    {
        renderCamera.fieldOfView = fov;
        depthCamera.fieldOfView = fov;
        anomalyCamera.fieldOfView = fov;
        skyboxCamera.fieldOfView = fov;

        float quadDistance = Vector3.Distance(renderQuadScene.transform.position, mainCamera.transform.position);

        // Use main camera's FOV and aspect ratio to calculate viewport dimensions at the given distance
        float fovVertical = mainCamera.fieldOfView;
        float aspect = mainCamera.aspect;

        // Convert vertical FOV to radians and calculate viewport height at distance
        float viewportHeight = 2f * quadDistance * Mathf.Tan(fovVertical * 0.5f * Mathf.Deg2Rad);
        float viewportWidth = viewportHeight * aspect;

        // Calculate quad size to match texture aspect ratio
        float aspectRatio = (float)textureHeight / (float)textureWidth;
        float quadWidth = viewportWidth, quadHeight = quadWidth * aspectRatio;

        if (quadHeight > viewportHeight)
        {
            quadHeight = viewportHeight;
            quadWidth = quadHeight / aspectRatio;
        }

        // Apply scale and positioning
        renderQuadScene.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);
        renderQuadScene.transform.position = mainCamera.transform.position + mainCamera.transform.forward * quadDistance;
        renderQuadScene.transform.LookAt(mainCamera.transform);
        renderQuadScene.transform.Rotate(0, 180, 0); // Flip the quad to face the camera properly

        // Do the same to the anomaly quad
        renderQuadAnomaly.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);
        renderQuadAnomaly.transform.position = mainCamera.transform.position + mainCamera.transform.forward * quadDistance - new Vector3(0, 0, 0.001f);
        renderQuadAnomaly.transform.LookAt(mainCamera.transform);
        renderQuadAnomaly.transform.Rotate(0, 180, 0); // Flip the quad to face the camera properly
    }

    void CreateRenderTextures()
    {
        if (depthTexture != null) Destroy(depthTexture);
        if (fogDepthTexture != null) Destroy(fogDepthTexture);
        if (rawViewTexture != null) Destroy(rawViewTexture);
        if (rawViewAATexture != null) Destroy(rawViewAATexture);
        if (frameTexture != null) Destroy(frameTexture);
        if (lastFrameTexture != null) Destroy(lastFrameTexture);
        if (differenceTexture != null) Destroy(differenceTexture);
        if (skyboxTexture != null) Destroy(skyboxTexture);
        if (anomalyTexture != null) Destroy(anomalyTexture);
        if (lastAnomalyTexture != null) Destroy(lastAnomalyTexture);
        if (anomalyMaskTexture != null) Destroy(anomalyMaskTexture);

        depthTexture = CreateRenderTexture();
        fogDepthTexture = CreateRenderTexture();
        rawViewTexture = CreateRenderTexture();
        if (useAA)
            rawViewAATexture = CreateRenderTexture();
        frameTexture = CreateRenderTexture();
        lastFrameTexture = CreateRenderTexture();
        differenceTexture = CreateRenderTexture();
        skyboxTexture = CreateRenderTexture();
        anomalyTexture = CreateRenderTexture();
        lastAnomalyTexture = CreateRenderTexture();
        anomalyMaskTexture = CreateRenderTexture();

        skyboxCamera.targetTexture = skyboxTexture;
        anomalyCamera.targetTexture = anomalyTexture;
        renderCamera.targetTexture = rawViewTexture;
        depthCamera.targetTexture = depthTexture;
    }

    RenderTexture CreateRenderTexture()
    {
        var rt = new RenderTexture((int)textureWidth, (int)textureHeight, 16)
        {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
        rt.Create();
        return rt;
    }

    void SetRenderQuadTextures(TextureMode mode)
    {
        activeViewMode = mode;
        RenderTexture texture;

        if (mode == TextureMode.Real)
        {
            texture = frameTexture;
            renderQuadAnomaly.SetActive(false);
        }
        else
        {
            texture = differenceTexture;
            renderQuadAnomaly.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", anomalyMaskTexture);
            renderQuadAnomaly.SetActive(true);
        }

        renderQuadScene.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", texture);
    }

    void DispatchCreateFrameComputeShader()
    {
        createFrame.SetBool("AntiAlias", useAA);
        createFrame.SetFloat("SceneDepth", sceneDepth);
        createFrame.SetFloat("zCameraDissolve", zCameraDissolve);
        createFrame.SetTexture(kernelHandleCreateFrame, "SkyboxTexture", skyboxTexture);
        createFrame.SetTexture(kernelHandleCreateFrame, "RawViewTexture", rawViewTexture);
        if (useAA)
            createFrame.SetTexture(kernelHandleCreateFrame, "RawViewAATexture", rawViewAATexture);
        createFrame.SetTexture(kernelHandleCreateFrame, "DepthTexture", depthTexture);
        createFrame.SetTexture(kernelHandleCreateFrame, "FogDepthTexture", fogDepthTexture);
        createFrame.SetTexture(kernelHandleCreateFrame, "Result", frameTexture);

        int threadGroupsX = Mathf.CeilToInt(textureWidth / 16f);
        int threadGroupsY = Mathf.CeilToInt(textureHeight / 16f);
        createFrame.Dispatch(kernelHandleCreateFrame, threadGroupsX, threadGroupsY, 1);
    }

    void DispatchFrameDiffComputeShader()
    {
        frameDiff.SetTexture(kernelHandleDiff, "CurrentFrame", frameTexture);
        frameDiff.SetTexture(kernelHandleDiff, "PrevFrame", lastFrameTexture);
        frameDiff.SetTexture(kernelHandleDiff, "Result", differenceTexture);
        frameDiff.SetFloat("SpikeThreshold", spikeThreshold);
        frameDiff.SetFloat("Frame", frame);

        int threadGroupsX = Mathf.CeilToInt(textureWidth / 16f);
        int threadGroupsY = Mathf.CeilToInt(textureHeight / 16f);
        frameDiff.Dispatch(kernelHandleDiff, threadGroupsX, threadGroupsY, 1);
    }

    void DispatchAnomalyMaskComputeShader()
    {
        anomalyMask.SetTexture(kernelHandleAnom, "CurrentFrame", anomalyTexture);
        anomalyMask.SetTexture(kernelHandleAnom, "PrevFrame", lastAnomalyTexture);
        anomalyMask.SetTexture(kernelHandleAnom, "FrameDiff", differenceTexture);
        anomalyMask.SetTexture(kernelHandleAnom, "Result", anomalyMaskTexture);
        anomalyMask.SetFloat("Frame", frame);

        int threadGroupsX = Mathf.CeilToInt(textureWidth / 16f);
        int threadGroupsY = Mathf.CeilToInt(textureHeight / 16f);
        anomalyMask.Dispatch(kernelHandleAnom, threadGroupsX, threadGroupsY, 1);
    }

    void DispatchPrepareDataComputeShader()
    {
        timestamp = (int)(Mathf.Ceil(frame * timeScale)) * 1000;

        int bufferElements = pixelCount * 2; // 2 uints per potential match (8 bytes)
        var outputEvents = new ComputeBuffer(bufferElements, sizeof(uint), ComputeBufferType.Structured);
        var outputAnomaly = new ComputeBuffer(bufferElements, sizeof(uint), ComputeBufferType.Structured);
        var indexEvents = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);
        var indexAnomaly = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);

        indexEvents.SetData(new uint[1] { 0 });
        indexAnomaly.SetData(new uint[1] { 0 });

        packEventBits.SetInt("Timestamp", timestamp);
        packEventBits.SetTexture(kernelHandlePrepareData, "FrameDiff", differenceTexture);
        packEventBits.SetTexture(kernelHandlePrepareData, "AnomalyMask", anomalyMaskTexture);
        packEventBits.SetBuffer(kernelHandlePrepareData, "OutputBufferEvents", outputEvents);
        packEventBits.SetBuffer(kernelHandlePrepareData, "OutputBufferAnomaly", outputAnomaly);
        packEventBits.SetBuffer(kernelHandlePrepareData, "OutputIndexEvents", indexEvents);
        packEventBits.SetBuffer(kernelHandlePrepareData, "OutputIndexAnomaly", indexAnomaly);

        int threadGroupsX = Mathf.CeilToInt(textureWidth / 16f);
        int threadGroupsY = Mathf.CeilToInt(textureHeight / 16f);
        packEventBits.Dispatch(kernelHandlePrepareData, threadGroupsX, threadGroupsY, 1);

        uint[] outputIndexEvents = new uint[1];
        indexEvents.GetData(outputIndexEvents);
        int uintCountEvents = (int)outputIndexEvents[0];

        uint[] outputIndexAnomaly = new uint[1];
        indexAnomaly.GetData(outputIndexAnomaly);
        int uintCountAnomaly = (int)outputIndexAnomaly[0];

        if (uintCountEvents > 0)
        {
            uint[] rawData = new uint[uintCountEvents];
            outputEvents.GetData(rawData);

            for (int i = 0; i < uintCountEvents; i += 2)
            {
                uint low = rawData[i];
                uint high = rawData[i + 1];

                events.Add((low, high));

                if (Debug.isDebugBuild)
                {
                    // Unpack timestamp
                    int ts = (int)(low & 0x7FFFFF);

                    // Unpack polarity (1 if white, 0 if black)
                    bool isWhite = ((low >> 23) & 0x1) == 1;

                    // Unpack x (upper 16 bits in high)
                    int x = (int)((high >> 8) & 0xFFFF);

                    // Unpack y (upper 8 bits in low & lower 8 bits in high)
                    int y = (int)(((low >> 24) & 0xFF) | ((high & 0xFF) << 8));

                    Debug.Log($"Entry {i / 2}, timestamp {ts}: {(isWhite ? "White" : "Black")} at ({x}, {y}).   Low = {low.ToString("X")}, High = {high.ToString("X")}");
                }
            }
        }

        if (uintCountAnomaly > 0)
        {
            uint[] rawData = new uint[uintCountAnomaly];
            outputAnomaly.GetData(rawData);

            for (int i = 0; i < uintCountAnomaly; i += 2)
            {
                uint low = rawData[i];
                uint high = rawData[i + 1];

                anomalies.Add((low, high));

                if (Debug.isDebugBuild)
                {
                    // Unpack timestamp
                    int ts = (int)(low & 0x7FFFFF);

                    // Unpack x (upper 16 bits in high)
                    int x = (int)((high >> 8) & 0xFFFF);

                    // Unpack y (upper 8 bits in low & lower 8 bits in high)
                    int y = (int)((((low >> 24) & 0xFF) << 8) | (high & 0xFF));

                    Debug.Log($"Anomaly {i / 2}, timestamp {ts}: at ({x}, {y}).   Low = {low.ToString("X")}, High = {high.ToString("X")}");
                }
            }
        }

        outputEvents.Release();
        indexEvents.Release();
    }

    void CopyAllShapeParamData(ShapeParamData[] src, ShapeParamData[] dst)
    {
        for (int i = 0; i < (int)ShapeParamLabels.COUNT; i++)
        {
            dst[i] = src[i].Copy();
        }
    }

    public void CopyShapeParamDataToClipboard(ShapeParamLabels shapeID)
    {
        shapeParamDataClipboard = shapeParamDataBuffer[(int)shapeID].Copy();
        dataInClipboard = true;
    }

    public void PasteShapeParamDataFromClipboard(ShapeParamLabels shapeID)
    {
        shapeParamDataBuffer[(int)shapeID] = shapeParamDataClipboard.Copy();
    }

    void ReadValues()
    {
        rngTransform = new System.Random(seed);

        // Read slider values
        shapeWeights = new float[(int)Shapes.COUNT];
        for (int i = 0; i < (int)Shapes.COUNT; i++)
        {
            shapeWeights[i] = weightSliders[i].value;
        }
        pThreshold = pThresholdSlider.value;

        // Read toggles
        useGPU = useGPUToggle.isOn;
        useAA = useAAToggle.isOn;
        cleanClipping = cleanClippingToggle.isOn;

        reboundOnEdges[0] = reboundNegXToggle.isOn;
        reboundOnEdges[1] = reboundPosXToggle.isOn;
        reboundOnEdges[2] = reboundNegYToggle.isOn;
        reboundOnEdges[3] = reboundPosYToggle.isOn;
        reboundOnEdges[4] = reboundNegZToggle.isOn;
        reboundOnEdges[5] = reboundPosZToggle.isOn;

        // Read input fields
        int.TryParse(seedInput.text, out seed);
        float.TryParse(spikeThresholdInput.text, out spikeThreshold);
        float.TryParse(fuzzinessInput.text, out fuzziness);
        int.TryParse(minNumObjectsInput.text, out minNumObjects);
        int.TryParse(maxNumObjectsInput.text, out maxNumObjects);
        float.TryParse(durationInput.text, out duration);
        float.TryParse(timeScaleInput.text, out timeScale);
        float.TryParse(fovInput.text, out fov);
        float.TryParse(sceneDepthInput.text, out sceneDepth);
        float.TryParse(fogDepthInput.text, out fogDepthNorm);
        float.TryParse(bgNoiseInput.text, out bgNoiseAmt);
        int.TryParse(textureWidthInput.text, out textureWidth);
        int.TryParse(textureHeightInput.text, out textureHeight);
        pixelCount = textureWidth * textureHeight;

        // Update Z plane distances
        zCameraClip = renderCamera.transform.position.z + cameraClipDistance;
        zCameraDissolve = renderCamera.transform.position.z + cameraCloseDissolveDistance;
        zCloseBoundary = renderCamera.transform.position.z + zCloseSpawnBuffer;
        zFarBoundary = zCloseBoundary + sceneDepth;
        zSpawnBoundary = zFarBoundary + zFarSpawnBuffer;

        // Update fog start distance
        fogDepth = (fogDepthNorm * sceneDepth) + zFarSpawnBuffer;
        fogStart = zSpawnBoundary - fogDepth;

        // Reset
        for (int i = 0; i < 3; i++)
        {
            includeShapeParam[(int)ShapeParams.X_ROTATION + i] = includeRotationTgls[i].isOn;
            includeShapeParam[(int)ShapeParams.X_TRANSLATION + i] = includeTranslationTgls[i].isOn;
            includeShapeParam[(int)ShapeParams.X_SCALE + i] = includeScaleTgls[i].isOn;
            includeShapeParam[(int)ShapeParams.X_INIT_ROTATION + i] = includeInitRotationTgls[i].isOn;
            includeShapeParam[(int)ShapeParams.X_INIT_POSITION + i] = includeInitPositionTgls[i].isOn;
        }
        includeShapeParam[(int)ShapeParams.SURFACE_NOISE] = includeSurfaceNoiseTgl.isOn;

        // Copy shape parameter values
        CopyAllShapeParamData(shapeParamDataBuffer, shapeParamData);
    }

    public float ReadShapeParam(ShapeParamLabels shape, ShapeParamsNorm param)
    {
        return shapeParamData[(int)shape].GetValue(param);
    }

    public float ReadShapeParamBuffer(ShapeParamLabels shape, ShapeParamsNorm param)
    {
        return shapeParamDataBuffer[(int)shape].GetValue(param);
    }

    public float ReadShapeParamClipboard(ShapeParamLabels shape, ShapeParamsNorm param)
    {
        return shapeParamDataClipboard.GetValue(param);
    }

    public void WriteShapeParamBuffer(ShapeParamLabels shape, ShapeParamsNorm param, float value)
    {
        shapeParamDataBuffer[(int)shape].SetValue(param, value);
    }

    void UpdateUIReadouts()
    {
        fpsReadout.text = $"{(int)(1 / Time.deltaTime)}";
        timeReadout.text = $"{frame * timeScale:0.00}ms";
        frameReadout.text = $"{frame}/{numFrames}";
        numObjectsReadout.text = $"{numObjectsInScene}";
    }

    public Rect GetUIScreenBoundary()
    {
        return new Rect(lhPanelRect.rect.width, 0, Screen.width - rhPanelRect.rect.width - lhPanelRect.rect.width - 33, Screen.height - 13);  // Yeah idk why we need these magic numbers to make things look nice but we dooooooooo
    }

    void UpdateDraggingMenu()
    {
        if (selectedMenuRect == null)
        {
            lastMousePosition = Vector2.zero;
            return;
        }

        RectTransform titleRect = selectedMenuRect.GetComponent<UI_ObjectParams>().titleRect;
        RectTransform parentRect = selectedMenuRect.parent as RectTransform;
        
        Vector2 mousePosition = Input.mousePosition;

        if (lastMousePosition == Vector2.zero)
            lastMousePosition = mousePosition;

        // Convert mouse positions to local points relative to the parent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, mousePosition, null, out Vector2 localMousePos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, lastMousePosition, null, out Vector2 localLastMousePos);

        // Apply delta to menu position
        selectedMenuRect.anchoredPosition += localMousePos - localLastMousePos;
        lastMousePosition = mousePosition;

        Rect uiScreenBoundary = GetUIScreenBoundary();

        // Adjusted screen boundaries relative to the parent
        Rect adjustedBoundary = new Rect(
            uiScreenBoundary.x - parentRect.rect.width * parentRect.pivot.x,
            uiScreenBoundary.y - parentRect.rect.height * parentRect.pivot.y,
            uiScreenBoundary.width,
            uiScreenBoundary.height
        );

        // Get size and position for clamping
        Vector2 size = selectedMenuRect.rect.size;
        Vector2 pos = selectedMenuRect.anchoredPosition;

        // Clamping boundaries
        float minX = adjustedBoundary.xMin + size.x * selectedMenuRect.pivot.x;
        float maxX = adjustedBoundary.xMax - size.x * (1 - selectedMenuRect.pivot.x);
        float minY = adjustedBoundary.yMin + size.y * selectedMenuRect.pivot.y - (size.y - titleRect.rect.height);
        float maxY = adjustedBoundary.yMax - size.y * (1 - selectedMenuRect.pivot.y);

        // Clamp position
        selectedMenuRect.anchoredPosition = new Vector2(
            Mathf.Clamp(pos.x, minX, maxX),
            Mathf.Clamp(pos.y, minY, maxY)
        );
    }
}
