using TypeD.Models.Data.SettingContexts;
using TypeD.Models.Interfaces;
using TypeD.View.Viewer;

namespace TypeD.Models
{
    internal sealed class ViewerCameraModel : IViewerCameraModel
    {
        private ISettingModel SettingModel { get; set; }
        private double _speed = ViewerCamera.DefaultSpeed;

        public ViewerCamera Camera { get; private set; }

        public double Speed
        {
            get => _speed;
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return;

                value = Math.Clamp(value, ViewerCamera.MinSpeed, ViewerCamera.MaxSpeed);
                if (value == _speed)
                    return;

                _speed = value;
                if (Camera != null)
                    Camera.Speed = value;

                var setting = SettingModel?.GetContext<MainWindowSettingContext>();
                if (setting != null)
                {
                    setting.CameraSpeed.Value = value;
                    SettingModel.SetContext(setting);
                }
                OnChanged();
            }
        }

        public event Action Changed;

        public void Init(IResourceModel resourceModel)
        {
            SettingModel = resourceModel.Get<ISettingModel>();
            var speed = SettingModel?.GetContext<MainWindowSettingContext>()?.CameraSpeed.Value ?? ViewerCamera.DefaultSpeed;
            _speed = double.IsNaN(speed) || speed <= 0 ? ViewerCamera.DefaultSpeed : Math.Clamp(speed, ViewerCamera.MinSpeed, ViewerCamera.MaxSpeed);
        }

        public void Attach(ViewerCamera camera)
        {
            if (ReferenceEquals(Camera, camera))
                return;

            if (Camera != null)
                Camera.Changed -= OnChanged;

            Camera = camera;
            if (Camera != null)
            {
                Camera.Speed = _speed;
                Camera.Changed += OnChanged;
            }
            OnChanged();
        }

        public void Detach(ViewerCamera camera)
        {
            if (camera == null || !ReferenceEquals(Camera, camera))
                return;

            Camera.Changed -= OnChanged;
            Camera = null;
            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke();
        }
    }
}
