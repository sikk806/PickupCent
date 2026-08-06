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
        [SerializeField] private float brushRadius = 0.5f;
        [Tooltip("높을수록 브러시 중심에만 침식이 집중되고 가장자리는 덜 깎임")]
        [SerializeField] private float brushFalloffPower = 2f;

        [Header("되메워짐 (안 건드리면 서서히 회복)")]
        [Tooltip("초당 회복량 (0~255 기준)")]
        [SerializeField] private float regenPerSecond = 30f;

        [Header("표시 (2단계 표현: 살짝 건드림=색만 어두워짐 / 확실히 뚫림=투명해짐)")]
        [SerializeField] private Color sandColor = new Color(0.76f, 0.65f, 0.42f);
        [SerializeField] private Color erodedColor = new Color(0.35f, 0.28f, 0.16f);
        [Tooltip("모래판 밖, 즉 팔 수 없는 영역을 채우는 색. 상점 UI의 어두운 갈색 계열과 맞춘다.")]
        [SerializeField] private Color nonDiggableColor = new Color(0.14f, 0.10f, 0.07f);
        [Tooltip("필드보다 몇 배 넓게 조작 불가 배경판을 깔지 정한다.")]
        [SerializeField] private float nonDiggableBackdropScale = 3f;
        [Tooltip("이 값(0~255) 이하로 떨어지면 '뚫린' 것으로 간주 (기획서 기준: 10)")]
        [SerializeField] private float holeThresholdByte = 10f;
        [SerializeField, Range(0.001f, 0.2f)] private float holeSoftEdge = 0.02f;

        [Header("지형 텍스처 (셋 다 있으면 텍스처 블렌딩, 하나라도 비어있으면 위 sandColor/erodedColor 단색으로 폴백)")]
        [Tooltip("마른 표면 텍스처")]
        [SerializeField] private Texture2D sandTexture;
        [Tooltip("젖은 표면 텍스처 (살짝 건드린 곳에 마른 표면과 섞여 보임)")]
        [SerializeField] private Texture2D wetTexture;
        [Tooltip("파낸 바닥 텍스처 (확실히 뚫린 곳에 드러남)")]
        [SerializeField] private Texture2D dugFloorTexture;
        [Tooltip("지형 텍스처 반복(타일링) 배율. 필드 전체에 이 배율만큼 텍스처가 반복된다")]
        [SerializeField] private float textureTiling = 4f;

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

        // 완전히 뚫린 자리에서 카메라 배경 대신 항상 보이는 불투명 배경 레이어 (아이템보다도 뒤에 위치).
        private const float DugFloorZOffset = 1f;
        private MeshRenderer dugFloorRenderer;
        private Material dugFloorMat;
        private Texture2D fallbackDugFloorTex;
        private MeshRenderer nonDiggableRenderer;
        private Material nonDiggableMat;

        private float readbackTimer;
        private bool readbackPending;
        private byte[] cpuCache;
        private int cpuCacheRes;
        private int cpuCacheBytesPerPixel = 1;
        private bool loggedReadbackDiagnostics;

        // 마스크는 색이 아니라 데이터(침식량)이므로 sRGB 없는 단일 채널 포맷으로 만든다.
        // 되메워짐은 프레임당 1/255보다 작은 값도 누적되어야 해서 R16을 우선 사용하고,
        // 지원되지 않는 플랫폼에서만 R8로 폴백한다.
        private static readonly GraphicsFormat PreferredMaskGraphicsFormat = GraphicsFormat.R16_UNorm;
        private static readonly GraphicsFormat FallbackMaskGraphicsFormat = GraphicsFormat.R8_UNorm;
        private static GraphicsFormat MaskGraphicsFormat =>
            SystemInfo.IsFormatSupported(PreferredMaskGraphicsFormat, GraphicsFormatUsage.Render)
                ? PreferredMaskGraphicsFormat
                : FallbackMaskGraphicsFormat;

        private float lastStrength, lastHardness;

        public float HoleThresholdNormalized => holeThresholdByte / 255f;
        public Vector2 FieldSize => fieldSize;
        public Texture CurrentMask => current;

        public Texture2D SandTexture => sandTexture;
        public Texture2D WetTexture => wetTexture;
        public Texture2D DugFloorTexture => dugFloorTexture;

        /// <summary>셋 다 연결돼 있어야 텍스처 블렌딩 모드로 표시한다 — 하나라도 비어있으면 단색 폴백.</summary>
        private bool UseTextures => sandTexture != null && wetTexture != null && dugFloorTexture != null;

        /// <summary>현재 장착된 도구의 강도. ToolManager가 도구 전환 시 이 값을 갈아끼운다.</summary>
        public float Strength
        {
            get => strength;
            set => strength = value;
        }

        /// <summary>지반 경도. 디버그 패널 등에서 실시간 조절용.</summary>
        public float Hardness
        {
            get => hardness;
            set => hardness = value;
        }

        public float BrushRadius
        {
            get => brushRadius;
            set => brushRadius = value;
        }

        /// <summary>초당 되메워짐 속도(0~255 기준). 디버그 패널 등에서 실시간 조절용.</summary>
        public float RegenPerSecond
        {
            get => regenPerSecond;
            set => regenPerSecond = value;
        }

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
            EnsureTerrainTextureWrapModes();
            SetupNonDiggableBackdrop();
            SetupDugFloorBackground();
            LogDugFloorTextureInfo();
            lastStrength = strength;
            lastHardness = hardness;
            LogFormula();
        }

        /// <summary>지형 텍스처는 타일링돼야 하므로, 임포트 설정과 무관하게 Wrap Mode를 Repeat로 강제한다.</summary>
        private void EnsureTerrainTextureWrapModes()
        {
            if (sandTexture != null) sandTexture.wrapMode = TextureWrapMode.Repeat;
            if (wetTexture != null) wetTexture.wrapMode = TextureWrapMode.Repeat;
            if (dugFloorTexture != null) dugFloorTexture.wrapMode = TextureWrapMode.Repeat;
        }

        private void SetupNonDiggableBackdrop()
        {
            var bgGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGO.name = "NonDiggableBackdrop";
            var col = bgGO.GetComponent<Collider>();
            if (col != null) Destroy(col);

            bgGO.transform.SetParent(transform, false);
            bgGO.transform.localPosition = new Vector3(0f, 0f, DugFloorZOffset + 0.25f);
            bgGO.transform.localScale = Vector3.one * Mathf.Max(1f, nonDiggableBackdropScale);

            nonDiggableRenderer = bgGO.GetComponent<MeshRenderer>();
            nonDiggableMat = new Material(Shader.Find("Unlit/Color"));
            nonDiggableMat.color = nonDiggableColor;
            nonDiggableRenderer.material = nonDiggableMat;
        }

        /// <summary>
        /// 완전히 뚫린 자리(모래 알파=0)에서 카메라 배경색이 그대로 비쳐 보이던 문제 수정용 —
        /// 모래 레이어보다 뒤(아이템보다도 뒤)에 항상 깔려있는 불투명 배경 레이어를 만든다.
        /// 아이템이 있으면 아이템이 이 배경 위에 보이고, 없으면 이 배경(파낸 바닥 텍스처)이 그대로 보인다.
        /// </summary>
        private void SetupDugFloorBackground()
        {
            var bgGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGO.name = "DugFloorBackground";
            var col = bgGO.GetComponent<Collider>();
            if (col != null) Destroy(col);

            bgGO.transform.SetParent(transform, false);
            // 부모(SandLayer)의 localScale이 이미 fieldSize이므로, 이 자식은 scale=1이면 그대로 필드 전체를 덮는다.
            bgGO.transform.localPosition = new Vector3(0f, 0f, DugFloorZOffset);
            bgGO.transform.localScale = Vector3.one;

            dugFloorRenderer = bgGO.GetComponent<MeshRenderer>();
            dugFloorMat = new Material(Shader.Find("Unlit/Texture"));
            dugFloorRenderer.material = dugFloorMat;

            RefreshDugFloorBackground();
        }

        private void RefreshDugFloorBackground()
        {
            if (dugFloorMat == null) return;
            if (nonDiggableMat != null) nonDiggableMat.color = nonDiggableColor;

            if (dugFloorTexture != null)
            {
                dugFloorMat.mainTexture = dugFloorTexture;
                dugFloorMat.mainTextureScale = new Vector2(textureTiling, textureTiling);
            }
            else
            {
                if (fallbackDugFloorTex == null) fallbackDugFloorTex = new Texture2D(1, 1);
                fallbackDugFloorTex.SetPixel(0, 0, erodedColor);
                fallbackDugFloorTex.Apply();
                dugFloorMat.mainTexture = fallbackDugFloorTex;
                dugFloorMat.mainTextureScale = Vector2.one;
            }
        }

        private void LogDugFloorTextureInfo()
        {
#if UNITY_EDITOR
            string path = dugFloorTexture != null ? UnityEditor.AssetDatabase.GetAssetPath(dugFloorTexture) : "(연결 안 됨)";
            Debug.Log($"[SandMask] Dug Floor Texture 연결 확인 — 이름: {(dugFloorTexture != null ? dugFloorTexture.name : "null")}, 경로: {path}");
#else
            Debug.Log($"[SandMask] Dug Floor Texture 연결 확인 — 이름: {(dugFloorTexture != null ? dugFloorTexture.name : "null")}");
#endif
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
            var graphicsFormat = MaskGraphicsFormat;
            if (!SystemInfo.IsFormatSupported(graphicsFormat, GraphicsFormatUsage.Render))
            {
                Debug.LogWarning($"[SandMask] {graphicsFormat}이 이 플랫폼에서 Render 용도로 지원되지 않습니다. " +
                                  "Unity가 다른 포맷으로 대체할 수 있으니 아래 실제 생성된 graphicsFormat 로그를 확인하세요.");
            }

            var desc = new RenderTextureDescriptor(textureResolution, textureResolution, graphicsFormat, 0)
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

            if (rt.graphicsFormat != graphicsFormat)
            {
                Debug.LogWarning($"[SandMask] 요청한 포맷({graphicsFormat})과 실제 생성된 포맷" +
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
            RefreshDugFloorBackground();
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

            bool useTextures = UseTextures;
            displayMat.SetFloat("_UseTextures", useTextures ? 1f : 0f);
            if (useTextures)
            {
                displayMat.SetTexture("_SandTex", sandTexture);
                displayMat.SetTexture("_WetTex", wetTexture);
                displayMat.SetTexture("_DugFloorTex", dugFloorTexture);
                displayMat.SetFloat("_TextureTiling", textureTiling);
            }
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

        /// <summary>월드 좌표 worldCenter에 "한 번 쓸기"(스트로크)를 적용하고, 파낸 양을 반환한다.</summary>
        public float Erode(Vector2 worldCenter)
        {
            if (!IsWorldInsideField(worldCenter)) return 0f;

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
            return ratio * brushRadius * brushRadius;
        }

        private Vector2 WorldToUV(Vector2 worldPos)
        {
            Vector2 local = worldPos - (Vector2)transform.position;
            float u = local.x / fieldSize.x + 0.5f;
            float v = local.y / fieldSize.y + 0.5f;
            return new Vector2(u, v);
        }

        public bool IsWorldInsideField(Vector2 worldPos)
        {
            Vector2 uv = WorldToUV(worldPos);
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
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
            if (fallbackDugFloorTex != null) Destroy(fallbackDugFloorTex);
            if (nonDiggableMat != null) Destroy(nonDiggableMat);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(fieldSize.x, fieldSize.y, 0.01f));
        }
    }
}

