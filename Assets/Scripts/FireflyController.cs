using UnityEngine;

// Per-firefly gaze-dwell interaction plus continuous forward flight with a vertical wave.
// Movement is physics-driven (Rigidbody velocity, not teleported transform writes) so
// fireflies actually collide with the room's walls/furniture colliders instead of drifting
// through them - Harry's explicit requirement ("only rigid... can't go through walls").
//
// Flight model: a horizontal heading that drifts gradually (never reverses sharply) combined
// with a vertical sine wave applied directly to the rigidbody's velocity, so the firefly is
// always moving forward AND undulating up/down at the same time - not alternating between
// the two, and never doubling back on itself the way a "pick a random point and beeline to
// it" approach would.
//
// Relies on Cardboard's own CardboardReticlePointer (Packages/com.google.xr.cardboard/
// Runtime/CardboardReticlePointer.cs) sending OnPointerEnter/OnPointerExit via
// GameObject.SendMessage - no custom raycasting needed here.
//
// Dwell (not click) is the catch trigger: the phone is sealed inside a light-blocking
// headset with no confirmed physical trigger button.
//
// The visible body is a child "FlyVisual" (an imported fly model) with its own fixed local
// scale - the root transform stays at scale 1 always, so dwell-growth is applied only to
// FlyVisual's scale and never compounds with the root's position/rotation driving.
[RequireComponent(typeof(Rigidbody))]
public class FireflyController : MonoBehaviour
{
    public float DwellSeconds = 1.8f;

    // Wander bounds centered on where Harry is actually standing, not the world origin -
    // set at scene-build time to whatever spot he's positioned at.
    public static Vector3 WanderCenter = new Vector3(-1f, 1.6f, 1f);
    private const float _wanderRadiusXZ = 1.8f;
    private const float _wanderMinY = 1.0f;
    private const float _wanderMaxY = 2.2f;

    private const float _forwardSpeed = 0.4f; // slow, deliberate drift - not a darting insect
    private const float _headingTurnRate = 0.8f; // radians/sec of gradual heading drift
    private const float _steerBackRate = 1.2f; // how quickly it re-aims at the center once outside the radius
    private const float _waveAmplitude = 0.35f; // vertical wave amplitude, within the Y bounds
    private const float _waveFrequency = 0.35f; // waves per second

    private Transform _flyVisual;
    private Vector3 _flyBaseScale;
    private Vector3 _flyVisualBaseLocalPos;
    private Light _glowLight;
    private float _baseLightIntensity;
    private bool _isDwelling = false;
    private float _dwellTimer = 0f;
    private ParticleSystem _catchBurst;
    private Rigidbody _rb;

    private Vector3 _heading; // horizontal, normalized
    private float _wavePhase;
    private float _noiseSeed;
    private float _centerY;

    void Start()
    {
        var visualGo = transform.Find("FlyVisual");
        _flyVisual = visualGo != null ? visualGo : transform;
        _flyBaseScale = _flyVisual.localScale;
        _flyVisualBaseLocalPos = _flyVisual.localPosition;

        // The imported fly model's tallest axis is vertical (built standing upright, not in a
        // natural horizontal flying pose) - rotate it so its body lies flat/horizontal instead.
        _flyVisual.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        _glowLight = GetComponentInChildren<Light>();
        _baseLightIntensity = _glowLight != null ? _glowLight.intensity : 1f;
        _catchBurst = GetComponentInChildren<ParticleSystem>();
        _wavePhase = Random.Range(0f, Mathf.PI * 2f);
        _noiseSeed = Random.Range(0f, 1000f);
        _centerY = (_wanderMinY + _wanderMaxY) * 0.5f;

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.linearDamping = 2f; // settle quickly rather than sliding/bouncing around after a wall hit

        _rb.position = RandomPointInBounds();
        float startAngle = Random.Range(0f, Mathf.PI * 2f);
        _heading = new Vector3(Mathf.Cos(startAngle), 0f, Mathf.Sin(startAngle));
    }

