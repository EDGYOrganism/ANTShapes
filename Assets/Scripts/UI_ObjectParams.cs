using System.Globalization;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UI_ObjectParams : MonoBehaviour
{
    SceneController sceneController;
    ShapeParamLabels shapeID;
    public GameObject controls;
    public Button closeMenuBtn;
    public Button copyBtn;
    public Button pasteBtn;
    public Button defaultsBtn;
    public Button randomBtn;
    public TMP_Text titleText;
    public RectTransform rect;
    public RectTransform titleRect;
    public RectTransform bgPanelRect;
    public float newMenuOffset = 30;  // Set in inspector
    public UI_PanelClickHold panelClickHeld;
    private bool created = false;
    private Color color;

    // Mapping from shape params to UI information
    private static readonly Dictionary<ShapeParams, UIParamInfo> shapeParamUIMap = new Dictionary<ShapeParams, UIParamInfo>
    {
        { ShapeParams.X_ROTATION, new UIParamInfo("UI_XRotation", ShapeParamsNorm.X_ROTATION_MEAN, ShapeParamsNorm.X_ROTATION_STD, ShapeParamsNorm.X_ROTATION_INCLUDE, -2, 2, 0, 2, 1) },
        { ShapeParams.Y_ROTATION, new UIParamInfo("UI_YRotation", ShapeParamsNorm.Y_ROTATION_MEAN, ShapeParamsNorm.Y_ROTATION_STD, ShapeParamsNorm.Y_ROTATION_INCLUDE, -2, 2, 0, 2, 1) },
        { ShapeParams.Z_ROTATION, new UIParamInfo("UI_ZRotation", ShapeParamsNorm.Z_ROTATION_MEAN, ShapeParamsNorm.Z_ROTATION_STD, ShapeParamsNorm.Z_ROTATION_INCLUDE, -2, 2, 0, 2, 1) },

        { ShapeParams.X_INIT_ROTATION, new UIParamInfo("UI_XInitRotation", ShapeParamsNorm.X_INIT_ROTATION_MEAN, ShapeParamsNorm.X_INIT_ROTATION_STD, ShapeParamsNorm.X_INIT_ROTATION_INCLUDE, -180, 180, 0, 180, 0) },
        { ShapeParams.Y_INIT_ROTATION, new UIParamInfo("UI_YInitRotation", ShapeParamsNorm.Y_INIT_ROTATION_MEAN, ShapeParamsNorm.Y_INIT_ROTATION_STD, ShapeParamsNorm.Y_INIT_ROTATION_INCLUDE, -180, 180, 0, 180, 0) },
        { ShapeParams.Z_INIT_ROTATION, new UIParamInfo("UI_ZInitRotation", ShapeParamsNorm.Z_INIT_ROTATION_MEAN, ShapeParamsNorm.Z_INIT_ROTATION_STD, ShapeParamsNorm.Z_INIT_ROTATION_INCLUDE, -180, 180, 0, 180, 0) },

        { ShapeParams.X_TRANSLATION, new UIParamInfo("UI_XTranslation", ShapeParamsNorm.X_TRANSLATION_MEAN, ShapeParamsNorm.X_TRANSLATION_STD, ShapeParamsNorm.X_TRANSLATION_INCLUDE, -2, 2, 0, 2, 1) },
        { ShapeParams.Y_TRANSLATION, new UIParamInfo("UI_YTranslation", ShapeParamsNorm.Y_TRANSLATION_MEAN, ShapeParamsNorm.Y_TRANSLATION_STD, ShapeParamsNorm.Y_TRANSLATION_INCLUDE, -2, 2, 0, 2, 1) },
        { ShapeParams.Z_TRANSLATION, new UIParamInfo("UI_ZTranslation", ShapeParamsNorm.Z_TRANSLATION_MEAN, ShapeParamsNorm.Z_TRANSLATION_STD, ShapeParamsNorm.Z_TRANSLATION_INCLUDE, -2, 2, 0, 2, 1) },

        { ShapeParams.X_INIT_POSITION, new UIParamInfo("UI_XInitPosition", ShapeParamsNorm.X_INIT_POSITION_MEAN, ShapeParamsNorm.X_INIT_POSITION_STD, ShapeParamsNorm.X_INIT_POSITION_INCLUDE, 0, 1, 0, 0.15f, 0) },
        { ShapeParams.Y_INIT_POSITION, new UIParamInfo("UI_YInitPosition", ShapeParamsNorm.Y_INIT_POSITION_MEAN, ShapeParamsNorm.Y_INIT_POSITION_STD, ShapeParamsNorm.Y_INIT_POSITION_INCLUDE, 0, 1, 0, 0.15f, 0) },
        { ShapeParams.Z_INIT_POSITION, new UIParamInfo("UI_ZInitPosition", ShapeParamsNorm.Z_INIT_POSITION_MEAN, ShapeParamsNorm.Z_INIT_POSITION_STD, ShapeParamsNorm.Z_INIT_POSITION_INCLUDE, 0, 1, 0, 0.15f, 0) },

        { ShapeParams.X_SCALE, new UIParamInfo("UI_XScale", ShapeParamsNorm.X_SCALE_MEAN, ShapeParamsNorm.X_SCALE_STD, ShapeParamsNorm.X_SCALE_INCLUDE, 0.25f, 2, 0, 2, 1) },
        { ShapeParams.Y_SCALE, new UIParamInfo("UI_YScale", ShapeParamsNorm.Y_SCALE_MEAN, ShapeParamsNorm.Y_SCALE_STD, ShapeParamsNorm.Y_SCALE_INCLUDE, 0.25f, 2, 0, 2, 1) },
        { ShapeParams.Z_SCALE, new UIParamInfo("UI_ZScale", ShapeParamsNorm.Z_SCALE_MEAN, ShapeParamsNorm.Z_SCALE_STD, ShapeParamsNorm.Z_SCALE_INCLUDE, 0.25f, 2, 0, 2, 1) },

        { ShapeParams.SURFACE_NOISE, new UIParamInfo("UI_SurfaceNoise", ShapeParamsNorm.SURFACE_NOISE_MEAN, ShapeParamsNorm.SURFACE_NOISE_STD, ShapeParamsNorm.SURFACE_NOISE_INCLUDE, 0, 1, 0, 1, 1) },
    };

    void Update()
    {
        if (panelClickHeld.isHeld && sceneController.selectedMenuRect == null)
        {
            BringToFront();
            sceneController.selectedMenuRect = rect;
        }
        else if (!panelClickHeld.isHeld && sceneController.selectedMenuRect == rect)
            sceneController.selectedMenuRect = null;
    }

    public void Init(SceneController sceneController, ShapeParamLabels shapeID)
    {
        gameObject.name = $"EditObjParamMenu_{shapeID}";
        
        this.sceneController = sceneController;
        this.shapeID = shapeID;

        // Set sorting orders
        BringToFront();

        // Set position
        Rect uiScreenBoundary = sceneController.GetUIScreenBoundary();
        Vector3 pos = new Vector3(uiScreenBoundary.xMax, uiScreenBoundary.yMax, 0);
        pos.x -= bgPanelRect.rect.width / 2f;
        pos.x -= (newMenuOffset * sceneController.numMenusOpen);
        pos.y -= bgPanelRect.rect.height / 2f;
        pos.y -= (newMenuOffset * sceneController.numMenusOpen);
        gameObject.transform.position = pos;

        closeMenuBtn.onClick.AddListener(CloseMenu);

        SetTitleColor();
        InitializeUIDataFields();

        if (sceneController.dataInClipboard)
            pasteBtn.interactable = true;

        created = true;
    }

    void BringToFront()
    {
        sceneController.menuSortCount++;
        bgPanelRect.gameObject.GetComponent<Canvas>().sortingOrder = sceneController.menuSortCount * 2;
        controls.gameObject.GetComponent<Canvas>().sortingOrder = sceneController.menuSortCount * 2;
        titleRect.gameObject.GetComponent<Canvas>().sortingOrder = sceneController.menuSortCount * 2;
        closeMenuBtn.gameObject.GetComponent<Canvas>().sortingOrder = sceneController.menuSortCount * 2 + 1;
        titleText.gameObject.GetComponent<Canvas>().sortingOrder = sceneController.menuSortCount * 2 + 1;
    }

    void SetTitleColor()
    {
        color = sceneController.editObjectParamButtons[(int)shapeID].GetComponent<Image>().color;
        titleRect.GetComponent<Image>().color = color;
    }

    void InitializeUIDataFields()
    {
        // Set title
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        string titleStr = textInfo.ToTitleCase($"{shapeID}".ToLower());
        titleStr = titleStr.Replace("_", "-");
        titleText.text = titleStr;

        copyBtn.onClick.AddListener(CopyToClipboard);
        pasteBtn.onClick.AddListener(PasteFromClipboard);

        defaultsBtn.onClick.AddListener(SetAllParametersToDefault);
        SetButtonColor(defaultsBtn);

        randomBtn.onClick.AddListener(SetAllParametersToRandom);
        SetButtonColor(randomBtn);

        for (int i = 0; i < (int)ShapeParams.COUNT; i++)
        {
            ShapeParams param = (ShapeParams)i;
            if (!shapeParamUIMap.TryGetValue(param, out UIParamInfo info))
                continue;

            ShapeParamsNorm meanParam = info.meanParam;
            ShapeParamsNorm stdParam = info.stdParam;
            ShapeParamsNorm includeParam = info.includeParam;

            float meanValue = sceneController.shapeParamDataBuffer[(int)shapeID].GetValue(meanParam);
            float stdValue = sceneController.shapeParamDataBuffer[(int)shapeID].GetValue(stdParam);
            bool includeValue = sceneController.shapeParamDataBuffer[(int)shapeID].GetValue(includeParam) > 0;

            Transform fields = controls.transform.Find(info.groupName);

            Slider meanSlider = fields.Find("Sld_Mean")?.GetComponent<Slider>();
            TMP_InputField meanInput = fields.Find("Inp_Mean")?.GetComponent<TMP_InputField>();
            Utils.BindSliderToInputField(meanSlider, meanInput);
            meanSlider.onValueChanged.AddListener(meanValue => WriteParamValue(meanValue, meanParam));
            meanSlider.minValue = info.minMean;
            meanSlider.maxValue = info.maxMean;
            meanSlider.value = meanValue;
            SetSliderColor(meanSlider);

            Slider stdSlider = fields.Find("Sld_Std")?.GetComponent<Slider>();
            TMP_InputField stdInput = fields.Find("Inp_Std")?.GetComponent<TMP_InputField>();
            Utils.BindSliderToInputField(stdSlider, stdInput);
            stdSlider.onValueChanged.AddListener(stdValue => WriteParamValue(stdValue, stdParam));
            stdSlider.minValue = info.minStd;
            stdSlider.maxValue = info.maxStd;
            stdSlider.value = stdValue;
            SetSliderColor(stdSlider);

            Toggle includeTgl = fields.Find("Tgl_Include")?.GetComponent<Toggle>();
            includeTgl.onValueChanged.AddListener(includeValue => WriteParamValue(includeValue ? 1f : 0f, includeParam));
            includeTgl.isOn = includeValue;

            Button defaultBtn = fields.Find("Btn_Def")?.GetComponent<Button>();
            defaultBtn.onClick.AddListener(() => SetParameterToDefault(param, meanSlider, stdSlider, includeTgl));
            SetButtonColor(defaultBtn);

            Button randBtn = fields.Find("Btn_Rnd")?.GetComponent<Button>();
            randBtn.onClick.AddListener(() => SetParameterToRandom(param, meanSlider, stdSlider));
            SetLightButtonColor(randBtn);
        }
    }

    float ReadParamValue(ShapeParamsNorm param)
    {
        return sceneController.ReadShapeParamBuffer(shapeID, param);
    }

    void WriteParamValue(float value, ShapeParamsNorm param)
    {
        sceneController.WriteShapeParamBuffer(shapeID, param, value);
        if (created)
            sceneController.SetChangesMadeFlag();
    }

    void SetAllParametersToDefault()
    {
        for (int i = 0; i < (int)ShapeParams.COUNT; i++)
        {
            ShapeParams param = (ShapeParams)i;
            if (!shapeParamUIMap.TryGetValue(param, out UIParamInfo info))
                continue;

            Transform fields = controls.transform.Find(info.groupName);
            Slider meanSlider = fields.Find("Sld_Mean")?.GetComponent<Slider>();
            Slider stdSlider = fields.Find("Sld_Std")?.GetComponent<Slider>();
            Toggle includeTgl = fields.Find("Tgl_Include")?.GetComponent<Toggle>();

            SetParameterToDefault(param, meanSlider, stdSlider, includeTgl);
        }
    }

    void SetAllParametersToRandom()
    {
        for (int i = 0; i < (int)ShapeParams.COUNT; i++)
        {
            ShapeParams param = (ShapeParams)i;
            if (!shapeParamUIMap.TryGetValue(param, out UIParamInfo info))
                continue;

            Transform fields = controls.transform.Find(info.groupName);
            Slider meanSlider = fields.Find("Sld_Mean")?.GetComponent<Slider>();
            Slider stdSlider = fields.Find("Sld_Std")?.GetComponent<Slider>();

            SetParameterToRandom(param, meanSlider, stdSlider);
        }
    }

    void SetParameterToDefault(ShapeParams param, Slider meanSld, Slider stdSld, Toggle includeTgl)
    {
        if (!shapeParamUIMap.TryGetValue(param, out UIParamInfo info))
            return;

        ShapeParamsNorm meanParam = info.meanParam;
        ShapeParamsNorm stdParam = info.stdParam;
        ShapeParamsNorm includeParam = info.includeParam;

        float mean, std, include;

        if (shapeID == ShapeParamLabels.GLOBAL)
        {
            mean = ShapeParamData.GetGlobalDefaultValue(meanParam);
            std = ShapeParamData.GetGlobalDefaultValue(stdParam);
            include = ShapeParamData.GetGlobalDefaultValue(includeParam);
        }
        else
        {
            mean = ShapeParamData.GetDefaultValue(meanParam);
            std = ShapeParamData.GetDefaultValue(stdParam);
            include = ShapeParamData.GetDefaultValue(includeParam);
        }

        WriteParamValue(mean, meanParam);
        WriteParamValue(std, stdParam);
        WriteParamValue(include, includeParam);

        meanSld.value = mean;
        stdSld.value = std;
        includeTgl.isOn = include > 0;
    }

    void SetParameterToRandom(ShapeParams param, Slider meanSld, Slider stdSld)
    {
        if (!shapeParamUIMap.TryGetValue(param, out UIParamInfo info))
            return;

        ShapeParamsNorm meanParam = info.meanParam;
        ShapeParamsNorm stdParam = info.stdParam;

        float mean, std;

        mean = ShapeParamData.GetRandomValue(meanParam);
        std = ShapeParamData.GetRandomValue(stdParam);

        mean = info.minMean + (info.maxMean - info.minMean) * mean;
        std = info.minStd + (info.maxStd - info.minStd) * std;

        WriteParamValue(mean, meanParam);
        WriteParamValue(std, stdParam);

        meanSld.value = mean;
        stdSld.value = std;
    }

    void CopyToClipboard()
    {
        sceneController.CopyShapeParamDataToClipboard(shapeID);
        pasteBtn.interactable = true;
    }

    void PasteFromClipboard()
    {
        sceneController.PasteShapeParamDataFromClipboard(shapeID);

        for (int i = 0; i < (int)ShapeParams.COUNT; i++)
        {
            ShapeParams param = (ShapeParams)i;

            if (!shapeParamUIMap.TryGetValue(param, out UIParamInfo info))
                return;

            ShapeParamsNorm meanParam = info.meanParam;
            ShapeParamsNorm stdParam = info.stdParam;
            ShapeParamsNorm includeParam = info.includeParam;

            float mean, std, include;

            mean = ReadParamValue(meanParam);
            std = ReadParamValue(stdParam);
            include = ReadParamValue(includeParam);

            Transform fields = controls.transform.Find(info.groupName);
            Slider meanSlider = fields.Find("Sld_Mean")?.GetComponent<Slider>();
            Slider stdSlider = fields.Find("Sld_Std")?.GetComponent<Slider>();
            Toggle includeTgl = fields.Find("Tgl_Include")?.GetComponent<Toggle>();

            meanSlider.value = mean;
            stdSlider.value = std;
            includeTgl.isOn = include > 0;
        }
    }

    public void CloseMenu()
    {
        sceneController.numMenusOpen--;

        if (sceneController.numMenusOpen == 0)
        {
            sceneController.closeAllMenusBtn.interactable = false;
            sceneController.menuSortCount = 0;
        }

        Destroy(gameObject);
    }

    void SetButtonColor(Button button)
    {
        button.GetComponent<Image>().color = color;
    }

    void SetLightButtonColor(Button button)
    {
        button.GetComponent<Image>().color = Utils.AdjustColorBrightness(color, 1f);
    }

    void SetSliderColor(Slider slider)
    {
        Transform sliderTransform = slider.gameObject.transform;
        Image handle = sliderTransform.Find("Handle Slide Area/Handle").GetComponent<Image>();
        Image fill = sliderTransform.Find("Fill Area/Fill").GetComponent<Image>();

        handle.color = color;
        if (shapeID == ShapeParamLabels.GLOBAL)
            fill.color = new Color(92f / 255f, 255f / 255f, 133f / 255f, 255f / 255f);
        else
            fill.color = color;
    }
}
