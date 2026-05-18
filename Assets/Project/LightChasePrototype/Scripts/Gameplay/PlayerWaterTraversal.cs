using UnityEngine;

namespace LightChasePrototype
{
    public class PlayerWaterTraversal : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;

        public bool IsInWater => _activeWaterVolume != null;

        private MonoBehaviour _controller;
        private System.Type _controllerType;
        private WaterVolume _activeWaterVolume;
        private Vector3 _defaultVisualLocalPosition;
        private float _defaultMoveSpeed;
        private float _defaultSprintSpeed;
        private float _defaultJumpHeight;
        private bool _defaultsCached;
        private bool _visualBaselineCaptured;

        private void Awake()
        {
            CacheDefaults();
            ResolveVisualRoot();
            ApplyCurrentWaterState();
        }

        private void OnDisable()
        {
            _activeWaterVolume = null;
            ApplyCurrentWaterState();
        }

        public void EnterWater(WaterVolume waterVolume)
        {
            if (waterVolume == null)
            {
                return;
            }

            CacheDefaults();
            ResolveVisualRoot();
            _activeWaterVolume = waterVolume;
            ApplyCurrentWaterState();
        }

        public void ExitWater(WaterVolume waterVolume)
        {
            if (waterVolume == null || _activeWaterVolume != waterVolume)
            {
                return;
            }

            _activeWaterVolume = null;
            ApplyCurrentWaterState();
        }

        private void CacheDefaults()
        {
            if (_defaultsCached)
            {
                return;
            }

            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour.GetType().FullName != "StarterAssets.ThirdPersonController")
                {
                    continue;
                }

                _controller = behaviour;
                _controllerType = behaviour.GetType();
                break;
            }

            if (_controller != null)
            {
                _defaultMoveSpeed = GetControllerFloat("MoveSpeed");
                _defaultSprintSpeed = GetControllerFloat("SprintSpeed");
                _defaultJumpHeight = GetControllerFloat("JumpHeight");
            }

            _defaultsCached = true;
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot == null)
            {
                visualRoot = FindVisualRoot();
            }

            if (visualRoot == null || _visualBaselineCaptured)
            {
                return;
            }

            _defaultVisualLocalPosition = visualRoot.localPosition;
            _visualBaselineCaptured = true;
        }

        private Transform FindVisualRoot()
        {
            foreach (var animator in GetComponentsInChildren<Animator>(true))
            {
                if (animator == null || animator.transform == transform)
                {
                    continue;
                }

                return animator.transform;
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.transform == transform)
                {
                    continue;
                }

                var objectName = renderer.transform.name;
                if (objectName.Contains("Glow") || objectName.Contains("Camera"))
                {
                    continue;
                }

                return renderer.transform;
            }

            return null;
        }

        private void ApplyCurrentWaterState()
        {
            if (_controller != null)
            {
                if (_activeWaterVolume != null)
                {
                    SetControllerFloat("MoveSpeed", _defaultMoveSpeed * _activeWaterVolume.MoveSpeedMultiplier);
                    SetControllerFloat("SprintSpeed", _defaultSprintSpeed * _activeWaterVolume.SprintSpeedMultiplier);
                    SetControllerFloat("JumpHeight", _defaultJumpHeight * _activeWaterVolume.JumpHeightMultiplier);
                }
                else
                {
                    SetControllerFloat("MoveSpeed", _defaultMoveSpeed);
                    SetControllerFloat("SprintSpeed", _defaultSprintSpeed);
                    SetControllerFloat("JumpHeight", _defaultJumpHeight);
                }
            }

            if (visualRoot == null || !_visualBaselineCaptured)
            {
                return;
            }

            var targetLocalPosition = _defaultVisualLocalPosition;
            if (_activeWaterVolume != null)
            {
                targetLocalPosition.y -= _activeWaterVolume.VisualSinkDepth;
            }

            visualRoot.localPosition = targetLocalPosition;
        }

        private float GetControllerFloat(string memberName)
        {
            if (_controllerType == null || _controller == null)
            {
                return 0f;
            }

            var field = _controllerType.GetField(memberName);
            if (field != null && field.FieldType == typeof(float))
            {
                return (float)field.GetValue(_controller);
            }

            var property = _controllerType.GetProperty(memberName);
            if (property != null && property.PropertyType == typeof(float))
            {
                return (float)property.GetValue(_controller);
            }

            return 0f;
        }

        private void SetControllerFloat(string memberName, float value)
        {
            if (_controllerType == null || _controller == null)
            {
                return;
            }

            var field = _controllerType.GetField(memberName);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(_controller, value);
                return;
            }

            var property = _controllerType.GetProperty(memberName);
            if (property != null && property.CanWrite && property.PropertyType == typeof(float))
            {
                property.SetValue(_controller, value);
            }
        }
    }
}
