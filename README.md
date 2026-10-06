# Black Hole Renderer

A real-time, ray-marched black hole for Unity URP: gravitational lensing of your skybox, a spinning accretion disk with temperature-based color and relativistic beaming.

## Installation

Requires **Unity 6** with **URP** (RenderGraph).

In Unity, open **Window → Package Manager**, click **+ → Install package from git URL…**, and enter:

```
https://github.com/cannoli-cat/black-hole-renderer.git#v1.0.0
```

## Setup

1. Add **Black Hole Feature** to your URP renderer (Renderer asset → **Add Renderer Feature**), and assign the `BlackHole` shader and `BlackHole` compute shader from this package.
2. Add the **Black Hole** component to a GameObject. Its position is the black hole's position.
3. Use a **cubemap skybox** material in Lighting settings; it is what gets lensed. Without one, the background renders black.

Tune the radius, spin, disk size, temperature, beaming and sky intensity on the renderer feature.

## License and attribution

**CC BY 4.0.** You may use, modify and redistribute this, including in commercial games, **as long as you give credit.**
In your game's credits (or documentation / about screen), include:

> Black Hole Renderer by Tyler Wolfe (cannoli-cat) — https://github.com/cannoli-cat/black-hole-renderer

See [LICENSE.md](LICENSE.md) for the full license text.
