🎮 Pixel Animator Capture Tool – Usage Guide
This Unity Editor tool allows you to convert 3D character animations into pixelated 2D sprite sheets and prefabs, just like Dead Cells style visuals.

🧰 Features
Converts any Animator animation into a pixel-art style sprite sheet.

Automatically creates .anim animation clips and prefab with Animator.

Optional texture settings: Point filtering, MipMap disabling, Compression disabling.

Supports multiple animation states at once.

Previews generated sprite sheets and prefabs in the editor.

🚀 How to Use
Open the Tool: Go to Tools > Pixel Animator Capture in the Unity Editor menu.

Assign References:

Drag your Camera and Animator (with assigned animation controller) into the fields.

Add your desired animation state names (e.g., "Idle", "Run", "Attack").

Configure Capture Settings:

Set Frame Count, Frame Rate, Resolution, and Pixel Size.

(Optional) Adjust Texture Settings:

Enable Point Filtering for crisp pixels.

Disable MipMaps and Compression if needed.

Click “Start Capture for All Animations”:

This will run each animation, capture it frame-by-frame, and save the results as:

markdown
Kopyala
Düzenle
Assets/
  CapturedFrames/
    Idle/
      sprite_sheet.png
      GeneratedAnim.anim
      GeneratedPrefab.prefab
Preview & Use Assets:

Scroll down to view sprite sheet previews.

Use “Instantiate in Scene” to add the prefab into your scene.
