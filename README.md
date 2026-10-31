# FastAbsorption

![FastAbsorption Demo](https://github.com/DysonSphereMod/FastAbsorption/blob/master/FastAbsorption.gif?raw=true)

This mod increase Dyson sphere absorption frequency of solar sails (defaults to one sail every 2 second).

No more waiting for those puny sails to find their way to a node !  
The only limits are your production and shooting speed!


## Configuration

The speed up is configurable (you must start the game at least once for the configuration file to be created) using the following parameters:

### frequencyMultiplier
> How much more frequently should sail be requested by every DysonSphere node.
- `Min Value     : 1 (one sail every 2 second)`
- `Max value     : 120 (one sail every frame)`
- `Default value : 10`

### travelSpeedMultiplier
> How much faster do sails take to travel to the requesting node.  
> Values greater than 1 make the sail teleport to the proximity of the target node.
- `Min Value     : 1 (4 minutes travel time)`
- `Max value     : 120 (2 seconds travel time)`
- `Default value : 1`
  
  
_Please  restart your game to apply any configuration changes._
  
  
## Building

The project is an SDK-style C# project (net472) and is compiled against the exact
BepInEx 5 and game assemblies it runs with. Any .NET SDK (6+) or Mono/msbuild works.

```
dotnet build -c Release ^
  -p:BepInExCoreDir="<game>\BepInEx\core" ^
  -p:GameManagedDir="<game>\DSPGAME_Data\Managed"
```

- `BepInExCoreDir` : the BepInEx 5 `core` folder of your install
  (Windows: `<game>\BepInEx\core` - with r2modman:
  `~/.config/r2modmanPlus-local/DysonSphereProgram/profiles/<Profile>/BepInEx/core` on Linux)
- `GameManagedDir` : the game's `DSPGAME_Data\Managed` folder

The resulting `FastAbsorption.dll` goes into `<game>\BepInEx\plugins\` (or the
plugins folder of your mod manager profile).


## Changelog

### v1.2.0
- Updated for the current game version: `DysonSphereLayer.GameTick` no longer uses a
  hard-coded constant `120` for the absorb interval; it is now the
  `GameHistoryData.dysonNodeAbsorbInterval` field. The frequency patch is now a
  postfix that scales that field at runtime (reflection), which also survives
  future constant changes.
- The `DysonSwarm.AbsorbSail` transpiler now keeps the `ldc.i4` opcode (the constant
  is loaded as a 32-bit int in the current game; Harmony refuses to rewrite an
  `ldc.i4` into an `ldc.i8`).
- Patches are applied separately so a failure in one no longer prevents the other,
  and every failure is reported to the BepInEx log.
- Rewrote the project file as an SDK-style project that compiles against the
  installed game/BepInEx assemblies (see Building).

### v1.1.0
- Add frequency and travel time multiplier configuration

### v1.0.0
- Initial Release




-----

Special thanks to Nordblum for the mod icon.
