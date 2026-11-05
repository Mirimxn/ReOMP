# Overlay crash & invisibility on multi-GPU (hybrid graphics) systems

How the Galaxy Unleashed overlay was made to stop crashing the game and to actually draw, especially on laptops with two GPUs.

## Symptoms

- Game crashed on launch (brief black flash) whenever galaxy-unleashed was installed; removing the mod fixed it.
- After the crash was fixed, the overlay was **invisible** - the cursor changed over UI elements and links were clickable, but no pixels drew.

## Root cause: the overlay shared a GPU texture across two GPUs

The overlay rendered the CEF web page into a **GPU shared texture** and drew it onto the game's frame each present. On a laptop with two GPUs (integrated + discrete), CEF's GPU process and the game run on **different GPUs**, and a D3D11 texture created on one GPU cannot be opened on the other:

- `OpenSharedResource1` returned `E_INVALIDARG`, so there was nothing to draw -> invisible overlay.
- Forcing both processes onto the same GPU (Windows per-app GPU preference) made the texture open, but then the copy ran on the game's D3D11 **immediate context from CEF's thread**, racing the game's own rendering -> GPU process crash.

Three distinct bugs were in play:

1. **Shader state not restored** after the overlay draw - left the game rendering with the overlay's shaders bound, crashing every machine at frame 1 (GPU-independent).
2. **Cross-GPU shared texture** - invisible on hybrid systems, or a GPU-process crash when forced onto the same GPU (above).
3. **FlipDiscard back buffer cached** - the render-target view was captured once, but a flip-model swapchain rotates its back buffer every frame, so the overlay drew to a buffer that wasn't being shown.

## Fix: software paint (no GPU shared texture)

Switched CEF to **software rendering** (`SharedTextureEnabled = false`):

- `OnPaint` (CEF thread) copies the CPU pixel buffer into a managed array.
- `Draw` (game render thread) uploads it to the texture via `UpdateSubresource`.

With no GPU shared texture there is no cross-GPU sharing and no cross-thread device-context use, so the overlay works regardless of which GPU each process runs on - no GPU-preference tweaks needed. The shader-state restore and per-frame back-buffer fixes are also kept.

## Code

`mod-sources/overlay1/overlay1/src/`: `DirectX11OverlayQuad.cs`, `DirectX11SwapChainPresentMethodHook.cs`, `DirectX11StateBlock.cs`, `Constructor.cs`.
