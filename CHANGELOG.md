# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

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
- The spin direction now matches the direction the disk orbits. Before, the disk was orbiting against the spin.

### Removed
- Beaming Power. Beaming comes from the physics now, so the slider isn't needed. Use Disk Exposure for brightness.

## [1.0.0]

First release as a Unity package (`com.cannoli-cat.black-hole`).

### Added
- Ray-marched black hole renderer feature with fragment and compute paths.
- Gravitational lensing of the scene's cubemap skybox.
- Accretion disk with spin, temperature-based color, relativistic beaming, and animated noise.
- `BlackHole` component marking the active black hole's position.
