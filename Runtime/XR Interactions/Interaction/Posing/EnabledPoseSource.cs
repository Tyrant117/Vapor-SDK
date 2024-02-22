namespace VaporXR
{
    public class EnabledPoseSource : PoseSource
    {
        private void OnEnable()
        {
            EnablePose();
        }

        private void OnDisable()
        {
            DisablePose();
        }
    }
}
