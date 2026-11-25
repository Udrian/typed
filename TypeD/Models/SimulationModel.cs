using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal sealed class SimulationModel : ISimulationModel
    {
        // Read by the update thread of the viewer while the UI changes them
        private volatile bool _isPlaying = true;
        private volatile bool _forceDeltaTime;
        private double _forcedDeltaTime = ISimulationModel.DefaultForcedDeltaTime;
        private double _speedMultiplier = 1;

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value)
                    return;
                _isPlaying = value;
                Changed?.Invoke();
            }
        }

        public bool ForceDeltaTime
        {
            get => _forceDeltaTime;
            set
            {
                if (_forceDeltaTime == value)
                    return;
                _forceDeltaTime = value;
                Changed?.Invoke();
            }
        }

        public double ForcedDeltaTime
        {
            get => Volatile.Read(ref _forcedDeltaTime);
            set
            {
                if (!double.IsFinite(value))
                    return;
                value = Math.Max(0, value);
                if (value == ForcedDeltaTime)
                    return;
                Volatile.Write(ref _forcedDeltaTime, value);
                Changed?.Invoke();
            }
        }

        public double SpeedMultiplier
        {
            get => Volatile.Read(ref _speedMultiplier);
            set
            {
                if (!double.IsFinite(value))
                    return;
                value = Math.Max(0, value);
                if (value == SpeedMultiplier)
                    return;
                Volatile.Write(ref _speedMultiplier, value);
                Changed?.Invoke();
            }
        }

        public event Action Changed;
        public event Action ResetRequested;

        public void RequestReset()
        {
            ResetRequested?.Invoke();
        }

        public void Init(IResourceModel resourceModel)
        {
        }

        public double? ResolveDeltaTime(double deltaTime)
        {
            if (!IsPlaying)
                return null;

            return (ForceDeltaTime ? ForcedDeltaTime : deltaTime) * SpeedMultiplier;
        }
    }
}
