# Structural material asset specification

Purpose: existing modular building blocks, Unity6000 URP, metre grid, PC desktop. Four structural Prefabs only; functional machinery retains its authored identity. Shared materials/textures avoid per-block material instances. Authoring source: Assets/Editor/Building/Materials/BlockMaterialAuthoring.cs; no external generated or licensed art.

| ID | density kg/m3 | durability/m3 | coins/m3 | appearance and break |
|---|---:|---:|---:|---|
| metal | 2.0 | 150 | 8 | cool steel, brushed detail, metallic .75, compact fragments |
| wood | .6 | 65 | 3 | warm brown grain, rough surface, elongated splinters |
| stone | 2.6 | 190 | 5 | mottled grey, rough surface, angular chips |
| glass | 1.2 | 30 | 6 | cyan clear glass, alpha .28, thin triangular shards |

Values are game units and provisional balance, not real physical material constants. Density/health/cost use the actual block volume. Shader URP/Lit; base detail textures64x64 RGB sRGB, no normal/ORM packing. Glass intentionally transparent and double-sided; no refraction. Generic source UVs reused. No geometry/collider modifications to structural Prefabs.

Four-vertex closed fragment meshes (4 triangles), no collider/rigidbody; max32 particles per effect, max32 simultaneous cosmetic effects, lifetime<=1.1s. Source mesh/texture seeds and generation are preserved in the authoring tool. Particle curves/Prefab parameters and catalog are editable Unity assets. Installer fills absent assets and support components, preserves edited resources. Selector installer owns only MaterialSelector in saved Main and makes a scene backup before saving.

Use Tools/HY-Sandbox/Building/Install structural materials then Install material selector in Main. Null/empty materialId is the legacy compatibility path and does not reprice or remodel existing blueprints. New placement defaults to metal; selecting material affects subsequent placement only. These four starter surfaces need actual visual inspection; generation and static build alone do not constitute art acceptance.