    void Update()
    {
        if (_isDwelling)
        {
            _dwellTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_dwellTimer / DwellSeconds);

            if (_glowLight != null) _glowLight.intensity = _baseLightIntensity * (1f + progress * 2f);
            _flyVisual.localScale = _flyBaseScale * (1f + progress * 0.4f);

            if (_dwellTimer >= DwellSeconds)
            {
                Catch();
            }
        }
    }

    void FixedUpdate()
    {
        if (_isDwelling)
        {
            // Deliberately frozen in place while being watched - reads as the firefly going
            // still under the player's gaze, and keeps the catch window fair.
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        // Gentle continuous heading drift (never a sharp reversal) using smooth noise, so the
        // flight path curves naturally instead of holding a perfectly straight line forever.
        float turn = (Mathf.PerlinNoise(Time.time * 0.25f, _noiseSeed) - 0.5f) * 2f * _headingTurnRate * Time.fixedDeltaTime;
        _heading = Quaternion.Euler(0f, turn * Mathf.Rad2Deg, 0f) * _heading;

        // If drifting outside the wander radius, gradually steer back toward the center rather
        // than snapping to a new point (which would mean flying backward).
        Vector3 toCenter = WanderCenter - _rb.position;
        toCenter.y = 0f;
        if (toCenter.magnitude > _wanderRadiusXZ)
        {
            Vector3 desiredDir = toCenter.normalized;
            _heading = Vector3.Slerp(_heading, desiredDir, Time.fixedDeltaTime * _steerBackRate).normalized;
        }

        // Vertical wave applied directly to velocity (derivative of a sine position curve) so
        // the actual body continuously undulates up/down while always moving forward
        // horizontally - both happen at once, never alternating.
        _wavePhase += Time.fixedDeltaTime * _waveFrequency * Mathf.PI * 2f;
        float targetY = _centerY + Mathf.Sin(_wavePhase) * _waveAmplitude;
        float verticalVelocity = (targetY - _rb.position.y) * 2f; // gently correct toward the wave curve

        Vector3 vel = _heading * _forwardSpeed;
        vel.y = verticalVelocity;
        _rb.linearVelocity = vel;

        transform.forward = Vector3.Lerp(transform.forward, _heading, Time.fixedDeltaTime * 2f);
    }

    // Called by CardboardReticlePointer via SendMessage when gaze enters this object.
    public void OnPointerEnter()
    {
        _isDwelling = true;
        _dwellTimer = 0f;
    }

    // Called by CardboardReticlePointer via SendMessage when gaze exits this object.
    public void OnPointerExit()
    {
        _isDwelling = false;
        _dwellTimer = 0f;
        if (_glowLight != null) _glowLight.intensity = _baseLightIntensity;
        _flyVisual.localScale = _flyBaseScale;
    }

    private void Catch()
    {
        _isDwelling = false;
        _dwellTimer = 0f;

        if (_catchBurst != null)
        {
            _catchBurst.transform.SetParent(null, true);
            _catchBurst.Play();
            Destroy(_catchBurst.gameObject, 2f);
        }

        if (FireflyGameManager.Instance != null)
        {
            FireflyGameManager.Instance.OnFireflyCaught();
        }

        // Respawn elsewhere in the wander zone and resume flying, rather than vanishing and
        // reappearing in the same spot.
        _rb.position = RandomPointInBounds();
        _rb.linearVelocity = Vector3.zero;
        float restartAngle = Random.Range(0f, Mathf.PI * 2f);
        _heading = new Vector3(Mathf.Cos(restartAngle), 0f, Mathf.Sin(restartAngle));
        if (_glowLight != null) _glowLight.intensity = _baseLightIntensity;
        _flyVisual.localScale = _flyBaseScale;
    }

    private Vector3 RandomPointInBounds()
    {
        Vector2 offset = Random.insideUnitCircle * _wanderRadiusXZ;
        return new Vector3(
            WanderCenter.x + offset.x,
            Random.Range(_wanderMinY, _wanderMaxY),
            WanderCenter.z + offset.y);
    }
}
