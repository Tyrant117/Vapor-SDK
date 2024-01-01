using System;
using UnityEngine;

namespace Vapor.Tweening
{
    public static class Easing
    {
        public enum Ease
        {
            Step = 0,
            Linear,
            InSine,
            OutSine,
            InOutSine,
            InQuad,
            OutQuad,
            InOutQuad,
            InCubic,
            OutCubic,
            InOutCubic,
            InQuart,
            OutQuart,
            InOutQuart,
            InQuint,
            OutQuint,
            InOutQuint,                       
            InExpo,
            OutExpo,
            InOutExpo,
            InCirc,
            OutCirc,
            InOutCirc,
            InBack,
            OutBack,
            InOutBack,
            InBounce,
            OutBounce,
            InOutBounce,
            InElastic,
            OutElastic,
            InOutElastic,
        }

        public delegate float Function(float t);

        /// <summary>
        /// Returns the function associated to the easingFunction enum. This value returned should be cached as it allocates memory
        /// to return.
        /// </summary>
        /// <param name="easing">The enum associated with the easing function.</param>
        /// <returns>The easing function</returns>
        public static Function GetEasingFunction(Ease easing) => easing switch
        {
            Ease.Step => Step,
            Ease.Linear => Linear,
            Ease.InSine => InSine,
            Ease.OutSine => OutSine,
            Ease.InOutSine => InOutSine,
            Ease.InQuad => InQuad,
            Ease.OutQuad => OutQuad,
            Ease.InOutQuad => InOutQuad,
            Ease.InCubic => InCubic,
            Ease.OutCubic => OutCubic,
            Ease.InOutCubic => InOutCubic,
            Ease.InQuart => InQuart,
            Ease.OutQuart => OutQuart,
            Ease.InOutQuart => InOutQuart,
            Ease.InQuint => InQuint,
            Ease.OutQuint => OutQuint,
            Ease.InOutQuint => InOutQuint,
            Ease.InExpo => InExpo,
            Ease.OutExpo => OutExpo,
            Ease.InOutExpo => InOutExpo,
            Ease.InCirc => InCirc,
            Ease.OutCirc => OutCirc,
            Ease.InOutCirc => InOutCirc,
            Ease.InBack => InBack,
            Ease.OutBack => OutBack,
            Ease.InOutBack => InOutBack,
            Ease.InElastic => InElastic,
            Ease.OutElastic => OutElastic,
            Ease.InOutElastic => InOutElastic,
            Ease.InBounce => InBounce,
            Ease.OutBounce => OutBounce,
            Ease.InOutBounce => InOutBounce,
            _ => null
        };

        public static Function GetEasingFunctionDerivative(Ease easing) => easing switch
        {
            Ease.Step => null,
            Ease.Linear => Linear_Df,
            Ease.InSine => InSine_Df,
            Ease.OutSine => OutSine_Df,
            Ease.InOutSine => InOutSine_Df,
            Ease.InQuad => InQuad_Df,
            Ease.OutQuad => OutQuad_Df,
            Ease.InOutQuad => InOutQuad_Df,
            Ease.InCubic => InCubic_Df,
            Ease.OutCubic => OutCubic_Df,
            Ease.InOutCubic => InOutCubic_Df,
            Ease.InQuart => InQuart_Df,
            Ease.OutQuart => OutQuart_Df,
            Ease.InOutQuart => InOutQuart_Df,
            Ease.InQuint => InQuint_Df,
            Ease.OutQuint => OutQuint_Df,
            Ease.InOutQuint => InOutQuint_Df,
            Ease.InExpo => InExpo_Df,
            Ease.OutExpo => OutExpo_Df,
            Ease.InOutExpo => InOutExpo_Df,
            Ease.InCirc => InCirc_Df,
            Ease.OutCirc => OutCirc_Df,
            Ease.InOutCirc => InOutCirc_Df,
            Ease.InBack => InBack_Df,
            Ease.OutBack => OutBack_Df,
            Ease.InOutBack => InOutBack_Df,
            Ease.InElastic => InElastic_Df,
            Ease.OutElastic => OutElastic_Df,
            Ease.InOutElastic => InOutElastic_Df,
            Ease.InBounce => InBounce_Df,
            Ease.OutBounce => OutBounce_Df,
            Ease.InOutBounce => InOutBounce_Df,
            _ => null
        };

        private const float HalfPi = MathF.PI / 2f;
        private const float NaturalLOGOf2 = 0.693147181f;

        public static float Step(float t)
        {
            return (!(t < 0.5f)) ? 1 : 0;
        }

        public static float Linear(float t)
        {
            return t;
        }

