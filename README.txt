NEON RUNNER - MIAMI BEACH  (Unity endless runner)
=================================================
Same game as the Tokyo Night project, re-themed to the "MIAMI BEACH" environment from your design slide.

OPEN IT IN UNITY HUB (1 minute)
1. Unzip NeonRunner_MiamiBeach.zip.
2. Unity Hub > Projects > Add > "Add project from disk" > select the NeonRunner_MiamiBeach folder.
   Editor version: 6000.5.8f1 (same as your Tokyo project). If Hub offers another installed 6000.x editor, that works too.
3. Wait for the first import (1-2 min). The project creates Assets/Scenes/MiamiBeach.unity by itself.
4. Press PLAY. Everything (beach, hotels, palms, character, UI) is built from code - nothing to drag in.
   If the scene was not created, just open any scene and press PLAY.

CONTROLS
  Keyboard: LEFT/RIGHT (A/D) lane, UP/W/SPACE jump, DOWN/S roll, ESC/P pause, R rain shower, M mute music
  Mobile/mouse: swipe left/right/up/down

WHAT IS NEW IN THE MIAMI BEACH THEME
  - Bright sunny day: gradient sky, haze, clouds, warm sunlight (Tokyo was night).
  - LEFT side = beach: boardwalk, sand, sea with foam line, palm trees, beach umbrellas + towels,
    lifeguard towers, surfboards stuck in the sand, flamingos.
  - RIGHT side = pastel Art Deco hotels (pink / mint / aqua / butter / peach) with neon trim, awnings,
    rooftop masts and glass towers behind.
  - Coral Art Deco welcome arches (with a sun emblem) over the track; title screen has the arch, sun,
    swaying party lights, palms and falling confetti.
  - Miami trains (teal/pink, white/turquoise, coral/yellow, sun-yellow/hot-pink), turquoise + hot-pink track lines.
  - Shop styles: CLASSIC, SOUTH BEACH, OCEAN BLUE, GOLDEN SUN, PALM GREEN, SUNSET.
    Hoverboards: FLAMINGO, OCEAN WAVE, SUNSHINE, PALM LEAF.
  - Home banner says MIAMI BEACH.

ALL FEATURES FROM THE TOKYO PROJECT ARE KEPT
  3 lanes, jump & roll, trains / ramps / barriers / overhead gantries / cargo, coins, mystery boxes,
  Inspector + Dog chase, power-ups (Magnet, 2X Score, Jetpack, Speed Boost, Score Boost, Hoverboard),
  HUD, Game Over (Revive / Home / Retry), daily missions, shop, your sound effects + music,
  best score and coins saved (PlayerPrefs), bloom glow when the project uses URP.

MODELS (read straight from the .glb files, no importer package needed)
  Assets/Models/MiamiEnvironment/Jack.glb            -> the runner character
  Assets/Models/MiamiEnvironment/subway_surfers.glb  -> the rails/track
  Optional: prefabs in Assets/MiamiAssets are used as extra roadside hotels.
  Credits (CC-BY-4.0): "Jake 3D Modeling Subway Surfers" - Javier Contreras (sketchfab.com/Javier.ConRod)
                       "Subway Surfers" map - Capy Studio's (sketchfab.com/Capy_Studios)

BUILD FOR ANDROID / iOS
  File > Build Settings (the scene is already added) > pick the platform > Build.
  If objects look pink/flat in a build, add "Standard" (or "Universal Render Pipeline/Lit") and "Skybox/Procedural"
  under Project Settings > Graphics > Always Included Shaders. For builds also copy the .glb files to Assets/StreamingAssets/.

TWEAK
  Colours: MakeMaterials() in GameManager.cs.  Scenery: SpawnChunk() and the Add...() prop methods.
  Sky / fog / sun: SetupScene().  Speeds / lanes: Runner.cs and GameManager.cs.
