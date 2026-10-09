# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

## [2.5.0]

### Added
- Render Scale setting. It renders the black hole and sky at a fraction of the screen resolution and upscales them, while scene objects stay at full resolution. 0.75 is about 1.8x cheaper, 0.5 about 4x.

### Changed
- Fast mode is about 2.7× faster. It now uses RK4 steps the size of Exact mode's instead of many small Euler steps, with the same accuracy, so rays take far fewer steps through the disk.
- The disk costs nothing when Disk Density is 0.
- Rays stop early once the disk in front of them is opaque, which makes views from inside or close to a dense disk much cheaper.
- The disk turbulence is sampled by distance travelled instead of every step, so thin, glowing disks seen from inside or close to the hole cost far less (about 1.6x in Exact mode and 2.4x in Fast mode inside the disk). The look is unchanged.

## [2.4.0]

### Added
- The camera can now go inside the horizon in Exact mode. Light is traced backward through the real spacetime, and inside the horizon (or in Falling mode) the camera is a freely falling observer, so the view stays correct all the way in.
- Black Hole Fall Camera component. It drops the camera from where it's placed, at the correct speed on the disk's clock, through the horizon, and stops just short of the center. It can loop.

### Changed
- Falling mode in Exact ray tracing is now built directly from a freely falling observer instead of a boosted observer at rest. Outside the horizon the result is the same.
- The starfield in Exact mode is rotated a few degrees around the spin axis compared with 2.3.0, from the change in how rays are traced. The black hole and disk are unchanged.

## [2.3.0]

### Added
- Moving observer. The camera can now move at relativistic speed: the view bunches toward the direction of motion, and light ahead is shifted blue and brighter while light behind is shifted red and dimmer. Set it with Observer Velocity, as a fraction of the speed of light.
- Observer Motion setting. Orbiting puts the camera in a circular orbit at its current distance, moving with the disk. Falling drops it from rest far away, straight toward the hole. Manual uses Observer Velocity.
- Black Hole Orbit Camera component. It flies the camera on a circular orbit at the physically correct speed, on the same clock as the disk, and renders that camera as Orbiting.
- Gravitational blueshift. Light falling down to the camera gains energy, so the sky and disk look bluer and brighter close to the hole.

### Fixed
- Close to the hole, the view in Exact mode was rendered as if the camera were falling inward, which made the shadow look too big (57° instead of 45° at 3 Schwarzschild radii). The camera is now at rest relative to the hole's rotating frame, and the shadow matches theory.

## [2.2.1]

### Fixed
- Fine disk detail no longer shimmers far away or in the lensed image above the hole. Noise detail smaller than a pixel now fades out smoothly.
- A dark notch above the shadow with very thin disks. The disk's gas is now integrated exactly along each ray step instead of sampled at step endpoints, so very thin disks render correctly and match their density settings more closely.

## [2.2.0]

### Added
- Exact Kerr ray tracing. Light now follows the true paths around a spinning black hole, so the shadow and the lensed disk have the right shape at any spin. A new Ray Tracing setting picks Exact (the default) or Fast, the old approximation. Fast is cheaper and recommended for WebGL and mobile.
- Presets. Assign a Black Hole Preset on the renderer feature and click Apply, or click Save Current to Preset to store your settings. Four come with the package: Realistic, Quasar, M87 and Interstellar. Make your own with **Create → CannoliCat → Black Hole Preset**.
- Turbulence Contrast setting. The turbulence now heats and cools the gas too, not just thins it, so the disk gets brighter hot spots and darker lanes.
- Filament Sharpness setting. It goes from soft clouds to sharp, stringy filaments.
- Turbulence Warp setting. It swirls the turbulence into eddies.
- Orbital Stretch setting. It stretches the turbulence into long streaks along the orbit, the way differential rotation shears real gas.
- Edge Fraying setting. It breaks the outer disk into separate wispy strands with gaps between them.
- Plunging region. Gas now spirals in from the inner edge of the disk (the ISCO) to the horizon, with its own inflow speed and redshift. Plunging Gas sets how much there is and Plunge Glow sets its temperature. It's most visible at low spin, where the gap between the ISCO and the horizon is widest.

### Changed
- In Exact mode, the disk's radius is the true Kerr radius, so the temperature and orbit speed are right close to the hole.
- `BlackHoleParams.ApplyToCompute` no longer takes a kernel index.

### Fixed
- Streaky "fingers" at the edge of the shadow.
- Red crescents near the disk plane, caused by steps that were too large to resolve a thin disk.

## [2.1.0]

### Added
- Doppler Strength setting. At 1 the Doppler shift and beaming are physical. At 0 they're off, like the disk in Interstellar.
- Temperature Falloff setting. At 1 the temperature falls off physically. Lower values keep the outer disk hotter, so it glows all the way out.

### Fixed
- The spin direction now matches the direction the disk orbits. Before, the disk was orbiting against the spin.

## [2.0.0]

The accretion disk is now physically based. It looks different from 1.x, so you'll probably want to retune your settings.

### Added
- Disk Exposure setting. It controls the disk's brightness without touching the sky.
- Blackbody color. The disk's color now comes from Planck's law and the CIE color matching functions, baked into a small lookup texture at startup.
- The turbulence now orbits. Inner gas moves faster than outer gas, so it shears into spirals.

### Changed
- The disk's temperature now drops to zero at the inner edge and peaks just outside it, like a real thin disk (Novikov–Thorne).
- Doppler shift and gravitational redshift are now one combined Kerr redshift. It sets both the color and the brightness, so the approaching side is brighter and bluer for physical reasons.
- Brighter temperatures are now actually brighter. Before, every temperature had the same brightness.
- Evolution Speed now changes how fast time passes. Moving the slider no longer jumps the animation.

### Removed
- Beaming Power. Beaming comes from the physics now, so the slider isn't needed. Use Disk Exposure for brightness.

## [1.0.0]

First release as a Unity package (`com.cannoli-cat.black-hole`).

### Added
- Ray-marched black hole renderer feature with fragment and compute paths.
- Gravitational lensing of the scene's cubemap skybox.
- Accretion disk with spin, temperature-based color, relativistic beaming, and animated noise.
- `BlackHole` component marking the active black hole's position.