        public static float InSine(float t)
        {
            return Mathf.Sin(HalfPi * (t - 1f)) + 1f;
        }

        public static float OutSine(float t)
        {
            return Mathf.Sin(t * HalfPi);
        }

        public static float InOutSine(float t)
        {
            return (Mathf.Sin(MathF.PI * (t - 0.5f)) + 1f) * 0.5f;
        }

        #region - Power -
        public static float InQuad(float t)
        {
            return t * t;
        }

        public static float OutQuad(float t)
        {
            return t * (2f - t);
        }

        public static float InOutQuad(float t)
        {
            t *= 2f;
            if (t < 1f)
            {
                return t * t * 0.5f;
            }

            return -0.5f * ((t - 1f) * (t - 3f) - 1f);
        }

        public static float InCubic(float t)
        {
            return InPower(t, 3);
        }

        public static float OutCubic(float t)
        {
            return OutPower(t, 3);
        }

        public static float InOutCubic(float t)
        {
            return InOutPower(t, 3);
        }

        public static float InQuart(float t)
        {
            return InPower(t, 4);
        }

        public static float OutQuart(float t)
        {
            return OutPower(t, 4);
        }

        public static float InOutQuart(float t)
        {
            return InOutPower(t, 4);
        }

        public static float InQuint(float t)
        {
            return InPower(t, 5);
        }

        public static float OutQuint(float t)
        {
            return OutPower(t, 5);
        }

        public static float InOutQuint(float t)
        {
            return InOutPower(t, 5);
        }

        public static float InExpo(float t)
        {
            return Mathf.Pow(2, 10 * (t - 1));
        }

        public static float OutExpo(float t)
        {
            return -Mathf.Pow(2, -10 * t) + 1;
        }

        public static float InOutExpo(float t)
        {
            t *= 2f;
            if (t < 1f)
            {
                return 0.5f * Mathf.Pow(2, 10 * (t - 1));
            }

            return 0.5f * (-Mathf.Pow(2, -10 * t) + 2);
        }

        public static float InPower(float t, int power)
        {
            return Mathf.Pow(t, power);
        }

        public static float OutPower(float t, int power)
        {
            var num = power % 2 != 0 ? 1 : -1;
            return num * (Mathf.Pow(t - 1f, power) + num);
        }

        public static float InOutPower(float t, int power)
        {
            t *= 2f;
            if (t < 1f)
            {
                return InPower(t, power) * 0.5f;
            }

            var num = power % 2 != 0 ? 1 : -1;
            return num * 0.5f * (Mathf.Pow(t - 2f, power) + num * 2);
        }
        #endregion

        #region - Complex -
        public static float InCirc(float t)
        {
            return 0f - (Mathf.Sqrt(1f - t * t) - 1f);
        }

        public static float OutCirc(float t)
        {
            t -= 1f;
            return Mathf.Sqrt(1f - t * t);
        }

        public static float InOutCirc(float t)
        {
            t *= 2f;
            if (t < 1f)
            {
                return -0.5f * (Mathf.Sqrt(1f - t * t) - 1f);
            }

            t -= 2f;
            return 0.5f * (Mathf.Sqrt(1f - t * t) + 1f);
        }

        public static float InBack(float t)
        {
            const float num = 1.70158f;
            return t * t * ((num + 1f) * t - num);
        }

        public static float OutBack(float t)
        {
            return 1f - InBack(1f - t);
        }

        public static float InOutBack(float t)
        {
            if (t < 0.5f)
            {
                return InBack(t * 2f) * 0.5f;
            }

            return OutBack((t - 0.5f) * 2f) * 0.5f + 0.5f;
        }

        public static float InBack(float t, float s)
        {
            return t * t * ((s + 1f) * t - s);
        }

        public static float OutBack(float t, float s)
        {
            return 1f - InBack(1f - t, s);
        }

        public static float InOutBack(float t, float s)
        {
            if (t < 0.5f)
            {
                return InBack(t * 2f, s) * 0.5f;
            }

            return OutBack((t - 0.5f) * 2f, s) * 0.5f + 0.5f;
        }

        public static float InElastic(float t)
        {
            if (t == 0f)
            {
                return 0f;
            }

            if (t == 1f)
            {
                return 1f;
            }

            const float num = 0.3f;
            const float num2 = num / 4f;
            var num3 = Mathf.Pow(2f, 10f * (t -= 1f));
            return 0f - num3 * Mathf.Sin((t - num2) * (MathF.PI * 2f) / num);
        }

