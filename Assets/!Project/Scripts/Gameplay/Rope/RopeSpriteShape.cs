using Playeble.Scripts.Gameplay;
using UnityEngine;
using UnityEngine.U2D;

namespace AzurGames.Wool.Gameplay.Rope
{
    public sealed class RopeSpriteShape : MonoBehaviour
    {
        [SerializeField] private LineRenderer _line;
        [SerializeField] private Playeble.Scripts.Gameplay.RopeColorOffsetConfig _colorOffsetConfig;

        [Header("Wave Settings")]
        [Tooltip("Амплитуда волнения (в UI-пикселях)")] [SerializeField]
        private float _waveAmplitude = 20f;

        [Tooltip("Частота волны (кол-во колебаний в секунду)")] [SerializeField]
        private float _waveFrequency = 2f;

        [Tooltip("Насколько быстрый дополнительный шум")] [SerializeField]
        private float _noiseSpeed = 1.5f;

        [Tooltip("Насколько сильно шум влияет на амплитуду (0–1)")] [SerializeField, Range(0f, 1f)]
        private float _noiseAmount = 0.3f;

        [Tooltip("Насколько волна к центру сильнее, чем к концам")] [SerializeField, Range(0f, 1f)]
        private float _centerBoost = 0.7f;

        [Tooltip("Новый сегмент на каждое значение длины,примерный расчет значения когда дракон рядом с блоками примерно 2-3 еденицы, от края до края примерно 16-20")]
        [SerializeField]
        private float _pixelsPerSegment = 80f;

        [SerializeField] private int _minPoints = 3;
        [SerializeField] private int _maxPoints = 9;

        [Header("Wave By Length")]
        [Tooltip("Длина, при которой волна почти исчезает примерный расчет значения, когда дракон рядом с блоками примерно 2-3 еденицы, от края до края примерно 16-20")]
        [SerializeField]
        private float _minLengthForWave = 50f;

        [Tooltip("Длина, при которой амплитуда достигает максимума")] [SerializeField]
        private float _maxLengthForWave = 400f;

        [Tooltip("Минимальный коэффициент амплитуды при очень короткой веревке")] [SerializeField, Range(0f, 1f)]
        private float _minAmplitudeFactor = 0.1f;

        [Tooltip("Максимальный коэффициент амплитуды при длинной веревке")] [SerializeField, Range(0f, 1f)]
        private float _maxAmplitudeFactor = 1.0f;

        [SerializeField] private float _tangentScale = 0.25f;

        [Header("Line Renderer Smoothing")]
        [Min(0)]
        [SerializeField] private int _smoothSubdivisions = 4;
        [Min(0)]
        [SerializeField] private int _cornerVertices = 4;
        [Min(0)]
        [SerializeField] private int _capVertices = 4;

        private float[] _randomPhaseOffsets;

        private RectTransform _ropeRect;

        //private SplineTools _spline;
        private Camera _camera;
        private float _prevColorOffset = -9999f;

        public void SetColorOffsetConfig(Playeble.Scripts.Gameplay.RopeColorOffsetConfig config)
        {
            _colorOffsetConfig = config;
        }

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;

            _ropeRect = (RectTransform)transform;

            if (_line == null)
            {
                _line = GetComponent<LineRenderer>();
            }

            ApplyLineSettings();
        }

        private void OnValidate()
        {
            if (_ropeRect == null && transform is RectTransform rect)
            {
                _ropeRect = rect;
            }

            if (_line == null)
            {
                _line = GetComponent<LineRenderer>();
            }

            ApplyLineSettings();
        }

        private void ApplyLineSettings()
        {
            if (_line == null)
            {
                return;
            }

            _line.useWorldSpace = false;
            _line.numCornerVertices = _cornerVertices;
            _line.numCapVertices = _capVertices;
        }

        private void EnsurePhaseArray(int requiredCount)
        {
            if (_randomPhaseOffsets == null || _randomPhaseOffsets.Length < requiredCount)
            {
                float[] newArray = new float[requiredCount];

                if (_randomPhaseOffsets != null)
                {
                    int copyLen = Mathf.Min(_randomPhaseOffsets.Length, requiredCount);
                    for (int i = 0; i < copyLen; i++)
                        newArray[i] = _randomPhaseOffsets[i];
                }

                var j = 0;

                if (_randomPhaseOffsets != null && _randomPhaseOffsets.Length != 0)
                {
                    j = _randomPhaseOffsets.Length;
                }
                
                for (;j < requiredCount; j++)
                    newArray[j] = Random.Range(0f, Mathf.PI * 2f);

                _randomPhaseOffsets = newArray;
            }
        }

