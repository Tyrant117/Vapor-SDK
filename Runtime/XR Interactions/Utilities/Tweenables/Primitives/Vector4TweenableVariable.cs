﻿using System;
using Unity.Jobs;
using Unity.Mathematics;

namespace VaporXR.Utilities.Tweenables
{
    /// <summary>
    /// Bindable variable that can tween over time towards a target float4 (Vector4) value.
    /// Uses an async implementation to tween using the job system.
    /// </summary>
    public class Vector4TweenableVariable : TweenableVariableAsyncBase<float4>
    {
        /// <inheritdoc />
        protected override JobHandle ScheduleTweenJob(ref TweenJobData<float4> jobData)
        {
            var job = new Float4TweenJob { jobData = jobData };
            return job.Schedule();
        }
    }
}