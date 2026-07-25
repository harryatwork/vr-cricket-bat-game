using UnityEngine;

// Per-firefly gaze-dwell interaction plus slow continuous wandering flight.
// Movement is physics-driven (Rigidbody velocity, not teleported transform writes) so
// fireflies actually collide with the room's walls/furniture colliders instead of drifting
// through them - Harry's explicit requirement ("only rigid... can't go through walls").
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

    private const float _wanderSpeed = 0.45f; // slow, deliberate drift - not a darting insect
    private const float _bobAmplitude = 0.1f;
    private const float _bobFrequency = 0.6f;
    private const float _arriveThreshold = 0.2f;

    private Transform _flyVisual;
    private Vector3 _flyBaseScale;
    private Vector3 _flyVisualBaseLocalPos;
    private Light _glowLight;
    private float _baseLightIntensity;
    private bool _isDwelling = false;
    private float _dwellTimer = 0f;
    private ParticleSystem _catchBurst;
    private Rigidbody _rb;

    private Vector3 _wanderTarget;
    private float _bobPhase;

    void Start()
    {
        var visualGo = transform.Find("FlyVisual");
        _flyVisual = visualGo != null ? visualGo : transform;
        _flyBaseScale = _flyVisual.localScale;
        _flyVisualBaseLocalPos = _flyVisual.localPosition;

        _glowLight = GetComponentInChildren<Light>();
        _baseLightIntensity = _glowLight != null ? _glowLight.intensity : 1f;
        _catchBurst = GetComponentInChildren<ParticleSystem>();
        _bobPhase = Random.Range(0f, Mathf.PI * 2f);

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.linearDamping = 2f; // settle quickly rather than sliding/bouncing around after a wall hit

        _rb.position = RandomPointInBounds();
        PickNewWanderTarget();
    }

    void Update()
    {
        // Cosmetic bob applied to the visual child only, never to the physics body itself -
        // keeps the rigidbody's actual collision position exactly where physics resolves it.
        _bobPhase += Time.deltaTime * _bobFrequency * Mathf.PI * 2f;
        _flyVisual.localPosition = _flyVisualBaseLocalPos + new Vector3(0f, Mathf.Sin(_bobPhase) * _bobAmplitude, 0f);

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

        Vector3 toTarget = _wanderTarget - _rb.position;
        if (toTarget.magnitude <= _arriveThreshold)
        {
            PickNewWanderTarget();
            toTarget = _wanderTarget - _rb.position;
        }

        Vector3 dir = toTarget.normalized;
        _rb.linearVelocity = dir * _wanderSpeed;

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.forward = Vector3.Lerp(transform.forward, dir, Time.fixedDeltaTime * 2f);
        }
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

        // Respawn elsewhere in the wander zone and resume wandering, rather than vanishing
        // and reappearing in the same spot.
        _rb.position = RandomPointInBounds();
        _rb.linearVelocity = Vector3.zero;
        PickNewWanderTarget();
        if (_glowLight != null) _glowLight.intensity = _baseLightIntensity;
        _flyVisual.localScale = _flyBaseScale;
    }

    private void PickNewWanderTarget()
    {
        _wanderTarget = RandomPointInBounds();
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
