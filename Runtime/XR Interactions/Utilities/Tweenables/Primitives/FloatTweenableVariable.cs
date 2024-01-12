using System;
using Unity.Jobs;

namespace VaporXR.Utilities.Tweenables
{
    /// <summary>
    /// Bindable variable that can tween over time towards a target float value.
    /// Uses an async implementation to tween using the job system.
    /// </summary>
    public class FloatTweenableVariable : TweenableVariableAsyncBase<float>
    {
        /// <inheritdoc />
        protected override JobHandle ScheduleTweenJob(ref TweenJobData<float> jobData)
        {
            var job = new FloatTweenJob { jobData = jobData };
            return job.Schedule();
        }
    }
}