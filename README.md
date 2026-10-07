# Black Hole Renderer

A real-time, ray-marched black hole for Unity URP. Gravitational lensing of your skybox, and a spinning accretion disk with blackbody color, a physically based temperature profile, and relativistic Doppler and gravitational redshift.

![Black hole with a lensed accretion disk in front of the Milky Way](Documentation~/black-hole.gif)

## Installation

Requires **Unity 6** with **URP** (RenderGraph).

In Unity, open **Window → Package Manager**, click **+ → Install package from git URL…**, and enter:

```
https://github.com/cannoli-cat/black-hole-renderer.git#v2.1.0
```

## Setup

1. Add **Black Hole Feature** to your URP renderer (Renderer asset → **Add Renderer Feature**), and assign the `BlackHole` shader and `BlackHole` compute shader from this package.
2. Add the **Black Hole** component to a GameObject. Its position is the black hole's position.
3. Use a **cubemap skybox** material in Lighting settings. It's what gets lensed. Without one, the background renders black.

Tune the radius, spin, disk size, temperature, disk exposure and sky intensity on the renderer feature.

Everything is physically based by default. For an *Interstellar*-style disk, set **Doppler Strength** to 0 and **Temperature Falloff** to around 0.3.

For the best result, turn on **HDR** in your URP asset and use a tonemapper (Neutral or ACES). The disk is much brighter than 1, and without HDR the bright side gets clipped to flat white.

## License and attribution

**CC BY 4.0.** You may use, modify and redistribute this, including in commercial games, **as long as you give credit.**
In your game's credits (or documentation / about screen), include:

> Black Hole Renderer by Tyler Wolfe (cannoli-cat) — https://github.com/cannoli-cat/black-hole-renderer

See [LICENSE.md](LICENSE.md) for the full license text.
