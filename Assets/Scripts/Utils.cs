using UnityEngine;

public enum Shapes { CUBE, SPHERE, ICOSPHERE, CYLINDER, PYRAMID, CONE, CAPSULE, TORUS, L_BLOCK, T_BLOCK, TEAPOT, SUZANNE, COUNT }
public enum ShapeParamLabels { CUBE, SPHERE, ICOSPHERE, CYLINDER, PYRAMID, CONE, CAPSULE, TORUS, L_BLOCK, T_BLOCK, TEAPOT, SUZANNE, GLOBAL, COUNT }
public enum ShapeParams { X_ROTATION, Y_ROTATION, Z_ROTATION,
                          X_INIT_ROTATION, Y_INIT_ROTATION, Z_INIT_ROTATION,
                          X_TRANSLATION, Y_TRANSLATION, Z_TRANSLATION,
                          X_INIT_POSITION, Y_INIT_POSITION, Z_INIT_POSITION,
                          X_SCALE, Y_SCALE, Z_SCALE,
                          SURFACE_NOISE,
                          COUNT}
public enum ShapeParamsNorm { X_ROTATION_MEAN, X_ROTATION_STD,
                              Y_ROTATION_MEAN, Y_ROTATION_STD,
                              Z_ROTATION_MEAN, Z_ROTATION_STD, 
                              X_INIT_ROTATION_MEAN, X_INIT_ROTATION_STD,
                              Y_INIT_ROTATION_MEAN, Y_INIT_ROTATION_STD,
                              Z_INIT_ROTATION_MEAN, Z_INIT_ROTATION_STD,
                              X_TRANSLATION_MEAN, X_TRANSLATION_STD,
                              Y_TRANSLATION_MEAN, Y_TRANSLATION_STD,
                              Z_TRANSLATION_MEAN, Z_TRANSLATION_STD,
                              X_INIT_POSITION_MEAN, X_INIT_POSITION_STD,
                              Y_INIT_POSITION_MEAN, Y_INIT_POSITION_STD,
                              Z_INIT_POSITION_MEAN, Z_INIT_POSITION_STD,
                              X_SCALE_MEAN, X_SCALE_STD,
                              Y_SCALE_MEAN, Y_SCALE_STD,
                              Z_SCALE_MEAN, Z_SCALE_STD,
                              SURFACE_NOISE_MEAN, SURFACE_NOISE_STD,
                              COUNT}

public enum TextureMode { Real, Spikes }

public static class Utils
{
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

    public static ShapeParamsNorm[] ShapeParamToShapeParamNorm(ShapeParams param)
    {
        // Calculate the base index for the mean and std
        int baseIndex = (int)param * 2;

        // Return the mean and std based on the calculated index
        return new ShapeParamsNorm[]
        {
            (ShapeParamsNorm)baseIndex,
            (ShapeParamsNorm)(baseIndex + 1)
        };
    }
}

public struct ShapeParamData
{
    public ShapeParamLabels shape;
    public float[] data;

    public ShapeParamData(ShapeParamLabels shape)
    {
        this.shape = shape;
        data = new float[(int)ShapeParamsNorm.COUNT];
        Default();
    }

    public void Default()
    {
        // Global settings
        if (shape == ShapeParamLabels.GLOBAL)
        {
            data[(int)ShapeParamsNorm.X_ROTATION_MEAN] = 1;
            data[(int)ShapeParamsNorm.X_ROTATION_STD] = 0;
            data[(int)ShapeParamsNorm.Y_ROTATION_MEAN] = 1;
            data[(int)ShapeParamsNorm.Y_ROTATION_STD] = 0;
            data[(int)ShapeParamsNorm.Z_ROTATION_MEAN] = 1;
            data[(int)ShapeParamsNorm.Z_ROTATION_STD] = 0;

            data[(int)ShapeParamsNorm.X_INIT_ROTATION_MEAN] = 0;
            data[(int)ShapeParamsNorm.X_INIT_ROTATION_STD] = 0;
            data[(int)ShapeParamsNorm.Y_INIT_ROTATION_MEAN] = 0;
            data[(int)ShapeParamsNorm.Y_INIT_ROTATION_STD] = 0;
            data[(int)ShapeParamsNorm.Z_INIT_ROTATION_MEAN] = 0;
            data[(int)ShapeParamsNorm.Z_INIT_ROTATION_STD] = 0;

            data[(int)ShapeParamsNorm.X_TRANSLATION_MEAN] = 0;
            data[(int)ShapeParamsNorm.X_TRANSLATION_STD] = 0;
            data[(int)ShapeParamsNorm.Y_TRANSLATION_MEAN] = 0;
            data[(int)ShapeParamsNorm.Y_TRANSLATION_STD] = 0;
            data[(int)ShapeParamsNorm.Z_TRANSLATION_MEAN] = 0;
            data[(int)ShapeParamsNorm.Z_TRANSLATION_STD] = 0;

            data[(int)ShapeParamsNorm.X_INIT_POSITION_MEAN] = 0.5f;
            data[(int)ShapeParamsNorm.X_INIT_POSITION_STD] = 0;
            data[(int)ShapeParamsNorm.Y_INIT_POSITION_MEAN] = 0.5f;
            data[(int)ShapeParamsNorm.Y_INIT_POSITION_STD] = 0;
            data[(int)ShapeParamsNorm.Z_INIT_POSITION_MEAN] = 0.5f;
            data[(int)ShapeParamsNorm.Z_INIT_POSITION_STD] = 0;

            data[(int)ShapeParamsNorm.X_SCALE_MEAN] = 1;
            data[(int)ShapeParamsNorm.X_SCALE_STD] = 0;
            data[(int)ShapeParamsNorm.Y_SCALE_MEAN] = 1;
            data[(int)ShapeParamsNorm.Y_SCALE_STD] = 0;
            data[(int)ShapeParamsNorm.Z_SCALE_MEAN] = 1;
            data[(int)ShapeParamsNorm.Z_SCALE_STD] = 0;

            data[(int)ShapeParamsNorm.SURFACE_NOISE_MEAN] = 0;
            data[(int)ShapeParamsNorm.SURFACE_NOISE_STD] = 0;
        }
        else
        {
            for (int i = 0; i < (int)ShapeParamsNorm.COUNT; i++)
            {
                if (i == (int)ShapeParamsNorm.X_INIT_POSITION_MEAN || i == (int)ShapeParamsNorm.Y_INIT_POSITION_MEAN || i == (int)ShapeParamsNorm.Z_INIT_POSITION_MEAN)
                    data[i] = 0.5f;
                else
                    data[i] = 0;
            }
        }
    }
}

struct UIParamInfo
{
    public string GroupName;
    public string SliderName;
    public string InputName;
    public float Min;
    public float Max;

    public UIParamInfo(string groupName, string sliderName, string inputName, float min, float max)
    {
        GroupName = groupName;
        SliderName = sliderName;
        InputName = inputName;
        Min = min;
        Max = max;
    }
}
