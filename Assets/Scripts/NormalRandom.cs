using System;
using UnityEngine;

public class NormalRandom : MonoBehaviour
{
    // Sample a normal random value using Box-Muller
    public static float Sample(System.Random rng)
    {
        float u1 = (float)rng.NextDouble();
        float u2 = (float)rng.NextDouble();

        float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        return z0;
    }

    // Approximate CDF of the normal distribution
    public static float CDF(float x)
    {
        return 0.5f * (1f + Erf(x / Mathf.Sqrt(2f)));
    }

    // Approximate error function (erf)
    // Abramowitz & Stegun formula 7.1.26
    public static float Erf(float x)
    {
        // Constants
        float a1 = 0.254829592f;
        float a2 = -0.284496736f;
        float a3 = 1.421413741f;
        float a4 = -1.453152027f;
        float a5 = 1.061405429f;
        float p = 0.3275911f;

        int sign = x < 0 ? -1 : 1;
        x = Mathf.Abs(x);

        float t = 1f / (1f + p * x);
        float y = 1f - (((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t) * Mathf.Exp(-x * x);

        return sign * y;
    }

    // Calculate two-tailed p-value for a given value
    public static float PValue(float x)
    {
        float cdf = CDF(x);
        float p = x > 0 ? 2f * (1f - cdf) : 2f * cdf;
        return p;
    }

    public static float ChiSquaredCDF(float x, int k)
    {
        if (x <= 0f) return 0f;

        float a = k * 0.5f;
        float gamma = GammaLowerRegularized(a, x * 0.5f);
        return Mathf.Clamp01(gamma);
    }

    public static float GammaLowerRegularized(float a, float x)
    {
        const int maxIterations = 100;
        const float epsilon = 1e-6f;

        if (x < 0 || a <= 0) return 0f;

        if (x < a + 1f)
        {
            // Series expansion
            float ap = a;
            float sum = 1f / a;
            float del = sum;

            for (int n = 1; n <= maxIterations; n++)
            {
                ap += 1f;
                del *= x / ap;
                sum += del;
                if (Mathf.Abs(del) < Mathf.Abs(sum) * epsilon)
                    break;
            }

            return sum * Mathf.Exp(-x + a * Mathf.Log(x) - LogGamma(a));
        }
        else
        {
            // Continued fraction
            float b = x + 1f - a;
            float c = 1f / 1e-30f;
            float d = 1f / b;
            float h = d;

            for (int i = 1; i <= maxIterations; i++)
            {
                float an = -i * (i - a);
                b += 2f;
                d = an * d + b;
                if (Mathf.Abs(d) < 1e-30f) d = 1e-30f;
                c = b + an / c;
                if (Mathf.Abs(c) < 1e-30f) c = 1e-30f;
                d = 1f / d;
                float delta = d * c;
                h *= delta;
                if (Mathf.Abs(delta - 1f) < epsilon)
                    break;
            }

            return 1f - h * Mathf.Exp(-x + a * Mathf.Log(x) - LogGamma(a));
        }
    }

    public static float LogGamma(float x)
    {
        float[] coef = {
            76.18009173f, -86.50532033f, 24.01409822f,
            -1.231739516f, 0.00120858003f, -0.00000536382f
        };

        float y = x;
        float tmp = x + 5.5f;
        tmp -= (x + 0.5f) * Mathf.Log(tmp);
        float ser = 1.000000000190015f;

        for (int j = 0; j < coef.Length; j++)
        {
            y += 1f;
            ser += coef[j] / y;
        }

        return -tmp + Mathf.Log(2.50662827465f * ser / x);
    }
}
