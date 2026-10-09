# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

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
