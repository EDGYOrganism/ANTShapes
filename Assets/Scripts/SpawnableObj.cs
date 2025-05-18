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

    public void SetRotation()
    {
        foreach (var axis in new[] { (ShapeParams.X_ROTATION, ShapeParamsNorm.X_ROTATION_MEAN, ShapeParamsNorm.X_ROTATION_STD),
                                     (ShapeParams.Y_ROTATION, ShapeParamsNorm.Y_ROTATION_MEAN, ShapeParamsNorm.Y_ROTATION_STD),
                                     (ShapeParams.Z_ROTATION, ShapeParamsNorm.Z_ROTATION_MEAN, ShapeParamsNorm.Z_ROTATION_STD) })
        {
            int shapeParam = (int)axis.Item1;
            
            // Local
            thisShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            thisShapeParamValues[shapeParam] = TransformSampledValue(thisShapeParamValuesRaw[shapeParam], 
                                                                     ReadShapeParam(axis.Item2), 
                                                                     ReadShapeParam(axis.Item3));

            // Global
            globalShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            globalShapeParamValues[shapeParam] = TransformSampledValue(globalShapeParamValuesRaw[shapeParam], 
                                                                       ReadGlobalShapeParam(axis.Item2), 
                                                                       ReadGlobalShapeParam(axis.Item3));
        }

        targetRotation = new Vector3(thisShapeParamValues[(int)ShapeParams.X_ROTATION], thisShapeParamValues[(int)ShapeParams.Y_ROTATION], thisShapeParamValues[(int)ShapeParams.Z_ROTATION]) + 
                         new Vector3(globalShapeParamValues[(int)ShapeParams.X_ROTATION], globalShapeParamValues[(int)ShapeParams.Y_ROTATION], globalShapeParamValues[(int)ShapeParams.Z_ROTATION]);
        rotation = targetRotation;

        
    }

    public void SetInitialRotation()
    {
        foreach (var axis in new[] { ((int)ShapeParams.X_INIT_ROTATION, ShapeParamsNorm.X_INIT_ROTATION_MEAN, ShapeParamsNorm.X_INIT_ROTATION_STD),
                                     ((int)ShapeParams.Y_INIT_ROTATION, ShapeParamsNorm.Y_INIT_ROTATION_MEAN, ShapeParamsNorm.Y_INIT_ROTATION_STD),
                                     ((int)ShapeParams.Z_INIT_ROTATION, ShapeParamsNorm.Z_INIT_ROTATION_MEAN, ShapeParamsNorm.Z_INIT_ROTATION_STD) })
        {
            int shapeParam = (int)axis.Item1;
            
            // Local
            thisShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            thisShapeParamValues[shapeParam] = TransformSampledValueWithoutFuzziness(thisShapeParamValuesRaw[shapeParam], 
                                                                                     ReadShapeParam(axis.Item2), 
                                                                                     ReadShapeParam(axis.Item3));

            // Global
            globalShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            globalShapeParamValues[shapeParam] = TransformSampledValueWithoutFuzziness(globalShapeParamValuesRaw[shapeParam], 
                                                                                       ReadGlobalShapeParam(axis.Item2), 
                                                                                       ReadGlobalShapeParam(axis.Item3));
        }

        gameObject.transform.localEulerAngles = new Vector3(thisShapeParamValues[(int)ShapeParams.X_INIT_ROTATION], thisShapeParamValues[(int)ShapeParams.Y_INIT_ROTATION], thisShapeParamValues[(int)ShapeParams.Z_INIT_ROTATION]) + 
                                                new Vector3(globalShapeParamValues[(int)ShapeParams.X_INIT_ROTATION], globalShapeParamValues[(int)ShapeParams.Y_INIT_ROTATION], globalShapeParamValues[(int)ShapeParams.Z_INIT_ROTATION]);
    }

    public void SetTranslation()
    {
        foreach (var axis in new[] { ((int)ShapeParams.X_TRANSLATION, ShapeParamsNorm.X_TRANSLATION_MEAN, ShapeParamsNorm.X_TRANSLATION_STD),
                                     ((int)ShapeParams.Y_TRANSLATION, ShapeParamsNorm.Y_TRANSLATION_MEAN, ShapeParamsNorm.Y_TRANSLATION_STD),
                                     ((int)ShapeParams.Z_TRANSLATION, ShapeParamsNorm.Z_TRANSLATION_MEAN, ShapeParamsNorm.Z_TRANSLATION_STD) })
        {
            int shapeParam = (int)axis.Item1;
            
            // Local
            thisShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            thisShapeParamValues[shapeParam] = TransformSampledValue(thisShapeParamValuesRaw[shapeParam], 
                                                                     ReadShapeParam(axis.Item2), 
                                                                     ReadShapeParam(axis.Item3));

            // Global
            globalShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            globalShapeParamValues[shapeParam] = TransformSampledValue(globalShapeParamValuesRaw[shapeParam], 
                                                                       ReadGlobalShapeParam(axis.Item2), 
                                                                       ReadGlobalShapeParam(axis.Item3));
        }

        translation = new Vector3(thisShapeParamValues[(int)ShapeParams.X_TRANSLATION], thisShapeParamValues[(int)ShapeParams.Y_TRANSLATION], thisShapeParamValues[(int)ShapeParams.Z_TRANSLATION]) + 
                      new Vector3(globalShapeParamValues[(int)ShapeParams.X_TRANSLATION], globalShapeParamValues[(int)ShapeParams.Y_TRANSLATION], globalShapeParamValues[(int)ShapeParams.Z_TRANSLATION]);
        translation *= 0.037f;
    }

    public void SetInitialPosition()
    {
        foreach (var axis in new[] { ((int)ShapeParams.X_INIT_POSITION, ShapeParamsNorm.X_INIT_POSITION_MEAN, ShapeParamsNorm.X_INIT_POSITION_STD),
                                     ((int)ShapeParams.Y_INIT_POSITION, ShapeParamsNorm.Y_INIT_POSITION_MEAN, ShapeParamsNorm.Y_INIT_POSITION_STD),
                                     ((int)ShapeParams.Z_INIT_POSITION, ShapeParamsNorm.Z_INIT_POSITION_MEAN, ShapeParamsNorm.Z_INIT_POSITION_STD) })
        {
            int shapeParam = (int)axis.Item1;
            
            // Local
            thisShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            thisShapeParamValues[shapeParam] = TransformSampledValueWithoutFuzziness(thisShapeParamValuesRaw[shapeParam], 
                                                                                     ReadShapeParam(axis.Item2), 
                                                                                     ReadShapeParam(axis.Item3));

            // Global
            globalShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            globalShapeParamValues[shapeParam] = TransformSampledValueWithoutFuzziness(globalShapeParamValuesRaw[shapeParam], 
                                                                                       ReadGlobalShapeParam(axis.Item2), 
                                                                                       ReadGlobalShapeParam(axis.Item3));
        }

        gameObject.transform.position = CalculateInitialWorldPos();
    }

    public void SetScale()
    {
        foreach (var axis in new[] { ((int)ShapeParams.X_SCALE, ShapeParamsNorm.X_SCALE_MEAN, ShapeParamsNorm.X_SCALE_STD),
                                     ((int)ShapeParams.Y_SCALE, ShapeParamsNorm.Y_SCALE_MEAN, ShapeParamsNorm.Y_SCALE_STD),
                                     ((int)ShapeParams.Z_SCALE, ShapeParamsNorm.Z_SCALE_MEAN, ShapeParamsNorm.Z_SCALE_STD) })
        {
            int shapeParam = (int)axis.Item1;
            
            // Local
            thisShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            thisShapeParamValues[shapeParam] = TransformSampledValue(thisShapeParamValuesRaw[shapeParam], 
                                                                     ReadShapeParam(axis.Item2), 
                                                                     ReadShapeParam(axis.Item3));

            // Global
            globalShapeParamValuesRaw[shapeParam] = NormalRandom.Sample(sceneController.rngTransform);
            globalShapeParamValues[shapeParam] = TransformSampledValue(globalShapeParamValuesRaw[shapeParam], 
                                                                       ReadGlobalShapeParam(axis.Item2), 
                                                                       ReadGlobalShapeParam(axis.Item3));
        }
        
        Vector3 localScale = new Vector3(thisShapeParamValues[(int)ShapeParams.X_SCALE], thisShapeParamValues[(int)ShapeParams.Y_SCALE], thisShapeParamValues[(int)ShapeParams.Z_SCALE]) + 
                             new Vector3(globalShapeParamValues[(int)ShapeParams.X_SCALE], globalShapeParamValues[(int)ShapeParams.Y_SCALE], globalShapeParamValues[(int)ShapeParams.Z_SCALE]) / 2f;
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

    public float EvaluateNormalPValue(ShapeParams param)
    {
        float std = 0;
        float p = 0;
        int n = 0;

        ShapeParamsNorm[] normParams = Utils.ShapeParamToShapeParamNorm(param);

        // Compute probability of shape-exclusive parameters
        std = sceneController.ReadShapeParam((ShapeParamLabels)shape, normParams[1]);
        if (std > 0 && sceneController.ReadShapeParam((ShapeParamLabels)shape, normParams[2]) > 0)
        {
            p += -2f * Mathf.Log(NormalRandom.PValue(thisShapeParamValuesRaw[(int)param]));
            n++;
        }

        // Sum with probability from global parameters
        std = sceneController.ReadShapeParam(ShapeParamLabels.GLOBAL, normParams[1]);
        if (std > 0 && sceneController.ReadShapeParam(ShapeParamLabels.GLOBAL, normParams[2]) > 0)
        {
            p += -2f * Mathf.Log(NormalRandom.PValue(globalShapeParamValuesRaw[(int)param]));
            n++;
        }

        return NormalRandom.ChiSquaredCDF(p, n * 2);
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

    public float EvaluateOverallPValue()
    {
        float chiSquareStat = 0f;
        int n = 0;

        for (int i = 0; i < (int)ShapeParams.COUNT; i++)
        {
            ShapeParams param = (ShapeParams)i;

            float pVal = EvaluateNormalPValue(param);
            
            if (pVal > 0)
            {
                chiSquareStat += -2f * Mathf.Log(pVal);
                n++;
            }
        }

        float combinedPValue = NormalRandom.ChiSquaredCDF(chiSquareStat, 2 * n);

        if (combinedPValue <= 0)
            combinedPValue = 1;

        return combinedPValue * GetShapeProbability();
    }
}
