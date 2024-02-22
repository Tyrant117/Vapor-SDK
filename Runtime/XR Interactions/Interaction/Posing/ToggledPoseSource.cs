namespace VaporXR
{
    public class ToggledPoseSource : PoseSource
    {
        private bool _isOn;
        public bool IsOn => _isOn;

        public void SetToggle(bool isOn)
        {
            if (_isOn != isOn)
            {
                if (isOn)
                {
                    EnablePose();
                }
                else
                {
                    DisablePose();
                }
                _isOn = isOn;
            }
        }
    }
}
