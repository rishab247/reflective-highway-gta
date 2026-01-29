# Reflective Highway - Phase 1 Upgrade

## Phase 1: Real Asphalt & Segmented Road

This update moves the project from a simple prototype to a higher fidelity foundation.

### Changes
- **Segmented Road System**: Replaced the single infinite plane with 100m road segments. This allows for better lighting, fog culling, and future texture variation.
- **PBR Material Setup**: The road material is now configured via code to use Normal and Specular highlights for a "wet asphalt" look.
- **Cinematic Lighting**: Adjusted global lighting to support the new road surface properties.

### How to Build
1. Open the project in Unity 2022.3.
2. Ensure `ReflectiveHighway/Assets/Resources/Materials/HighwayMaterial.mat` exists. If not, create a new Material and name it `HighwayMaterial`.
3. Open `ReflectiveHighway/Assets/Scenes/Main.unity`.
4. Go to **File > Build Settings**, switch to **Android**, and click **Build**.

### Troubleshooting
If you see pink textures:
- The shader might be missing. Select the `HighwayMaterial` and change the shader to `Standard`.