        public static float OutElastic(float t)
        {
            if (t == 0f)
            {
                return 0f;
            }

            if (t == 1f)
            {
                return 1f;
            }

            float num = 0.3f;
            float num2 = num / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - num2) * (MathF.PI * 2f) / num) + 1f;
        }

        public static float InOutElastic(float t)
        {
            if (t < 0.5f)
            {
                return InElastic(t * 2f) * 0.5f;
            }

            return OutElastic((t - 0.5f) * 2f) * 0.5f + 0.5f;
        }

        public static float InBounce(float t)
        {
            return 1f - OutBounce(1f - t);
        }

        public static float OutBounce(float t)
        {
            if (t < 0.363636374f)
            {
                return 7.5625f * t * t;
            }

            if (t < 0.727272749f)
            {
                float num = (t -= 0.545454562f);
                return 7.5625f * num * t + 0.75f;
            }

            if (t < 0.909090936f)
            {
                float num2 = (t -= 0.8181818f);
                return 7.5625f * num2 * t + 0.9375f;
            }

            float num3 = (t -= 21f / 22f);
            return 7.5625f * num3 * t + 63f / 64f;
        }

        public static float InOutBounce(float t)
        {
            if (t < 0.5f)
            {
                return InBounce(t * 2f) * 0.5f;
            }

            return OutBounce((t - 0.5f) * 2f) * 0.5f + 0.5f;
        }
        #endregion

        #region - Derivatives -
        //
        // These are derived functions that the motor can use to get the speed at a specific time.
        //
        // The easing functions all work with a normalized time (0 to 1) and the returned value here
        // reflects that. Values returned here should be divided by the actual time.
        //
        #region - Simple -
        public static float Linear_Df(float t)
        {
            return 1f;
        }

        public static float InSine_Df(float t)
        {
            return HalfPi * Mathf.Cos(HalfPi * (t - 1));
        }

        public static float OutSine_Df(float t)
        {
            return HalfPi * Mathf.Cos(HalfPi * t);
        }

        public static float InOutSine_Df(float t)
        {
            return HalfPi * Mathf.Cos(Mathf.PI * (t - 0.5f));
        }
        #endregion

        #region - Power -
        public static float InPower_Df(float t, int power)
        {
            return power * Mathf.Pow(t, power - 1);
        }

        public static float OutPower_Df(float t, int power)
        {
            float num = (power % 2 != 0) ? 1f : (-1f);
            return num * power * Mathf.Pow(t - 1f, power - 1);
        }

        public static float InOutPower_Df(float t, int power)
        {
            t *= 2f;
            if (t < 1f)
            {
                return InPower_Df(t, power) * 0.5f;
            }

            float num = (power % 2 != 0) ? 1f : (-1f);
            return num * 0.5f * power * Mathf.Pow(t - 2f, power - 1);
        }

        public static float InQuad_Df(float t)
        {
            return 2f * t;
        }

        public static float OutQuad_Df(float t)
        {
            return 2 - 2 * t;
        }

        public static float InOutQuad_Df(float t)
        {
            t *= 2f;
            if (t < 1f)
            {
                return t;
            }

            return 1 - t;
        }

        public static float InCubic_Df(float t)
        {
            return InPower_Df(t, 3);
        }

        public static float OutCubic_Df(float t)
        {
            return OutPower_Df(t, 3);
        }

        public static float InOutCubic_Df(float t)
        {
            return InOutPower_Df(t, 3);
        }

        public static float InQuart_Df(float t)
        {
            return InPower_Df(t, 4);
        }

        public static float OutQuart_Df(float t)
        {
            return OutPower_Df(t, 4);
        }

        public static float InOutQuart_Df(float t)
        {
            return InOutPower_Df(t, 4);
        }

        public static float InQuint_Df(float t)
        {
            return InPower_Df(t, 5);
        }

        public static float OutQuint_Df(float t)
        {
            return OutPower_Df(t, 5);
        }

        public static float InOutQuint_Df(float t)
        {
            return InOutPower_Df(t, 5);
        }

        public static float InExpo_Df(float t)
        {
            return 10f * NaturalLOGOf2 * Mathf.Pow(2f, 10f * (t - 1));
        }

        public static float OutExpo_Df(float t)
        {
            return 5f * NaturalLOGOf2 * Mathf.Pow(2f, 1f - 10f * t);
        }

        public static float InOutExpo_Df(float t)
        {
            t *= 2f;
            if (t < 1)
            {
                return 5f * NaturalLOGOf2 * Mathf.Pow(2f, 10f * (t - 1));
            }

            return 5f * NaturalLOGOf2 / Mathf.Pow(2f, 10f * (t - 1));
        }
        #endregion

        #region - Complex -
        public static float InCirc_Df(float t)
        {
            return t / Mathf.Sqrt(1f - t * t);
        }

        public static float OutCirc_Df(float t)
        {
            t--;
            return (-t) / Mathf.Sqrt(1f - t * t);
        }

        public static float InOutCirc_Df(float t)
        {
            t *= 2f;
            if (t < 1)
            {
                return t / (2f * Mathf.Sqrt(1f - t * t));
            }

            t -= 2;
            return (-t) / (2f * Mathf.Sqrt(1f - t * t));
        }

        public static float InBack_Df(float t)
        {
            const float s = 1.70158f;
            return 3f * (s + 1f) * t * t - 2f * s * t;
        }

        public static float OutBack_Df(float t)
        {
            const float s = 1.70158f;
            t--;
            return (s + 1f) * t * t + 2f * t * ((s + 1f) * t + s);
        }

        public static float InOutBack_Df(float t)
        {
            float s = 1.70158f;
            t *= 2f;

            if (t < 1)
            {
                s *= 1.525f;
                return 0.5f * (s + 1) * t * t + t * ((s + 1f) * t - s);
            }

            t -= 2;
            s *= 1.525f;
            return 0.5f * ((s + 1) * t * t + 2f * t * ((s + 1f) * t + s));
        }

        public static float InBack_DF(float t, float s)
        {
            return t * t * ((s + 1f) * t - s);
        }

        public static float OutBack_DF(float t, float s)
        {
            t--;
            return (s + 1f) * t * t + 2f * t * ((s + 1f) * t + s);
        }

        public static float InOutBack_DF(float t, float s)
        {
            t *= 2f;

            if (t < 1)
            {
                s *= 1.525f;
                return 0.5f * (s + 1) * t * t + t * ((s + 1f) * t - s);
            }

            t -= 2;
            s *= 1.525f;
            return 0.5f * ((s + 1) * t * t + 2f * t * ((s + 1f) * t + s));
        }

        public static float InElastic_Df(float t)
        {
            return OutElastic_Df(1f - t);
        }

        public static float OutElastic_Df(float t)
        {
            float d = 1f;
            float p = d * .3f;
            float s;
            float a = 0;

            if (a is 0f or < 1f)
            {
                a = 1f;
                s = p * 0.25f;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(1f / a);
            }

            return (a * Mathf.PI * d * Mathf.Pow(2f, 1f - 10f * t) *
                Mathf.Cos((2f * Mathf.PI * (d * t - s)) / p)) / p - 5f * NaturalLOGOf2 * a *
                Mathf.Pow(2f, 1f - 10f * t) * Mathf.Sin((2f * Mathf.PI * (d * t - s)) / p);
        }

        public static float InOutElastic_Df(float t)
        {
            float d = 1f;
            float p = d * .3f;
            float s;
            float a = 0;

            if (a is 0f or < 1f)
            {
                a = 1f;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(1f / a);
            }

            if (t < 1)
            {
                t -= 1;

                return -5f * NaturalLOGOf2 * a * Mathf.Pow(2f, 10f * t) * Mathf.Sin(2 * Mathf.PI * (d * t - 2f) / p) -
                    a * Mathf.PI * d * Mathf.Pow(2f, 10f * t) * Mathf.Cos(2 * Mathf.PI * (d * t - s) / p) / p;
            }

            t -= 1;

            return a * Mathf.PI * d * Mathf.Cos(2f * Mathf.PI * (d * t - s) / p) / (p * Mathf.Pow(2f, 10f * t)) -
                5f * NaturalLOGOf2 * a * Mathf.Sin(2f * Mathf.PI * (d * t - s) / p) / (Mathf.Pow(2f, 10f * t));
        }

        public static float InBounce_Df(float t)
        {
            const float d = 1f;
            return OutBounce_Df(d - t);
        }

        public static float OutBounce_Df(float t)
        {
            t /= 1f;
            if (t < (1 / 2.75f))
            {
                return 2f * 7.5625f * t;
            }
            else if (t < (2 / 2.75f))
            {
                t -= (1.5f / 2.75f);
                return 2f * 7.5625f * t;
            }
            else if (t < (2.5 / 2.75))
            {
                t -= (2.25f / 2.75f);
                return 2f * 7.5625f * t;
            }
            else
            {
                t -= (2.625f / 2.75f);
                return 2f * 7.5625f * t;
            }
        }

        public static float InOutBounce_Df(float t)
        {
            const float d = 1f;
            return t < d * 0.5f ? InBounce_Df(t * 2) * 0.5f : OutBounce_Df(t * 2 - d) * 0.5f;
        }
        #endregion
        #endregion
    }
}
