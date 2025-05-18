using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Shapes { CUBE, SPHERE, ICOSPHERE, CYLINDER, PYRAMID, CONE, CAPSULE, TORUS, L_BLOCK, T_BLOCK, TEAPOT, SUZANNE, COUNT }
public enum ShapeParamLabels { CUBE, SPHERE, ICOSPHERE, CYLINDER, PYRAMID, CONE, CAPSULE, TORUS, L_BLOCK, T_BLOCK, TEAPOT, SUZANNE, GLOBAL, COUNT }
public enum ShapeParams { X_ROTATION, Y_ROTATION, Z_ROTATION, 
                          X_INIT_ROTATION, Y_INIT_ROTATION, Z_INIT_ROTATION, 
                          X_TRANSLATION, Y_TRANSLATION, Z_TRANSLATION, 
                          X_INIT_POSITION, Y_INIT_POSITION, Z_INIT_POSITION, 
                          X_SCALE, Y_SCALE, Z_SCALE, 
                          SURFACE_NOISE, 
                          COUNT}
public enum ShapeParamsNorm { X_ROTATION_MEAN, X_ROTATION_STD, X_ROTATION_INCLUDE,
                              Y_ROTATION_MEAN, Y_ROTATION_STD, Y_ROTATION_INCLUDE, 
                              Z_ROTATION_MEAN, Z_ROTATION_STD, Z_ROTATION_INCLUDE,
                              X_INIT_ROTATION_MEAN, X_INIT_ROTATION_STD, X_INIT_ROTATION_INCLUDE, 
                              Y_INIT_ROTATION_MEAN, Y_INIT_ROTATION_STD, Y_INIT_ROTATION_INCLUDE, 
                              Z_INIT_ROTATION_MEAN, Z_INIT_ROTATION_STD, Z_INIT_ROTATION_INCLUDE,
                              X_TRANSLATION_MEAN, X_TRANSLATION_STD, X_TRANSLATION_INCLUDE, 
                              Y_TRANSLATION_MEAN, Y_TRANSLATION_STD, Y_TRANSLATION_INCLUDE, 
                              Z_TRANSLATION_MEAN, Z_TRANSLATION_STD, Z_TRANSLATION_INCLUDE,
                              X_INIT_POSITION_MEAN, X_INIT_POSITION_STD, X_INIT_POSITION_INCLUDE, 
                              Y_INIT_POSITION_MEAN, Y_INIT_POSITION_STD, Y_INIT_POSITION_INCLUDE, 
                              Z_INIT_POSITION_MEAN, Z_INIT_POSITION_STD, Z_INIT_POSITION_INCLUDE,
                              X_SCALE_MEAN, X_SCALE_STD, X_SCALE_INCLUDE, 
                              Y_SCALE_MEAN, Y_SCALE_STD, Y_SCALE_INCLUDE, 
                              Z_SCALE_MEAN, Z_SCALE_STD, Z_SCALE_INCLUDE,
                              SURFACE_NOISE_MEAN, SURFACE_NOISE_STD, SURFACE_NOISE_INCLUDE,
                              COUNT}

public enum TextureMode { Real, Spikes }

public static class Utils
{
    public static void BindSliderToInputField(Slider slider, TMP_InputField inputField, bool wholeNumbers = false)
    {
        bool isUpdating = false;

        void OnSliderChanged(float value)
        {
            if (isUpdating) return;
            isUpdating = true;
            inputField.text = wholeNumbers ? ((int)value).ToString() : value.ToString("0.00");
            isUpdating = false;
        }

        void OnInputChanged(string text)
        {
            if (isUpdating) return;

            if (float.TryParse(text, out float value))
            {
                float clampedValue = Mathf.Clamp(value, slider.minValue, slider.maxValue);

                isUpdating = true;

                slider.value = clampedValue;

                if (!Mathf.Approximately(value, clampedValue))
                {
                    inputField.text = wholeNumbers ? ((int)clampedValue).ToString() : clampedValue.ToString("0.00");
                }

                isUpdating = false;
            }
        }

        slider.onValueChanged.AddListener(OnSliderChanged);
        inputField.onEndEdit.AddListener(OnInputChanged);

        inputField.text = wholeNumbers ? ((int)slider.value).ToString() : slider.value.ToString("0.00");
    }

