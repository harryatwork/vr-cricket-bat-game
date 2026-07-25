# CLAUDE.md — VRHeadSetSetup (Unity project for the DIY Mobile VR System)

> This Unity project belongs to Harry's DIY Mobile VR System. The **full project source of truth** lives at `D:\xampp\htdocs\VR-Make\CLAUDE.md` — read it before making non-trivial decisions here (hardware specs, tracking architecture, glove input spec, business/ecosystem constraints, communication contract).

## Game Visual Quality Directive — AUTO-APPLIES to ALL work in this project

> **Standing instruction (Harry, 2026-07-19): every game built for this platform must look ultra-realistic, HD, and deeply interactive — "best of the best." NOT a blocky/primitive prototype aesthetic. Apply automatically in every session; never wait to be re-asked.**

### Visual bar (non-negotiable defaults)
- **NEVER ship primitive-cube/capsule placeholder visuals in anything Harry will see in the headset.** Every visible object gets a real modeled mesh (ProBuilder-sculpted, imported model, or generated via asset tools), proper UVs, and PBR materials — albedo + normal + metallic/smoothness maps, not flat colors.
- **URP configured for maximum mobile fidelity:** HDR on, MSAA 4x, per-pixel lighting, reflection probes, light probes, baked global illumination (baked — not realtime GI) for every static environment. Post-processing volume in every scene: tonemapping (ACES), bloom, vignette, color grading tuned per scene mood.
- **Lighting is where realism lives:** every scene gets a deliberate lighting design (key/fill/ambient logic, baked shadows from a real sun/lamp source, emissive materials for glow sources) — never default skybox + single directional light left untouched.
- **Materials:** physically plausible values (real-world metallic/roughness references). Tile textures at real-world scale. No stretched or obviously repeating textures on hero surfaces.
- **Environment density:** scenes must feel *dressed* — props, decals, ground clutter, atmospheric depth (fog/haze where fitting), skybox matched to scene lighting. Empty floors and bare walls read as prototype; that's a defect, not a style.
- **Audio is part of realism:** spatialized 3D SFX for every interaction, ambient bed per scene, physics-driven impact sounds. A silent interaction is an unfinished interaction.

### Interactivity bar
- Everything the player can plausibly reach with glove hands should *respond*: grabbable via finger-pad grip (full fist = 4 fingers closed = grab), physics-reactive (Rigidbody + proper colliders + realistic mass), with feedback (visual + audio cues on touch/grab/release — gloves have no rumble).
- Hand presence is the product's soul: per-finger visualization must drive believable hand poses on the in-game hand model; near-miss/hover highlights on interactables; two-hand interactions where the genre fits.
- World reacts to the player: destructibles, knock-overs, particle responses on hits, persistent consequences within a session (broken stays broken).

### The one engineering constraint that PROTECTS realism (do not trade it away)
- Target device is Pixel 10 (Tensor G5) rendering **stereo at 2424×1080 total** via Google Cardboard XR Plugin. Hold a locked **60fps minimum** — in VR, dropped frames = motion sickness, and a nauseating "realistic" game is worse than none. Achieve the visual bar through: baked lighting over realtime, LODs on every mesh over ~5k tris, texture atlasing, GPU instancing, occlusion culling, draw-call budgets (~150-250), shader complexity discipline — NOT by lowering the visual bar. If a scene can't hold 60fps, optimize technique first (bake more, batch more, LOD harder); visual downgrade is the last resort and must be flagged to Harry.
- Profile on-device (Unity Profiler over USB) at every milestone, not just at the end.

### Asset sourcing order for realistic content
1. Generate/import high-quality assets via available MCP tools (generate_model, generate_image for textures, import_model) and refine materials in-engine.
2. Free high-quality PBR sources (Unity Asset Store free tier, Polyhaven — CC0) — verify license allows commercial redistribution inside the companion app before use.
3. ProBuilder hand-modeling with proper beveling/smoothing + full PBR texturing as fallback — never left as flat-shaded blockouts.
- Blockouts/greyboxing ARE allowed as an internal design step, but must be flagged as such and replaced before any build Harry tests in the headset.

## Project facts
- Unity 6000.5.4f1, URP, Google Cardboard XR Plugin (`com.google.xr.cardboard` v1.34.0, git install).
- Lens profile (measured from Harry's real Arcnet headset, 2026-07-19): 40mm lenses; inter-lens 60–75mm adjustable (default 67.5mm); lens-to-screen 35–45mm adjustable per-eye (default 40mm); ~103° FOV.
- Controller input arrives over WiFi UDP (ESP32 gloves: IMU rotation, IR-based XYZ position via Nano over USB serial, trigger + 4 finger pads, active LOW). See the main CLAUDE.md Sections 2–4.
- Harry is a complete beginner: explain every step, one thing at a time; he is the end user/tester — Claude drives the Editor via MCP.
