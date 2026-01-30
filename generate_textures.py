import numpy as np
from PIL import Image, ImageOps
import sys
import os

def generate_maps(input_path, normal_path, mask_path):
    print(f"Processing {input_path}...")
    try:
        img = Image.open(input_path).convert('RGB')
        arr = np.array(img).astype(float)
        
        # --- Generate Normal Map ---
        # Simple Sobel filter for gradients
        grayscale = np.mean(arr, axis=2)
        
        # Gradients (using simple differences)
        dy = np.diff(grayscale, axis=0, append=grayscale[-1:, :]) * 2.0 # Amplify bump
        dx = np.diff(grayscale, axis=1, append=grayscale[:, -1:]) * 2.0
        
        # Construct Normal vector (x, y, z)
        # Unity standard normal map: Z is up (blue), X/Y are slopes.
        # Actually usually encoded as RGB: R=X, G=Y, B=Z
        
        h, w = grayscale.shape
        normals = np.zeros((h, w, 3))
        
        # Scale down dx/dy to keep z dominant
        scale = 0.5
        
        # Normalize
        z = np.ones((h, w)) / scale
        length = np.sqrt(dx**2 + dy**2 + z**2)
        
        normals[:,:,0] = (dx / length + 1.0) * 0.5 * 255 # R
        normals[:,:,1] = (dy / length + 1.0) * 0.5 * 255 # G
        normals[:,:,2] = (z / length + 1.0) * 0.5 * 255  # B
        
        norm_img = Image.fromarray(normals.astype(np.uint8))
        norm_img.save(normal_path)
        print(f"Saved Normal Map: {normal_path}")
        
        # --- Generate Smoothness/Metallic Mask ---
        # Unity Standard Shader MaskMap: 
        # R = Metallic
        # G = Occlusion
        # B = Detail Mask (not used usually)
        # A = Smoothness
        
        # Logic for Wet Asphalt:
        # Darker areas in Albedo often imply wetness (high smoothness).
        # Lighter areas (lines) are matte (low smoothness).
        
        # Invert brightness for smoothness base
        brightness = np.mean(arr, axis=2) / 255.0
        
        # Enhance contrast for puddles
        # Pixels < 0.4 brightness -> Super Smooth (puddle)
        # Pixels > 0.6 brightness -> Rough (dry/lines)
        
        smoothness = (1.0 - brightness) 
        smoothness = np.clip((smoothness - 0.3) * 2.5, 0, 1) # Contrast stretch
        
        # Puddle logic: Threshold
        # If very dark, make it 1.0 smooth
        puddles = brightness < 0.25
        smoothness[puddles] = 1.0
        
        # Metallic: Asphalt is non-metal (0). Water is non-metal (0.02, but usually handled by smoothness).
        # But we can make puddles slightly metallic to fake sky reflection punchiness if needed.
        # Let's keep Metallic low/zero for realism.
        metallic = np.zeros_like(brightness)
        
        # Occlusion: Cracks are dark.
        occlusion = brightness
        
        mask = np.zeros((h, w, 4))
        mask[:,:,0] = metallic * 255 # R
        mask[:,:,1] = occlusion * 255 # G
        mask[:,:,2] = 0 # B
        mask[:,:,3] = smoothness * 255 # A
        
        mask_img = Image.fromarray(mask.astype(np.uint8), 'RGBA')
        mask_img.save(mask_path)
        print(f"Saved Mask Map: {mask_path}")
        
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    base_dir = "Assets/Resources/Textures"
    # Ensure dir exists
    if not os.path.exists(base_dir):
        os.makedirs(base_dir)

    # Road
    generate_maps(
        f"{base_dir}/HighwayTex.jpg",
        f"{base_dir}/HighwayNormal.png",
        f"{base_dir}/HighwayMask.png"
    )

    # Attempt Building generation if file exists
    building_path = f"{base_dir}/BuildingColor.jpg"
    if os.path.exists(building_path):
        # For buildings, windows are bright usually (neon/lights) or dark (glass).
        # Let's assume a "Cyber" look where bright parts = smooth/emissive.
        try:
             img = Image.open(building_path).convert('RGB')
             arr = np.array(img)
             bright = np.mean(arr, axis=2)
             
             # High smoothness for bright areas
             smooth = np.clip(bright * 1.5, 0, 255).astype(np.uint8)
             
             # Save as single channel spec map for now or overwrite alpha of albedo if possible.
             # Easier: Create a separate Smoothness map
             Image.fromarray(smooth).save(f"{base_dir}/BuildingSmoothness.png")
             print("Saved Building Smoothness Map")
        except:
            pass