    public static void SetupTextInputFormatting(TMP_InputField inputField, string format, int charLimit)
    {
        void OnInputChanged(string text)
        {
            inputField.text = string.Format(inputField.text, format);
            inputField.characterLimit = charLimit;
        }

        inputField.onEndEdit.AddListener(OnInputChanged);
    }

    public static void SetupTextInputFormatting(TMP_InputField inputField, string format, int charLimit, float min, float max)
    {
        void OnInputChanged(string text)
        {
            if (float.TryParse(text, out float value))
            {
                if (value < min)
                    value = min;
                else if (value > max)
                    value = max;

                inputField.text = value.ToString(format);
                inputField.characterLimit = charLimit;
            }
        }

        inputField.onEndEdit.AddListener(OnInputChanged);
    }

    public static Texture2D RenderTextureToTexture2D(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    public static Color32 SumColor32(Color32 c1, Color32 c2)
    {
        byte r = (byte)Mathf.Clamp(c1.r + c2.r, 0, 255);
        byte g = (byte)Mathf.Clamp(c1.g + c2.g, 0, 255);
        byte b = (byte)Mathf.Clamp(c1.b + c2.b, 0, 255);
        byte a = (byte)Mathf.Clamp(c1.a + c2.a, 0, 255);

        return new Color32(r, g, b, a);
    }

    public static Color AdjustColorBrightness(Color color, float coefficient)
    {
        coefficient = Mathf.Clamp(coefficient, -1f, 1f);
        
        // Convert to HSV
        Color.RGBToHSV(color, out float h, out float s, out float v);

        // Adjust the value (brightness)
        v = Mathf.Clamp01(v + coefficient);

        // Convert back to RGB
        return Color.HSVToRGB(h, s, v);
    }

    public static ShapeParamsNorm[] ShapeParamToShapeParamNorm(ShapeParams param)
    {
        // Calculate the base index for the mean and std
        int baseIndex = (int)param * 3;

        // Return the mean, std and inclusion based on the calculated index
        return new ShapeParamsNorm[]
        {
            (ShapeParamsNorm)baseIndex,
            (ShapeParamsNorm)(baseIndex + 1),
            (ShapeParamsNorm)(baseIndex + 2)
        };
    }
}

public struct ShapeParamData
{
    public bool isGlobal;
    public float[] data;

    public ShapeParamData(bool isGlobal = false)
    {
        this.isGlobal = isGlobal;
        this.data = new float[(int)ShapeParamsNorm.COUNT];
        Default();
    }

    public float GetValue(ShapeParamsNorm param)
    {
        return data[(int)param];
    }

    public float GetValue(int paramID)
    {
        return data[paramID];
    }

    public void SetValue(ShapeParamsNorm param, float value)
    {
        data[(int)param] = value;
    }

    public void SetValue(int paramID, float value)
    {
        data[paramID] = value;
    }

    public void Default()
    {
        // Global settings
        if (isGlobal)
        {
            for (int i = 0; i < (int)ShapeParamsNorm.COUNT; i++)
            {
                data[i] = GetGlobalDefaultValue((ShapeParamsNorm)i);
            }
        }
        else
        {
            for (int i = 0; i < (int)ShapeParamsNorm.COUNT; i++)
            {
                data[i] = GetDefaultValue((ShapeParamsNorm)i);
            }
        }
    }