        public void Show(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible)
            {
                gameObject.SetActive(isVisible);
            }
        }

        public void SetColor(Colors color)
        {
            var colorIndex = _colorOffsetConfig != null ? _colorOffsetConfig.GetRopeColorOffset(color) : color.GetOffset();
            if (Mathf.Abs(_prevColorOffset - colorIndex) < 0.0001f)
                return;

            _prevColorOffset = colorIndex;

            if (_line == null)
            {
                return;
            }

            for (int i = 0; i < _line.materials.Length; i++)
            {
                var mat = _line.materials[i];
                if (mat != null)
                {
                    mat.SetFloat("_colorOffset", colorIndex);
                    mat.SetFloat("_ColorOffset", colorIndex);
                }
            }
        }

        public void UpdateRope(Vector3 startWorld, Vector3 endWorld)
        {
            if (_camera == null || _line == null || _ropeRect == null)
                return;

            var startScreen = _camera.WorldToScreenPoint(startWorld);
            var endScreen = _camera.WorldToScreenPoint(endWorld);

            if (startScreen.z <= 0f || endScreen.z <= 0f)
            {
                _line.enabled = false;
                return;
            }

            _line.enabled = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _ropeRect, startScreen, _camera, out Vector2 startLocal2D);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _ropeRect, endScreen, _camera, out Vector2 endLocal2D);

            Vector3 startLocal = new Vector3(startLocal2D.x, startLocal2D.y, 0f);
            Vector3 endLocal = new Vector3(endLocal2D.x, endLocal2D.y, 0f);

            Vector3 diff = endLocal - startLocal;
            float length = diff.magnitude;
            if (length < 0.1f)
                return;

            float lengthFactor01 = Mathf.InverseLerp(_minLengthForWave, _maxLengthForWave, length);
            float lengthAmplitudeFactor = Mathf.Lerp(_minAmplitudeFactor, _maxAmplitudeFactor, lengthFactor01);

            int desiredPoints = Mathf.RoundToInt(length / _pixelsPerSegment) + 1;
            desiredPoints = Mathf.Clamp(desiredPoints, _minPoints, _maxPoints);

            EnsurePhaseArray(desiredPoints);

            int lastIndex = desiredPoints - 1;

            var controlPoints = new Vector3[desiredPoints];
            controlPoints[0] = startLocal;
            controlPoints[lastIndex] = endLocal;

            Vector3 dir = diff / length;

            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

            float tTime = UnityEngine.Time.time;

            for (int i = 1; i < lastIndex; i++)
            {
                float t = (float)i / lastIndex;

                Vector3 basePos = Vector3.Lerp(startLocal, endLocal, t);

                float centerFactor = 1f;
                if (_centerBoost > 0f)
                {
                    float distToCenter = Mathf.Abs(t - 0.5f) * 2f;
                    centerFactor = Mathf.Lerp(1f, 0f, _centerBoost * (1f - distToCenter));
                }

                float phase = _randomPhaseOffsets[i];
                float wave = Mathf.Sin(tTime * _waveFrequency + phase);

                float noise = 0f;
                if (_noiseAmount > 0f)
                {
                    float noiseVal = Mathf.PerlinNoise(t * 3f, tTime * _noiseSpeed);
                    noise = (noiseVal - 0.5f) * 2f;
                }

                float amp = _waveAmplitude
                            * lengthAmplitudeFactor
                            * centerFactor
                            * (1f + noise * _noiseAmount);

                Vector3 offset = perp * wave * amp;
                Vector3 finalPos = basePos + offset;
                controlPoints[i] = finalPos;
            }

            var points = BuildSmoothedPoints(controlPoints, _smoothSubdivisions);
            _line.positionCount = points.Length;
            _line.SetPositions(points);
        }

        private static Vector3[] BuildSmoothedPoints(Vector3[] controlPoints, int subdivisions)
        {
            if (controlPoints == null || controlPoints.Length < 2)
            {
                if (controlPoints == null)
                    return new Vector3[0];
                return controlPoints;
            }

            if (subdivisions <= 0 || controlPoints.Length < 4)
            {
                return controlPoints;
            }

            // Catmull-Rom smoothing across the polyline of control points.
            // We keep endpoints by duplicating edge points.
            var segments = controlPoints.Length - 1;
            var result = new Vector3[segments * subdivisions + 1];
            var idx = 0;

            for (var i = 0; i < segments; i++)
            {
                var p0 = controlPoints[Mathf.Max(i - 1, 0)];
                var p1 = controlPoints[i];
                var p2 = controlPoints[i + 1];
                var p3 = controlPoints[Mathf.Min(i + 2, controlPoints.Length - 1)];

                for (var s = 0; s < subdivisions; s++)
                {
                    var t = s / (float)subdivisions;
                    result[idx++] = CatmullRom(p0, p1, p2, p3, t);
                }
            }

            result[idx] = controlPoints[controlPoints.Length - 1];
            return result;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // Standard uniform Catmull-Rom spline.
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
    }
}