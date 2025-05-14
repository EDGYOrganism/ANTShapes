using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class FrameDifferenceCPU : MonoBehaviour
{
    public static void ComputeFrameDifference(RenderTexture currentFrame, RenderTexture prevFrame, RenderTexture result, float spikeThreshold)
    {
        int width = currentFrame.width;
        int height = currentFrame.height;
        int pixelCount = width * height;

        NativeArray<Color32> currPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);
        NativeArray<Color32> prevPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);
        NativeArray<Color32> resultPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);

        // Convert RenderTexture to Texture2D and extract pixels
        Texture2D currTex = Utils.RenderTextureToTexture2D(currentFrame);
        Texture2D prevTex = Utils.RenderTextureToTexture2D(prevFrame);

        currPixels.CopyFrom(currTex.GetPixels32());
        prevPixels.CopyFrom(prevTex.GetPixels32());

        // Dispose temporary textures
        Object.Destroy(currTex);
        Object.Destroy(prevTex);

        // Schedule the job
        FrameDifferenceJob job = new FrameDifferenceJob
        {
            currPixels = currPixels,
            prevPixels = prevPixels,
            resultPixels = resultPixels,
            thresh = spikeThreshold
        };

        JobHandle handle = job.Schedule(pixelCount, 64);
        handle.Complete();

        // Create result texture
        Texture2D resultTex = new Texture2D(width, height, TextureFormat.RGB24, false);
        resultTex.SetPixels32(resultPixels.ToArray());
        resultTex.Apply();

        // Copy result to the output RenderTexture
        Graphics.Blit(resultTex, result);

        // Cleanup
        resultPixels.Dispose();
        currPixels.Dispose();
        prevPixels.Dispose();
        Object.Destroy(resultTex);
    }

    [BurstCompile]
    private struct FrameDifferenceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Color32> currPixels;
        [ReadOnly] public NativeArray<Color32> prevPixels;
        [WriteOnly] public NativeArray<Color32> resultPixels;
        [ReadOnly] public float thresh;

        public void Execute(int index)
        {
            Color32 currColor = currPixels[index];
            Color32 prevColor = prevPixels[index];

            // Convert Color32 to [0, 1] normalized float values
            float currR = currColor.r / 255f;
            float currG = currColor.g / 255f;
            float currB = currColor.b / 255f;
            float prevR = prevColor.r / 255f;
            float prevG = prevColor.g / 255f;
            float prevB = prevColor.b / 255f;

            // Calculate grayscale values
            float currGray = Greyscale(currR, currG, currB);
            float prevGray = Greyscale(prevR, prevG, prevB);
            
            // Calculate the difference
            float diff = currGray - prevGray;

            // Apply the spike threshold logic
            float outputValue = diff > thresh ? 1 : (diff < -thresh ? -1 : 0);
            outputValue = (outputValue + 1) * 0.5f; // Normalize to [0, 1]

            byte byteValue = (byte)(outputValue * 255); // Convert to byte range [0, 255]
            resultPixels[index] = new Color32(byteValue, byteValue, byteValue, 255);
        }

        // Greyscale calculation: using weighted sum of RGB components
        private static float Greyscale(float r, float g, float b)
        {
            return r * 0.299f + g * 0.587f + b * 0.114f;
        }
    }

    public static bool IsGPUAvailable()
    {
        try
        {
            return SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
        }
        catch
        {
            return false;
        }
    }
}