    public static float GetDefaultValue(ShapeParamsNorm param)
    {
        if (param == ShapeParamsNorm.X_INIT_POSITION_MEAN || param == ShapeParamsNorm.Y_INIT_POSITION_MEAN || param == ShapeParamsNorm.Z_INIT_POSITION_MEAN)
            return 0.5f;
        else if (param == ShapeParamsNorm.X_SCALE_MEAN || param == ShapeParamsNorm.Y_SCALE_MEAN || param == ShapeParamsNorm.Z_SCALE_MEAN ||
                 param == ShapeParamsNorm.X_ROTATION_INCLUDE || param == ShapeParamsNorm.Y_ROTATION_INCLUDE || param == ShapeParamsNorm.Z_ROTATION_INCLUDE ||
                 param == ShapeParamsNorm.X_TRANSLATION_INCLUDE || param == ShapeParamsNorm.Y_TRANSLATION_INCLUDE || param == ShapeParamsNorm.Z_TRANSLATION_INCLUDE ||
                 param == ShapeParamsNorm.X_SCALE_INCLUDE || param == ShapeParamsNorm.Y_SCALE_INCLUDE || param == ShapeParamsNorm.Z_SCALE_INCLUDE ||
                 param == ShapeParamsNorm.SURFACE_NOISE_INCLUDE)
            return 1;
        else
            return 0;
    }

    public static float GetGlobalDefaultValue(ShapeParamsNorm param)
    {
        if (param == ShapeParamsNorm.X_INIT_POSITION_MEAN || param == ShapeParamsNorm.Y_INIT_POSITION_MEAN || param == ShapeParamsNorm.Z_INIT_POSITION_MEAN)
            return 0.5f;
        else if (param == ShapeParamsNorm.X_ROTATION_MEAN || param == ShapeParamsNorm.Y_ROTATION_MEAN || param == ShapeParamsNorm.Z_ROTATION_MEAN ||
                 param == ShapeParamsNorm.X_SCALE_MEAN || param == ShapeParamsNorm.Y_SCALE_MEAN || param == ShapeParamsNorm.Z_SCALE_MEAN ||
                 param == ShapeParamsNorm.X_ROTATION_INCLUDE || param == ShapeParamsNorm.Y_ROTATION_INCLUDE || param == ShapeParamsNorm.Z_ROTATION_INCLUDE ||
                 param == ShapeParamsNorm.X_TRANSLATION_INCLUDE || param == ShapeParamsNorm.Y_TRANSLATION_INCLUDE || param == ShapeParamsNorm.Z_TRANSLATION_INCLUDE ||
                 param == ShapeParamsNorm.X_SCALE_INCLUDE || param == ShapeParamsNorm.Y_SCALE_INCLUDE || param == ShapeParamsNorm.Z_SCALE_INCLUDE ||
                 param == ShapeParamsNorm.SURFACE_NOISE_INCLUDE)
            return 1;
        else
            return 0;
    }

    public static float GetRandomValue(ShapeParamsNorm param)
    {
        return Random.Range(0f, 1f); 
    }

    public ShapeParamData Copy()
    {
        ShapeParamData ret = new ShapeParamData(isGlobal);

        for (int i = 0; i < (int)ShapeParamsNorm.COUNT; i++)
        {
            ret.data[i] = data[i];
        }

        return ret;
    }
}

struct UIParamInfo
{
    public string groupName;
    public ShapeParamsNorm meanParam;
    public ShapeParamsNorm stdParam;
    public ShapeParamsNorm includeParam;
    public float minMean, minStd;
    public float maxMean, maxStd;
    public float include;

    public UIParamInfo(string groupName, ShapeParamsNorm meanParam, ShapeParamsNorm stdParam, ShapeParamsNorm includeParam, float minMean, float maxMean, float minStd, float maxStd, float include)
    {
        this.groupName = groupName;
        this.meanParam = meanParam;
        this.stdParam = stdParam;
        this.includeParam = includeParam;
        this.minMean = minMean;
        this.minStd = minStd;
        this.maxMean = maxMean;
        this.maxStd = maxStd;
        this.include = include;
    }
}
