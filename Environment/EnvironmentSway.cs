using UnityEngine;

/// <summary>
/// Điều khiển hiệu ứng sway gió cho cây / bụi / cỏ trang trí bằng shader vertex displacement.
///
/// Cơ chế:
///   Shader "Game/SpriteWindSway" dùng UV.y làm height factor:
///   UV.y = 0 (gốc) → displacement ≈ 0 | UV.y = 1 (ngọn) → displacement tối đa.
///   Component này chỉ set MaterialPropertyBlock 1 lần lúc Awake — không có Update().
///
/// Yêu cầu setup:
///   1. Tạo Material dùng shader "Game/SpriteWindSway".
///   2. Assign material đó vào SpriteRenderer.Material của object cây.
///   3. Gắn component này lên cùng GameObject.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnvironmentSway : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Sway Strength")]
    [Tooltip("Biên độ rung tối đa (world units). Tree ~0.08, Bush ~0.12, Grass ~0.15.")]
    [SerializeField] private float swayStrength = 0.08f;

    [Tooltip("Tốc độ rung (cycles/giây).")]
    [SerializeField] private float swaySpeed = 1.2f;

    [Header("Root / Canopy Control")]
    [Tooltip("Hệ số mũ power curve. Cao hơn → gốc cứng hơn, chỉ ngọn rung. Khuyến nghị 2–4.")]
    [Range(1f, 6f)]
    [SerializeField] private float rootStiffness = 2.5f;

    [Tooltip("Nhân tổng ảnh hưởng phần ngọn. < 1 giảm biên độ, > 1 phóng đại.")]
    [Range(0f, 2f)]
    [SerializeField] private float topInfluence = 1f;

    [Header("Phase & Randomize")]
    [Tooltip("Random phase + speed mỗi lần Awake để nhiều cây không rung đồng bộ.")]
    [SerializeField] private bool randomizeOnAwake = true;

    [Tooltip("Phase offset thủ công (radian). Dùng khi randomizeOnAwake = false.")]
    [Range(0f, 6.2832f)]
    [SerializeField] private float phaseOffset = 0f;

    [Tooltip("Random ±% trên swaySpeed giữa các instance.")]
    [Range(0f, 0.4f)]
    [SerializeField] private float speedRandomRange = 0.2f;

    [Header("UV")]
    [Tooltip("Bật nếu gốc cây nằm ở phía UV.y = 1 (sprite bị lộn ngược trong texture).")]
    [SerializeField] private bool flipUVY = false;

    // ── Shader property ID cache (static → chỉ resolve 1 lần cho cả project) ──

    private static readonly int PropSwayStrength  = Shader.PropertyToID("_SwayStrength");
    private static readonly int PropSwaySpeed     = Shader.PropertyToID("_SwaySpeed");
    private static readonly int PropRootStiffness = Shader.PropertyToID("_RootStiffness");
    private static readonly int PropTopInfluence  = Shader.PropertyToID("_TopInfluence");
    private static readonly int PropPhaseOffset   = Shader.PropertyToID("_PhaseOffset");
    private static readonly int PropFlipUVY       = Shader.PropertyToID("_FlipUVY");

    private const string ExpectedShader = "Game/SpriteWindSway";

    // ── Internal ──────────────────────────────────────────────────────────────

    private SpriteRenderer        _sr;
    private MaterialPropertyBlock _mpb;

    // Giữ runtime values riêng để OnValidate không re-randomize mỗi lần chỉnh Inspector
    private float _runtimePhase;
    private float _runtimeSpeed;
    private bool  _runtimeReady;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();

        // Randomize 1 lần duy nhất khi object được tạo trong gameplay
        _runtimePhase = randomizeOnAwake ? Random.Range(0f, Mathf.PI * 2f) : phaseOffset;
        _runtimeSpeed = randomizeOnAwake
            ? swaySpeed * (1f + Random.Range(-speedRandomRange, speedRandomRange))
            : swaySpeed;
        _runtimeReady = true;

        ValidateShader();
        PushToMPB(_runtimePhase, _runtimeSpeed);
    }

    private void OnValidate()
    {
        // Gọi cả trong Edit Mode lẫn Play Mode khi Inspector thay đổi
        if (_sr  == null) _sr  = GetComponent<SpriteRenderer>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // Edit mode: dùng phaseOffset tĩnh để preview nhất quán
        // Play mode: dùng runtime values đã randomize từ Awake
        float phase = _runtimeReady ? _runtimePhase : phaseOffset;
        float speed = _runtimeReady ? _runtimeSpeed  : swaySpeed;

        PushToMPB(phase, speed);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void PushToMPB(float phase, float speed)
    {
        if (_sr == null) return;

        // GetPropertyBlock trước để không xóa properties của script khác trên cùng renderer
        _sr.GetPropertyBlock(_mpb);

        _mpb.SetFloat(PropSwayStrength,  swayStrength);
        _mpb.SetFloat(PropSwaySpeed,     speed);
        _mpb.SetFloat(PropRootStiffness, rootStiffness);
        _mpb.SetFloat(PropTopInfluence,  topInfluence);
        _mpb.SetFloat(PropPhaseOffset,   phase);
        _mpb.SetFloat(PropFlipUVY,       flipUVY ? 1f : 0f);

        _sr.SetPropertyBlock(_mpb);
    }

    private void ValidateShader()
    {
        if (_sr == null || _sr.sharedMaterial == null) return;
        if (_sr.sharedMaterial.shader.name != ExpectedShader)
        {
            Debug.LogWarning(
                $"[EnvironmentSway] '{name}': material đang dùng shader " +
                $"'{_sr.sharedMaterial.shader.name}', cần '{ExpectedShader}'.\n" +
                "Tạo material với shader 'Game/SpriteWindSway' và assign vào SpriteRenderer.",
                this);
        }
    }

    // ── Presets ───────────────────────────────────────────────────────────────

    [ContextMenu("Preset – Tree Large")]
    private void PresetTreeLarge()
    {
        swayStrength     = 0.08f;
        swaySpeed        = 0.9f;
        rootStiffness    = 3.0f;
        topInfluence     = 1.0f;
        speedRandomRange = 0.15f;
        randomizeOnAwake = true;
    }

    [ContextMenu("Preset – Bush")]
    private void PresetBush()
    {
        swayStrength     = 0.12f;
        swaySpeed        = 1.4f;
        rootStiffness    = 2.0f;
        topInfluence     = 1.2f;
        speedRandomRange = 0.25f;
        randomizeOnAwake = true;
    }

    [ContextMenu("Preset – Grass Decoration")]
    private void PresetGrassDecoration()
    {
        swayStrength     = 0.15f;
        swaySpeed        = 1.8f;
        rootStiffness    = 1.5f;
        topInfluence     = 1.5f;
        speedRandomRange = 0.30f;
        randomizeOnAwake = true;
    }
}
