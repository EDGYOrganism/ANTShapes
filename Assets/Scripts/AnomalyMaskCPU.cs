using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

public static class AnomalyMaskCPU
{
    [BurstCompile]
    public struct AnomalyMaskJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Color32> currentFrame;
        [ReadOnly] public NativeArray<Color32> prevFrame;
        public NativeArray<Color32> resultFrame;
        public int totalPixels;

        public void Execute(int index)
        {
            if (index >= totalPixels) return;

            Color32 currentColor = currentFrame[index];
            Color32 prevColor = prevFrame[index];

            Color32 accumulatedColor = Utils.SumColor32(currentColor, prevColor);

            if (accumulatedColor.r == 0 && accumulatedColor.g == 0 && accumulatedColor.b == 0)
            {
                accumulatedColor.a = 0;
            }
            else
            {
                accumulatedColor.a = 127;
            }

            resultFrame[index] = accumulatedColor;
        }
    }

    public static void CreateAnomalyMask(RenderTexture currentFrame, RenderTexture prevFrame, RenderTexture result)
    {
        int width = currentFrame.width;
        int height = currentFrame.height;
        int pixelCount = width * height;

        // Get pixel data from the textures
        NativeArray<Color32> currPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);
        NativeArray<Color32> prevPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);

        Texture2D currTex = Utils.RenderTextureToTexture2D(currentFrame);
        Texture2D prevTex = Utils.RenderTextureToTexture2D(prevFrame);

        currPixels.CopyFrom(currTex.GetPixels32());
        prevPixels.CopyFrom(prevTex.GetPixels32());

        // Dispose temporary textures
        Object.Destroy(currTex);
        Object.Destroy(prevTex);
    
        // Create an array to store the result
        NativeArray<Color32> resultPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);

        // Create the job
        AnomalyMaskJob job = new AnomalyMaskJob
        {
            currentFrame = currPixels,
            prevFrame = prevPixels,
            resultFrame = resultPixels,
            totalPixels = pixelCount
        };

        // Schedule the job to run in parallel
        JobHandle handle = job.Schedule(resultPixels.Length, 64); // 64 is the batch size for parallel execution
        handle.Complete(); // Wait for the job to finish

        // Create result texture
        Texture2D resultTex = new Texture2D(width, height, TextureFormat.RGB24, false);
        resultTex.SetPixels32(resultPixels.ToArray());
        resultTex.Apply();

        // Copy result to the output RenderTexture
        Graphics.Blit(resultTex, result);

        // Dispose of the NativeArrays after use
        currPixels.Dispose();
        prevPixels.Dispose();
        resultPixels.Dispose();
    }
}
