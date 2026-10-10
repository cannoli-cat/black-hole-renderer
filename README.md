# Black Hole Renderer

A real-time, ray-marched black hole for Unity URP. Gravitational lensing of your skybox, and a spinning accretion disk with blackbody color, a physically based temperature profile, and relativistic Doppler and gravitational redshift.

![Black hole with the M87 preset](Documentation~/black-hole.gif)

## Installation

Requires **Unity 6** with **URP** (RenderGraph).

In Unity, open **Window → Package Manager**, click **+ → Install package from git URL…**, and enter:

```
https://github.com/cannoli-cat/black-hole-renderer.git#v2.6.0
```

## Setup

1. Add **Black Hole Feature** to your URP renderer (Renderer asset → **Add Renderer Feature**), and assign the `BlackHole` shader and `BlackHole` compute shader from this package.
2. Add the **Black Hole** component to a GameObject. Its position is the black hole's position.
3. Use a **cubemap skybox** material in Lighting settings. It's what gets lensed. Without one, the background renders black.

Tune the radius, spin, disk size, temperature, disk exposure and sky intensity on the renderer feature.

## Presets

The renderer feature has a **Preset** field. Assign one and click **Apply** to copy its values into the settings, or click **Save Current to Preset** to store yours. The package includes:

- **Realistic**: a physically based thin disk around a fast-spinning black hole.
- **Quasar**: a hotter, brighter, more turbulent disk.
- **M87**: a thick, dim, orange disk, like the Event Horizon Telescope image.
- **Interstellar**: a Gargantua-style disk with no Doppler shift, long hair-like streaks and a frayed outer edge.
- **Flare**: the Realistic disk with an orbiting hot spot, to show its delayed echoes.

Create your own with **Create → CannoliCat → Black Hole Preset**.

## Ray tracing

**Exact** (the default) traces true Kerr light paths around a spinning black hole. **Fast** uses Schwarzschild bending with approximate frame dragging. It's cheaper, so use it on WebGL and mobile.

## Observer

**Observer Motion** sets how the camera moves:

- **Manual**: uses **Observer Velocity**, a world-space velocity as a fraction of the speed of light. Zero means at rest relative to the black hole.
- **Orbiting**: a circular orbit at the camera's current distance, moving with the disk.
- **Falling**: falling straight in from rest far away.

Moving fast bends the view toward the direction of motion and shifts colors blue ahead and red behind. Close to the hole, light falling down to the camera is also shifted bluer and brighter. In Exact mode the camera can also go inside the horizon: there it's always a freely falling observer, since nothing can stay at rest. Fast mode needs the camera to stay outside.

To fly a real orbit, add **Black Hole Orbit Camera** to your camera. It moves the camera around the hole at the correct orbital speed, on the same clock as the disk, so the gas beside you keeps pace with you. While it's active, that camera always renders as Orbiting. Raise the disk's **Evolution Speed** or the component's **Speed Multiplier** for a faster orbit.

To fall in, add **Black Hole Fall Camera** instead. It drops the camera from where you placed it, at the correct speed, through the horizon, and stops just short of the center, optionally looping. While it's active, that camera renders as Falling with Exact ray tracing. Set **Look** to **Away** inside the horizon to see the outside universe shrink into a bright patch behind you.

## Hot spot

Raise **Hot Spot Strength** to add a bright blob orbiting in the disk. Light from it reaches the camera along several paths that take different amounts of time, so you see it directly and then again as delayed echoes in the lensed image above the hole and in the photon ring. **Hot Spot Radius** sets its orbit (as a multiple of the innermost stable orbit) and **Hot Spot Size** its size.

## Disk look

Everything is physically based by default. **Doppler Strength** and **Temperature Falloff** at 1 are physical. Lower them for an *Interstellar*-style disk.

The turbulence settings shape the gas:

- **Turbulence Contrast**: how much the turbulence heats and cools the gas.
- **Filament Sharpness**: soft clouds at 0, sharp filaments at 1.
- **Turbulence Warp**: swirls the gas into eddies.
- **Orbital Stretch**: stretches the gas into streaks along the orbit.
- **Edge Fraying**: breaks the outer disk into separate wispy strands.
- **Plunging Gas** and **Plunge Glow**: gas spiralling in from the disk's inner edge to the horizon.

If it runs too slowly, lower **Render Scale** on the renderer feature. It renders the black hole and sky at a fraction of the screen resolution and upscales them, while scene objects stay sharp. 0.75 is about 1.8x cheaper, 0.5 about 4x.

For the best result, turn on **HDR** in your URP asset and use a tonemapper (Neutral or ACES). The disk is much brighter than 1, and without HDR the bright side gets clipped to flat white.

## License and attribution

**CC BY 4.0.** You may use, modify and redistribute this, including in commercial games, **as long as you give credit.**
In your game's credits (or documentation / about screen), include:

> Black Hole Renderer by Tyler Wolfe (cannoli-cat) — https://github.com/cannoli-cat/black-hole-renderer

See [LICENSE.md](LICENSE.md) for the full license text.
