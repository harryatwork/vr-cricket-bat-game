using UnityEngine;

// The imported fly model has no separate wing geometry or baked animation (Sketchfab
// metadata confirmed animationCount=0) - this builds two simple translucent wing quads at
// runtime and flaps them rapidly around their hinge, giving a believable "flying insect"
// look without needing to reverse-engineer or re-rig the base mesh.
public class FlappingWings : MonoBehaviour
{
    public float FlapSpeed = 22f; // flaps per second range - fast, fly-like
    public float FlapAngle = 55f;

    private Transform _leftWing;
    private Transform _rightWing;
    private float _phase;

    void Start()
    {
        _phase = Random.Range(0f, Mathf.PI * 2f);

        var wingMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wingMat.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.9f, 0.35f));
        wingMat.SetFloat("_Surface", 1f); // transparent
        wingMat.SetFloat("_Blend", 0f);
        wingMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        wingMat.renderQueue = 3000;

        _leftWing = BuildWing("WingLeft", wingMat, -1f);
        _rightWing = BuildWing("WingRight", wingMat, 1f);
    }

    private Transform BuildWing(string name, Material mat, float side)
    {
        var hinge = new GameObject(name + "Hinge");
        hinge.transform.SetParent(transform, false);
        hinge.transform.localPosition = new Vector3(side * 0.35f, 0.15f, 0f);
        hinge.transform.localRotation = Quaternion.identity;

        var wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wing.name = name;
        Object.Destroy(wing.GetComponent<Collider>());
        wing.transform.SetParent(hinge.transform, false);
        wing.transform.localPosition = new Vector3(side * 0.5f, 0f, 0f);
        wing.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        wing.transform.localScale = new Vector3(1f, 0.55f, 1f);
        wing.GetComponent<Renderer>().sharedMaterial = mat;

        return hinge.transform;
    }

    void Update()
    {
        _phase += Time.deltaTime * FlapSpeed * Mathf.PI * 2f;
        float angle = Mathf.Sin(_phase) * FlapAngle;
        if (_leftWing != null) _leftWing.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (_rightWing != null) _rightWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}
