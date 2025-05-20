using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class SpawnableObj : MonoBehaviour
{
    private int id;
    private SceneController sceneController;
    public Shapes shape;
    public MeshFilter meshFilter;
    public Renderer meshRenderer;
    public float[] thisShapeParamValues, thisShapeParamValuesRaw;
    public float[] globalShapeParamValues, globalShapeParamValuesRaw;
    public Vector3 rotation, targetRotation;
    public Vector3 translation;
    public Vector3 rebounding = Vector3.zero;
    public bool respawning = false;
    public float pValue;
    private MaterialPropertyBlock propertyBlock;

    public void Init(int id, SceneController sceneController)
    {
        this.id = id;
        this.sceneController = sceneController;

        int count = (int)ShapeParams.COUNT;
        thisShapeParamValues = new float[count];
        thisShapeParamValuesRaw = new float[count];
        globalShapeParamValues = new float[count];
        globalShapeParamValuesRaw = new float[count];

        SetBehavior(false);
    }

    public void UpdateFrame()
    {
        UpdateMovement();

        gameObject.transform.Rotate(rotation * sceneController.timeScale);
        gameObject.transform.position += translation * sceneController.timeScale;

        bool[] outOfView = IsOutOfView();

        // Object has just left view
        if ((outOfView[0] && !sceneController.reboundOnEdges[0]) ||
            (outOfView[1] && !sceneController.reboundOnEdges[1]) ||
            (outOfView[2] && !sceneController.reboundOnEdges[2]) ||
            (outOfView[3] && !sceneController.reboundOnEdges[3]) ||
            (outOfView[4] && !sceneController.reboundOnEdges[4]) ||
            (outOfView[5] && !sceneController.reboundOnEdges[5]))
        {
            if (!respawning)
                SetBehavior(true);
        }
        // Objects in view have their respawn flag cleared
        else
        {
            respawning = false;
        }
    }

    void UpdateMovement()
    {
        Vector3 toRebound = ShouldReboundOnAxis();

        // Reflect on the X-axis
        if (toRebound.x > 0)
        {
            if (rebounding.x == 0)
            {
                translation.x = -translation.x;
                targetRotation.y = -targetRotation.y; // Flip rotation on Y-axis for natural reflection
                rebounding.x = 1;
            }
        }
        else if (rebounding.x == 1)
            rebounding.x = 0;

        // Reflect on the Y-axis
        if (toRebound.y > 0)
        {
            if (rebounding.y == 0)
            {
                translation.y = -translation.y;
                targetRotation.x = -targetRotation.x; // Flip rotation on X-axis for natural Reflection
                rebounding.y = 1;
            }
        }
        else if (rebounding.y == 1)
            rebounding.y = 0;

        // Reflect on the Z-axis
        if (toRebound.z > 0)
        {
            if (rebounding.z == 0)
            {
                translation.z = -translation.z;
                targetRotation.y = -targetRotation.y; // Flip rotation on Y-axis for natural reflection
                rebounding.z = 1;
            }
        }
        else if (rebounding.z == 1)
            rebounding.z = 0;

        // Rotate towards target rotation
        rotation.x = Mathf.Lerp(rotation.x, targetRotation.x, sceneController.timeScale);
        rotation.y = Mathf.Lerp(rotation.y, targetRotation.y, sceneController.timeScale);
        rotation.z = Mathf.Lerp(rotation.z, targetRotation.z, sceneController.timeScale);
    }

    public void SetBehavior(bool isRespawning)
    {
        // Set respawning flag
        respawning = isRespawning;

        SetRandomShape();
        SetRotation();
        SetInitialRotation();
        SetTranslation();
        SetScale();
        SetSurfaceNoise();

        SetInitialPosition();
        if (respawning)
            SetOffscreenPosition();

        pValue = EvaluateOverallPValue();
    }

    float ReadShapeParam(ShapeParamsNorm param)
    {
        return sceneController.ReadShapeParam((ShapeParamLabels)shape, param);
    }

    float ReadGlobalShapeParam(ShapeParamsNorm param)
    {
        return sceneController.ReadShapeParam(ShapeParamLabels.GLOBAL, param);
    }

    public void SetRandomShape()
    {
        int shape_id = sceneController.GetWeightedRandomShapeID();
        SetShape((Shapes)shape_id, sceneController.meshes[shape_id]);
    }

    public void SetShape(Shapes shape, Mesh mesh)
    {
        gameObject.name = $"Spawned_{id}_{shape}";
        this.shape = shape;
        meshFilter.sharedMesh = mesh;
    }

    float TransformSampledValue(float val, float mean, float std)
    {
        float lin = val - sceneController.pThreshold;
        float fuzziness = sceneController.fuzziness;
        float qd = PowTransform(val);

        val = qd * (1 - fuzziness) + lin * fuzziness;
        return val * std + mean;
    }

    float PowTransform(float val)
    {
        float pThresh = sceneController.pThreshold;
        if (val <= -pThresh)
            return Mathf.Pow(val + pThresh, 3);
        else if (val >= pThresh)
            return Mathf.Pow(val - pThresh, 3);
        else
            return 0;

    }

    float TransformSampledValueWithoutFuzziness(float val, float mean, float std)
    {
        return val * std + mean;
    }

    private Vector3 ProcessShapeParams(
        ShapeParamsNorm xMeanParam, ShapeParamsNorm yMeanParam, ShapeParamsNorm zMeanParam,
        ShapeParamsNorm xStdParam, ShapeParamsNorm yStdParam, ShapeParamsNorm zStdParam,
        ShapeParams xRawParam, ShapeParams yRawParam, ShapeParams zRawParam,
        ShapeParams xParam, ShapeParams yParam, ShapeParams zParam,
        bool useGlobal, bool useFuzziness = true)
    {
        Vector3 mean = new Vector3(
            useGlobal ? ReadGlobalShapeParam(xMeanParam) : ReadShapeParam(xMeanParam),
            useGlobal ? ReadGlobalShapeParam(yMeanParam) : ReadShapeParam(yMeanParam),
            useGlobal ? ReadGlobalShapeParam(zMeanParam) : ReadShapeParam(zMeanParam)
        );

        Vector3 std = new Vector3(
            useGlobal ? ReadGlobalShapeParam(xStdParam) : ReadShapeParam(xStdParam),
            useGlobal ? ReadGlobalShapeParam(yStdParam) : ReadShapeParam(yStdParam),
            useGlobal ? ReadGlobalShapeParam(zStdParam) : ReadShapeParam(zStdParam)
        );

        Vector3 sample = new Vector3(
            NormalRandom.Sample(sceneController.rngTransform),
            NormalRandom.Sample(sceneController.rngTransform),
            NormalRandom.Sample(sceneController.rngTransform)
        );

        if (useGlobal)
        {
            globalShapeParamValuesRaw[(int)xRawParam] = sample.x;
            globalShapeParamValuesRaw[(int)yRawParam] = sample.y;
            globalShapeParamValuesRaw[(int)zRawParam] = sample.z;
        }
        else
        {
            thisShapeParamValuesRaw[(int)xRawParam] = sample.x;
            thisShapeParamValuesRaw[(int)yRawParam] = sample.y;
            thisShapeParamValuesRaw[(int)zRawParam] = sample.z;
        }

        if (useFuzziness)
        {
            sample.x = TransformSampledValue(sample.x, mean.x, std.x);
            sample.y = TransformSampledValue(sample.y, mean.y, std.y);
            sample.z = TransformSampledValue(sample.z, mean.z, std.z);
        }
        else
        {
            sample.x = TransformSampledValueWithoutFuzziness(sample.x, mean.x, std.x);
            sample.y = TransformSampledValueWithoutFuzziness(sample.y, mean.y, std.y);
            sample.z = TransformSampledValueWithoutFuzziness(sample.z, mean.z, std.z);
        }

        if (useGlobal)
        {
            globalShapeParamValues[(int)xParam] = sample.x;
            globalShapeParamValues[(int)yParam] = sample.y;
            globalShapeParamValues[(int)zParam] = sample.z;
        }
        else
        {
            thisShapeParamValues[(int)xParam] = sample.x;
            thisShapeParamValues[(int)yParam] = sample.y;
            thisShapeParamValues[(int)zParam] = sample.z;
        }

        return sample;
    }

    public void SetRotation()
    {
        Vector3 localSample = ProcessShapeParams(
            ShapeParamsNorm.X_ROTATION_MEAN, ShapeParamsNorm.Y_ROTATION_MEAN, ShapeParamsNorm.Z_ROTATION_MEAN,
            ShapeParamsNorm.X_ROTATION_STD, ShapeParamsNorm.Y_ROTATION_STD, ShapeParamsNorm.Z_ROTATION_STD,
            ShapeParams.X_ROTATION, ShapeParams.Y_ROTATION, ShapeParams.Z_ROTATION,
            ShapeParams.X_ROTATION, ShapeParams.Y_ROTATION, ShapeParams.Z_ROTATION,
            useGlobal: false
        );

        Vector3 globalSample = ProcessShapeParams(
            ShapeParamsNorm.X_ROTATION_MEAN, ShapeParamsNorm.Y_ROTATION_MEAN, ShapeParamsNorm.Z_ROTATION_MEAN,
            ShapeParamsNorm.X_ROTATION_STD, ShapeParamsNorm.Y_ROTATION_STD, ShapeParamsNorm.Z_ROTATION_STD,
            ShapeParams.X_ROTATION, ShapeParams.Y_ROTATION, ShapeParams.Z_ROTATION,
            ShapeParams.X_ROTATION, ShapeParams.Y_ROTATION, ShapeParams.Z_ROTATION,
            useGlobal: true
        );

        targetRotation = localSample + globalSample;
        rotation = targetRotation;
    }

    public void SetInitialRotation()
    {
        Vector3 localSample = ProcessShapeParams(
            ShapeParamsNorm.X_INIT_ROTATION_MEAN, ShapeParamsNorm.Y_INIT_ROTATION_MEAN, ShapeParamsNorm.Z_INIT_ROTATION_MEAN,
            ShapeParamsNorm.X_INIT_ROTATION_STD, ShapeParamsNorm.Y_INIT_ROTATION_STD, ShapeParamsNorm.Z_INIT_ROTATION_STD,
            ShapeParams.X_INIT_ROTATION, ShapeParams.Y_INIT_ROTATION, ShapeParams.Z_INIT_ROTATION,
            ShapeParams.X_INIT_ROTATION, ShapeParams.Y_INIT_ROTATION, ShapeParams.Z_INIT_ROTATION,
            useGlobal: false, useFuzziness: false
        );

        Vector3 globalSample = ProcessShapeParams(
            ShapeParamsNorm.X_INIT_ROTATION_MEAN, ShapeParamsNorm.Y_INIT_ROTATION_MEAN, ShapeParamsNorm.Z_INIT_ROTATION_MEAN,
            ShapeParamsNorm.X_INIT_ROTATION_STD, ShapeParamsNorm.Y_INIT_ROTATION_STD, ShapeParamsNorm.Z_INIT_ROTATION_STD,
            ShapeParams.X_INIT_ROTATION, ShapeParams.Y_INIT_ROTATION, ShapeParams.Z_INIT_ROTATION,
            ShapeParams.X_INIT_ROTATION, ShapeParams.Y_INIT_ROTATION, ShapeParams.Z_INIT_ROTATION,
            useGlobal: true, useFuzziness: false
        );

        gameObject.transform.localEulerAngles = localSample + globalSample;
    }

    public void SetTranslation()
    {
        Vector3 localSample = ProcessShapeParams(
            ShapeParamsNorm.X_TRANSLATION_MEAN, ShapeParamsNorm.Y_TRANSLATION_MEAN, ShapeParamsNorm.Z_TRANSLATION_MEAN,
            ShapeParamsNorm.X_TRANSLATION_STD, ShapeParamsNorm.Y_TRANSLATION_STD, ShapeParamsNorm.Z_TRANSLATION_STD,
            ShapeParams.X_TRANSLATION, ShapeParams.Y_TRANSLATION, ShapeParams.Z_TRANSLATION,
            ShapeParams.X_TRANSLATION, ShapeParams.Y_TRANSLATION, ShapeParams.Z_TRANSLATION,
            useGlobal: false
        );

        Vector3 globalSample = ProcessShapeParams(
            ShapeParamsNorm.X_TRANSLATION_MEAN, ShapeParamsNorm.Y_TRANSLATION_MEAN, ShapeParamsNorm.Z_TRANSLATION_MEAN,
            ShapeParamsNorm.X_TRANSLATION_STD, ShapeParamsNorm.Y_TRANSLATION_STD, ShapeParamsNorm.Z_TRANSLATION_STD,
            ShapeParams.X_TRANSLATION, ShapeParams.Y_TRANSLATION, ShapeParams.Z_TRANSLATION,
            ShapeParams.X_TRANSLATION, ShapeParams.Y_TRANSLATION, ShapeParams.Z_TRANSLATION,
            useGlobal: true
        );

        translation = (localSample + globalSample) * 0.037f;
    }

    public void SetInitialPosition()
    {
        ProcessShapeParams(
            ShapeParamsNorm.X_INIT_POSITION_MEAN, ShapeParamsNorm.Y_INIT_POSITION_MEAN, ShapeParamsNorm.Z_INIT_POSITION_MEAN,
            ShapeParamsNorm.X_INIT_POSITION_STD, ShapeParamsNorm.Y_INIT_POSITION_STD, ShapeParamsNorm.Z_INIT_POSITION_STD,
            ShapeParams.X_INIT_POSITION, ShapeParams.Y_INIT_POSITION, ShapeParams.Z_INIT_POSITION,
            ShapeParams.X_INIT_POSITION, ShapeParams.Y_INIT_POSITION, ShapeParams.Z_INIT_POSITION,
            useGlobal: false, useFuzziness: false
        );

        ProcessShapeParams(
            ShapeParamsNorm.X_INIT_POSITION_MEAN, ShapeParamsNorm.Y_INIT_POSITION_MEAN, ShapeParamsNorm.Z_INIT_POSITION_MEAN,
            ShapeParamsNorm.X_INIT_POSITION_STD, ShapeParamsNorm.Y_INIT_POSITION_STD, ShapeParamsNorm.Z_INIT_POSITION_STD,
            ShapeParams.X_INIT_POSITION, ShapeParams.Y_INIT_POSITION, ShapeParams.Z_INIT_POSITION,
            ShapeParams.X_INIT_POSITION, ShapeParams.Y_INIT_POSITION, ShapeParams.Z_INIT_POSITION,
            useGlobal: true, useFuzziness: false
        );

        gameObject.transform.position = CalculateInitialWorldPos();
    }

    public void SetScale()
    {
        Vector3 localSample = ProcessShapeParams(
            ShapeParamsNorm.X_SCALE_MEAN, ShapeParamsNorm.Y_SCALE_MEAN, ShapeParamsNorm.Z_SCALE_MEAN,
            ShapeParamsNorm.X_SCALE_STD, ShapeParamsNorm.Y_SCALE_STD, ShapeParamsNorm.Z_SCALE_STD,
            ShapeParams.X_SCALE, ShapeParams.Y_SCALE, ShapeParams.Z_SCALE,
            ShapeParams.X_SCALE, ShapeParams.Y_SCALE, ShapeParams.Z_SCALE,
            useGlobal: false
        );

        Vector3 globalSample = ProcessShapeParams(
            ShapeParamsNorm.X_SCALE_MEAN, ShapeParamsNorm.Y_SCALE_MEAN, ShapeParamsNorm.Z_SCALE_MEAN,
            ShapeParamsNorm.X_SCALE_STD, ShapeParamsNorm.Y_SCALE_STD, ShapeParamsNorm.Z_SCALE_STD,
            ShapeParams.X_SCALE, ShapeParams.Y_SCALE, ShapeParams.Z_SCALE,
            ShapeParams.X_SCALE, ShapeParams.Y_SCALE, ShapeParams.Z_SCALE,
            useGlobal: true
        );

        Vector3 localScale = (localSample + globalSample) / 2f;
        localScale.x = Mathf.Clamp(localScale.x, 0.25f, 2f);
        localScale.y = Mathf.Clamp(localScale.y, 0.25f, 2f);
        localScale.z = Mathf.Clamp(localScale.z, 0.25f, 2f);

        gameObject.transform.localScale = localScale;
    }

    public void SetSurfaceNoise()
    {
        float thisSurfaceNoise = NormalRandom.Sample(sceneController.rngTransform);
        thisShapeParamValuesRaw[(int)ShapeParams.SURFACE_NOISE] = thisSurfaceNoise;
        thisShapeParamValues[(int)ShapeParams.SURFACE_NOISE] = TransformSampledValue(thisSurfaceNoise, ReadShapeParam(ShapeParamsNorm.SURFACE_NOISE_MEAN), ReadShapeParam(ShapeParamsNorm.SURFACE_NOISE_STD));

        float globalSurfaceNoise = NormalRandom.Sample(sceneController.rngTransform);
        globalShapeParamValuesRaw[(int)ShapeParams.SURFACE_NOISE] = globalSurfaceNoise;
        globalShapeParamValues[(int)ShapeParams.SURFACE_NOISE] = TransformSampledValue(globalSurfaceNoise, ReadGlobalShapeParam(ShapeParamsNorm.SURFACE_NOISE_MEAN), ReadGlobalShapeParam(ShapeParamsNorm.SURFACE_NOISE_STD));
    }

    Vector3 FixZPositionToCamera(Vector3 pos)
    {
        float zOffset = meshRenderer.bounds.size.z / 2;

        pos.z += sceneController.zCameraClip + zOffset;

        if (pos.z <= sceneController.zCameraClip + zOffset)
            pos.z = sceneController.zCameraClip + zOffset;
        if (pos.z >= sceneController.zSpawnBoundary - zOffset)
            pos.z = sceneController.zSpawnBoundary - zOffset;

        return pos;
    }

    public void SetOffscreenPosition(float stepSize = 1f, int maxSteps = 1000)
    {
        // Simulate moving backwards along the movement vector until the object is fully out of view
        for (int i = 0; i < maxSteps; i++)
        {
            gameObject.transform.position -= translation.normalized * stepSize;

            bool[] outOfView = IsOutOfView();

            for (int j = 0; j < 6; j++)
            {
                if (outOfView[j])
                    return;
            }
        }

        Debug.LogWarning("Could not find a fully out-of-view position in given steps.");
    }

    public void SetShader()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        // Get current values (retains existing)
        meshRenderer.GetPropertyBlock(propertyBlock);

        // Set new values
        propertyBlock.SetFloat("_pValue", pValue);
        propertyBlock.SetFloat("_pThresh", sceneController.pThreshold);
        propertyBlock.SetFloat("_fogStart", sceneController.fogStart);
        propertyBlock.SetFloat("_fogDepth", sceneController.fogDepth);
        propertyBlock.SetFloat("_sceneDepth", sceneController.sceneDepth);
        propertyBlock.SetVector("_cameraPos", sceneController.renderCamera.transform.position);
        propertyBlock.SetFloat("_zCameraDissolve", sceneController.zCameraDissolve);
        propertyBlock.SetFloat("_cleanClipping", sceneController.cleanClipping ? 1 : 0);

        float noiseValue = Mathf.Clamp01(thisShapeParamValues[(int)ShapeParams.SURFACE_NOISE] + globalShapeParamValues[(int)ShapeParams.SURFACE_NOISE]);
        propertyBlock.SetFloat("_noiseAmt", noiseValue);
        propertyBlock.SetFloat("_showAnomalies", sceneController.activeViewMode == TextureMode.Real ? 1 : 0);

        // Apply to renderer
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    Vector3 CalculateInitialWorldPos()
    {
        Vector3 thisInitPosition = new Vector3(thisShapeParamValues[(int)ShapeParams.X_INIT_POSITION], thisShapeParamValues[(int)ShapeParams.Y_INIT_POSITION], thisShapeParamValues[(int)ShapeParams.Z_INIT_POSITION]);
        Vector3 globalInitPosition = new Vector3(globalShapeParamValues[(int)ShapeParams.X_INIT_POSITION], globalShapeParamValues[(int)ShapeParams.Y_INIT_POSITION], globalShapeParamValues[(int)ShapeParams.Z_INIT_POSITION]);
        Vector3 initPosition = (thisInitPosition + globalInitPosition) / 2f;

        initPosition.x = Mathf.Clamp01(initPosition.x);
        initPosition.y = Mathf.Clamp01(initPosition.y);
        initPosition.z = sceneController.sceneDepth * Mathf.Clamp01(initPosition.z) + sceneController.zCloseBoundary;

        return MapToViewport(FixZPositionToCamera(initPosition));
    }

    public Vector3 MapToViewport(Vector3 normalizedPos)
    {
        // Convert (u, v) from [0,1] range to world space
        return sceneController.renderCamera.ViewportToWorldPoint(normalizedPos);
    }

    List<string> GetIntersectedCameraPlanes(Vector3 point)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(sceneController.renderCamera);
        Transform camTransform = sceneController.renderCamera.transform;

        // Manually adjust the Near and Far planes
        frustumPlanes[4] = new Plane(camTransform.forward, camTransform.position + camTransform.forward * sceneController.zCloseBoundary);
        frustumPlanes[5] = new Plane(-camTransform.forward, camTransform.position + camTransform.forward * sceneController.zFarBoundary);

        // To store the planes that are intersected
        List<string> intersectedPlanes = new List<string>();

        // Iterate through the planes and check the side
        for (int i = 0; i < 6; i++)
        {
            if (!frustumPlanes[i].GetSide(point))
            {
                string planeName = i switch
                {
                    0 => "-X",
                    1 => "+X",
                    2 => "-Y",
                    3 => "+Y",
                    4 => "-Z",
                    5 => "+Z",
                    _ => "Unknown"
                };

                intersectedPlanes.Add(planeName);
            }
        }

        return intersectedPlanes;
    }

    bool[] IsOutOfView()
    {
        // Calculate the 8 corners of the rotated bounding box
        Vector3[] corners = new Vector3[8];
        Bounds bounds = meshRenderer.bounds;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(min.x, min.y, max.z);
        corners[3] = new Vector3(max.x, min.y, max.z);
        corners[4] = new Vector3(min.x, max.y, min.z);
        corners[5] = new Vector3(max.x, max.y, min.z);
        corners[6] = new Vector3(min.x, max.y, max.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        float[] isOutside = { 0, 0, 0, 0, 0, 0 };

        foreach (Vector3 corner in corners)
        {
            Vector3 viewportPoint = sceneController.renderCamera.WorldToViewportPoint(corner);

            if (viewportPoint.x < 0)
                isOutside[0]++;
            else if (viewportPoint.x > 1)
                isOutside[1]++;

            if (viewportPoint.y < 0)
                isOutside[2]++;
            else if (viewportPoint.y > 1)
                isOutside[3]++;

            if (corner.z < sceneController.zCameraClip)
                isOutside[4]++;
            else if (corner.z > sceneController.zSpawnBoundary)
                isOutside[5]++;
        }

        bool[] ret = new bool[6];
        for (int i = 0; i < 6; i++)
        {
            ret[i] = isOutside[i] == 8;
        }

        return ret;
    }

    Vector3 ShouldReboundOnAxis()
    {
        // Calculate the 8 corners of the rotated bounding box
        Vector3[] corners = new Vector3[8];
        Bounds bounds = meshRenderer.bounds;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 center = bounds.center;

        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(min.x, min.y, max.z);
        corners[3] = new Vector3(max.x, min.y, max.z);
        corners[4] = new Vector3(min.x, max.y, min.z);
        corners[5] = new Vector3(max.x, max.y, min.z);
        corners[6] = new Vector3(min.x, max.y, max.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        Vector3 ret = Vector3.zero;

        foreach (Vector3 corner in corners)
        {
            List<string> intersects = GetIntersectedCameraPlanes(corner);

            if (intersects.Count == 0)
                continue;

            if ((intersects.Contains("-X") && translation.x < 0 && sceneController.reboundOnEdges[0]) || (intersects.Contains("+X") && translation.x > 0 && sceneController.reboundOnEdges[1]))
                ret.x = 1;
            if ((intersects.Contains("-Y") && translation.y < 0 && sceneController.reboundOnEdges[2]) || (intersects.Contains("+Y") && translation.y > 0 && sceneController.reboundOnEdges[3]))
                ret.y = 1;
            if ((intersects.Contains("-Z") && translation.z < 0 && sceneController.reboundOnEdges[4]) || (intersects.Contains("+Z") && translation.z > 0 && sceneController.reboundOnEdges[5]))
                ret.z = 1;
        }

        return ret;
    }

    public void Evaluate1DPValue(ref float d2, ref int k, ShapeParams param)
    {
        ShapeParamsNorm[] normParams = Utils.ShapeParamToShapeParamNorm(param);

        // Local shape parameter
        if (ReadShapeParam(normParams[2]) > 0 && ReadShapeParam(normParams[1]) > 0)
        {
            float value = thisShapeParamValuesRaw[(int)param];
            d2 += value * value; // std = 1 → value² / 1²
            k += 1;
        }

        // Global parameter
        if (ReadGlobalShapeParam(normParams[2]) > 0 && ReadGlobalShapeParam(normParams[1]) > 0)
        {
            float value = globalShapeParamValuesRaw[(int)param];
            d2 += value * value; // std = 1
            k += 1;
        }
    }

    public void Evaluate3DPValue(ref float d2, ref int k, ShapeParams paramX, ShapeParams paramY, ShapeParams paramZ)
    {
        // Helper function: no std needed now
        float AddComponent(float value) => value * value;
        float std = 0;
        float d2x, d2y, d2z;
        float n = 0;

        // X
        d2x = 0;
        if (ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramX)[2]) > 0 && ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramX)[1]) > 0)
        {
            std += ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramX)[1]);
            d2x += thisShapeParamValuesRaw[(int)paramX];
            n++;
        }
        if (ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramX)[2]) > 0 && ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramX)[1]) > 0)
        {
            std += ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramX)[1]);
            d2x += globalShapeParamValuesRaw[(int)paramX];
            n++;
        }
        if (std > 0)
        {
            d2 += AddComponent(d2x / n);
            k += 1;
        }

        // Y
        std = 0;
        d2y = 0;
        n = 0;
        if (ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramY)[2]) > 0 && ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramY)[1]) > 0)
        {
            std += ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramY)[1]);
            d2y += thisShapeParamValuesRaw[(int)paramY];
            n++;
        }
        if (ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramY)[2]) > 0 && ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramY)[1]) > 0)
        {
            std += ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramY)[1]);
            d2y += globalShapeParamValuesRaw[(int)paramY];
            n++;
        }
        if (std > 0)
        {
            d2 += AddComponent(d2y / n);
            k += 1;
        }

        // Z
        std = 0;
        d2z = 0;
        n = 0;
        if (ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramZ)[2]) > 0 && ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramZ)[1]) > 0)
        {
            std += ReadShapeParam(Utils.ShapeParamToShapeParamNorm(paramZ)[1]);
            d2z += thisShapeParamValuesRaw[(int)paramZ];
            n++;
        }
        if (ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramZ)[2]) > 0 && ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramZ)[1]) > 0)
        {
            std += ReadGlobalShapeParam(Utils.ShapeParamToShapeParamNorm(paramZ)[1]);
            d2z += globalShapeParamValuesRaw[(int)paramZ];
            n++;
        }
        if (std > 0)
        {
            d2 += AddComponent(d2z / n);
            k += 1;
        }
    }

    float EvaluateAllPValues()
    {
        float d2 = 0;
        int k = 0;

        for (int i = 0; i < (int)ShapeParams.COUNT;)
        {
            ShapeParams paramX = (ShapeParams)i;

            if (paramX == ShapeParams.SURFACE_NOISE)
            {
                Evaluate1DPValue(ref d2, ref k, paramX);
                i++;
            }
            else
            {
                ShapeParams paramY = (ShapeParams)i + 1;
                ShapeParams paramZ = (ShapeParams)i + 2;

                Evaluate3DPValue(ref d2, ref k, paramX, paramY, paramZ);
                i += 3;
            }
        }

        float overallPValue = 1 - NormalRandom.ChiSquaredCDF(d2, k);

        // Clamp to avoid edge case
        if (overallPValue <= 0)
            overallPValue = 1;

        return overallPValue;
    }

    public float EvaluateOverallPValue()
    {
        return EvaluateAllPValues() * GetShapeProbability();
    }
    
    public float GetShapeProbability()
    {
        float totalP = 0;

        // Sum all probabilities
        for (int i = 0; i < (int)Shapes.COUNT; i++)
        {
            totalP += sceneController.shapeWeights[i];
        }

        if (totalP <= 0)
            return 1;

        return sceneController.shapeWeights[(int)shape] / totalP;
    }
}
