using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace PickupCent.Digging
{
    /// <summary>
    /// RenderTexture 기반 모래 마스크. 픽셀(0~1, 표시상 0~255)이 "덮여있는 정도"를 나타낸다.
    /// 255(1.0)=완전히 덮임, 0=완전히 뚫림. 강도/경도 값으로 1회 침식량을 계산한다.
    /// Update()에서 되메워짐(decay) 패스를 먼저 처리하므로, 같은 프레임에 Erode()를 호출하는
    /// 스크립트(SandDigInput)보다 반드시 먼저 실행돼야 한다 -> DefaultExecutionOrder로 순서 고정.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class SandMaskController : MonoBehaviour
    {
        [Header("필드 크기 / 해상도")]
        [SerializeField] private Vector2 fieldSize = new Vector2(8f, 8f);
        [SerializeField] private int textureResolution = 512;

        [Header("파기 강도 / 지반 경도 (기획서 2장: 강도 ÷ 경도 = 1회 침식 비율)")]
        [Tooltip("도구 강도 (임시값, 나중에 도구별로 대체됨)")]
        [SerializeField] private float strength = 1f;
        [Tooltip("스테이지 지반 경도 (임시값, 나중에 스테이지별로 대체됨)")]
        [SerializeField] private float hardness = 2f;

        [Header("브러시")]
        [SerializeField] private float brushRadius = 0.6f;
        [Tooltip("높을수록 브러시 중심에만 침식이 집중되고 가장자리는 덜 깎임")]
        [SerializeField] private float brushFalloffPower = 2f;

        [Header("되메워짐 (안 건드리면 서서히 회복)")]
        [Tooltip("초당 회복량 (0~255 기준)")]
        [SerializeField] private float regenPerSecond = 6f;

        [Header("표시 (2단계 표현: 살짝 건드림=색만 어두워짐 / 확실히 뚫림=투명해짐)")]
        [SerializeField] private Color sandColor = new Color(0.76f, 0.65f, 0.42f);
        [SerializeField] private Color erodedColor = new Color(0.35f, 0.28f, 0.16f);
        [Tooltip("이 값(0~255) 이하로 떨어지면 '뚫린' 것으로 간주 (기획서 기준: 10)")]
        [SerializeField] private float holeThresholdByte = 10f;
        [SerializeField, Range(0.001f, 0.2f)] private float holeSoftEdge = 0.02f;

        [Header("CPU 판정용 리드백")]
        [SerializeField] private float readbackInterval = 0.08f;
        [Tooltip("D3D 등에서 AsyncGPUReadback 결과가 상하로 뒤집혀 나오는 경우 보정용. " +
                 "체크포인트 로그가 시각적 침식과 안 맞으면 꺼보세요.")]
        [SerializeField] private bool readbackFlipY = true;

        [Header("디버그")]
        [SerializeField] private bool logFormulaOnChange = true;

        private RenderTexture rtA, rtB;
        private RenderTexture current, other;
        private Material decayMat, brushMat, displayMat;
        private MeshRenderer mr;

        private float readbackTimer;
        private bool readbackPending;
        private byte[] cpuCache;
        private int cpuCacheRes;
        private int cpuCacheBytesPerPixel = 1;
        private bool loggedReadbackDiagnostics;

        // 마스크는 색이 아니라 데이터(침식량)이므로 항상 sRGB 없는 8비트 단일 채널로 만든다.
        // RenderTextureFormat.R8 + 프로젝트의 sRGB 색공간 설정을 그대로 두면 Unity가
        // 내부적으로 R8_SRGB를 요청했다가(미지원) RGBA32로 조용히 폴백하는 경우가 있어(콘솔 경고 참고),
        // GraphicsFormat을 직접 지정해서 그 폴백 경로 자체를 피한다.
        private static readonly GraphicsFormat MaskGraphicsFormat = GraphicsFormat.R8_UNorm;

        private float lastStrength, lastHardness;

        public float HoleThresholdNormalized => holeThresholdByte / 255f;
        public Vector2 FieldSize => fieldSize;
        public Texture CurrentMask => current;

        /// <summary>현재 장착된 도구의 강도. ToolManager가 도구 전환 시 이 값을 갈아끼운다.</summary>
        public float Strength
        {
            get => strength;
            set => strength = value;
        }

        public float BrushRadius => brushRadius;

        /// <summary>파기 범위 강화. UpgradeManager가 호출 — 브러시 반경을 확장한다.</summary>
        public void AddBrushRadius(float amount)
        {
            brushRadius += amount;
            Debug.Log($"[SandMask] 브러시 반경 +{amount} → 현재 {brushRadius}");
        }

        private void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            SetupTextures();
            SetupMaterials();
            lastStrength = strength;
            lastHardness = hardness;
            LogFormula();
        }

        private void SetupTextures()
        {
            rtA = CreateMaskRT();
            rtB = CreateMaskRT();

            var initTex = new Texture2D(1, 1, TextureFormat.R8, false);
            initTex.SetPixel(0, 0, Color.white);
            initTex.Apply();
            Graphics.Blit(initTex, rtA);
            Graphics.Blit(initTex, rtB);
            Destroy(initTex);

            current = rtA;
            other = rtB;
        }

        private RenderTexture CreateMaskRT()
        {
            if (!SystemInfo.IsFormatSupported(MaskGraphicsFormat, FormatUsage.Render))
            {
                Debug.LogWarning($"[SandMask] {MaskGraphicsFormat}이 이 플랫폼에서 Render 용도로 지원되지 않습니다. " +
                                  "Unity가 다른 포맷으로 대체할 수 있으니 아래 실제 생성된 graphicsFormat 로그를 확인하세요.");
            }

            var desc = new RenderTextureDescriptor(textureResolution, textureResolution, MaskGraphicsFormat, 0)
            {
                sRGB = false,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false
            };

            var rt = new RenderTexture(desc)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();

            if (rt.graphicsFormat != MaskGraphicsFormat)
            {
                Debug.LogWarning($"[SandMask] 요청한 포맷({MaskGraphicsFormat})과 실제 생성된 포맷" +
                                  $"({rt.graphicsFormat})이 다릅니다. 체크포인트 판정용 리드백 코드는 " +
                                  "픽셀당 바이트 수를 자동으로 계산하므로 동작은 하지만, 값이 sRGB 보정을 " +
                                  "받을 수 있으니 가능하면 지원되는 GPU/플랫폼에서 확인하세요.");
            }

            return rt;
        }

        private void SetupMaterials()
        {
            decayMat = new Material(Shader.Find("PickupCent/SandDecay"));
            brushMat = new Material(Shader.Find("PickupCent/SandBrush"));
            displayMat = new Material(Shader.Find("PickupCent/SandDisplay"));
            mr.material = displayMat;
            UpdateDisplayMaterial();
        }

        private void Update()
        {
            // 1) 되메워짐 패스 (항상 적용)
            float dt = Time.deltaTime;
            decayMat.SetFloat("_RegenRate", regenPerSecond / 255f);
            decayMat.SetFloat("_DeltaTime", dt);

            var next = (current == rtA) ? rtB : rtA;
            Graphics.Blit(current, next, decayMat);
            current = next;
            other = (current == rtA) ? rtB : rtA;

            UpdateDisplayMaterial();
            CheckFormulaChanged();

            // 2) 주기적 CPU 리드백 요청 (체크포인트 판정용)
            readbackTimer += dt;
            if (readbackTimer >= readbackInterval)
            {
                readbackTimer = 0f;
                RequestReadback();
            }
        }

        private void UpdateDisplayMaterial()
        {
            displayMat.SetTexture("_MaskTex", current);
            displayMat.SetColor("_SandColor", sandColor);
            displayMat.SetColor("_ErodedColor", erodedColor);
            displayMat.SetFloat("_HoleThreshold", HoleThresholdNormalized);
            displayMat.SetFloat("_SoftEdge", holeSoftEdge);
        }

        private void CheckFormulaChanged()
        {
            if (!logFormulaOnChange) return;
            if (Mathf.Approximately(strength, lastStrength) && Mathf.Approximately(hardness, lastHardness)) return;
            lastStrength = strength;
            lastHardness = hardness;
            LogFormula();
        }

        private void LogFormula()
        {
            float ratio = ErosionRatio();
            Debug.Log($"[SandMask] 강도={strength} / 경도={hardness} => 1회 침식량={ratio * 255f:F1} " +
                      $"(0~255 기준, {ratio:P0}) / 되메워짐={regenPerSecond}/s");
        }

        private float ErosionRatio()
        {
            return Mathf.Clamp01(hardness > 0f ? strength / hardness : 1f);
        }

        /// <summary>월드 좌표 worldCenter에 "한 번 쓸기"(스트로크)를 적용한다.</summary>
        public void Erode(Vector2 worldCenter)
        {
            Vector2 uv = WorldToUV(worldCenter);
            float ratio = ErosionRatio();

            brushMat.SetVector("_BrushCenter", new Vector4(uv.x, uv.y, 0f, 0f));
            brushMat.SetFloat("_BrushRadius", brushRadius / fieldSize.x);
            brushMat.SetFloat("_FalloffPower", brushFalloffPower);
            brushMat.SetFloat("_ErosionAmount", ratio);

            Graphics.Blit(current, other, brushMat);
            var tmp = current;
            current = other;
            other = tmp;

            UpdateDisplayMaterial();
        }

        private Vector2 WorldToUV(Vector2 worldPos)
        {
            Vector2 local = worldPos - (Vector2)transform.position;
            float u = local.x / fieldSize.x + 0.5f;
            float v = local.y / fieldSize.y + 0.5f;
            return new Vector2(u, v);
        }

        private void RequestReadback()
        {
            if (readbackPending || current == null) return;
            readbackPending = true;
            AsyncGPUReadback.Request(current, 0, TextureFormat.R8, OnReadbackComplete);
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest req)
        {
            readbackPending = false;
            if (req.hasError)
            {
                Debug.LogWarning("[SandMask] AsyncGPUReadback 실패 (req.hasError=true), 이번 프레임 리드백을 건너뜁니다.");
                return;
            }

            NativeArray<byte> data = req.GetData<byte>();

            int expectedPixels = textureResolution * textureResolution;
            int bytesPerPixel = expectedPixels > 0 ? Mathf.Max(1, data.Length / expectedPixels) : 1;

            if (!loggedReadbackDiagnostics)
            {
                loggedReadbackDiagnostics = true;
                Debug.Log($"[SandMask] 리드백 진단 — data.Length={data.Length}, " +
                          $"1바이트/px 가정 시 예상 길이={expectedPixels}, 실측 bytesPerPixel={bytesPerPixel}, " +
                          $"RT graphicsFormat={current.graphicsFormat}");
            }

            if (cpuCache == null || cpuCache.Length != data.Length)
                cpuCache = new byte[data.Length];
            data.CopyTo(cpuCache);
            cpuCacheRes = textureResolution;
            cpuCacheBytesPerPixel = bytesPerPixel;
        }

        /// <summary>월드 좌표의 마스크 값을 0~255 범위로 반환한다. (아직 리드백이 없으면 255=완전히 덮임으로 간주)</summary>
        public float SampleAlpha255(Vector2 worldPos)
        {
            if (cpuCache == null || cpuCacheRes <= 0) return 255f;

            Vector2 uv = WorldToUV(worldPos);
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return 255f;

            int x = Mathf.Clamp(Mathf.RoundToInt(uv.x * (cpuCacheRes - 1)), 0, cpuCacheRes - 1);
            float vy = readbackFlipY ? 1f - uv.y : uv.y;
            int y = Mathf.Clamp(Mathf.RoundToInt(vy * (cpuCacheRes - 1)), 0, cpuCacheRes - 1);

            // 픽셀당 바이트 수는 실측값(cpuCacheBytesPerPixel)을 쓴다 — RT가 R8이 아니라
            // RGBA류로 폴백된 경우에도 각 픽셀의 첫 바이트(R 채널, 셰이더가 값을 저장하는 채널)를 읽는다.
            int index = (y * cpuCacheRes + x) * cpuCacheBytesPerPixel;
            if (index < 0 || index >= cpuCache.Length) return 255f;
            return cpuCache[index];
        }

        private void OnDestroy()
        {
            if (rtA != null) rtA.Release();
            if (rtB != null) rtB.Release();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(fieldSize.x, fieldSize.y, 0.01f));
        }
    }
}
