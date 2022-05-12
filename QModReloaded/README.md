### Continuation of QMod Manager

- Removed console app, entirely GUI based.
- Can patch resolutions in.
- Enabled searching Steam libraries for game location to retrieve game location automatically.
- Installation and removal of mods via the GUI. Will create valid JSONS if one isn't found.
- Added patch to remove intros. This is permanent and will require a re-download of Assembly-CSharp.dll (and subsequent re-patching)
- Toggle mods on/off
- QOL features such as direct opening of mod and game directory and log files.
- Can start game via GUI. Launches via steam:// first, and then EXE directly if it fails.
- Implemented mod load order
- Doesnt rely on correct entry point being entered in JSON to load. Will search DLL's directly.

### Why:

I'm late to the party I know - I recently started playing SDV despite having it for years and never touching it, and love it. Remembered I had Graveyard Keeper on Steam as well as some other similar games. I know the modding scene isnt huge/active for GYK anymore, and that's fine. I mainly developed this to see if I could do it (using oldark87's code as a base). I'm not a software developer by profession, it's more of a hobby at the moment, so learning as I go. Enjoy.

### Installation:

Use the included installer and ensure it installs into your "steamapps\common\Graveyard Keeper\Graveyard Keeper_Data\Managed" folder.
___

### Images:

| ![alt text](https://github.com/p1xel8ted/GraveyardKeeper/QModReloaded/blob/master/main_ui.png?raw=true) | ![alt text](https://github.com/p1xel8ted/GraveyardKeeper/QModReloaded/blob/master/res_ui.png?raw=true)|
| ![alt text](https://github.com/p1xel8ted/GraveyardKeeper/QModReloaded/blob/master/checklist_ui.png?raw=true) | ![alt text](https://github.com/p1xel8ted/GraveyardKeeper/QModReloaded/blob/master/about_ui.png?raw=true) |

___

### Note to developers

Any JSONS that are named as "info.json" will be renamed "mod.json" by the GUI to remain compatible with the patcher.

To support your mod for the QMods system, you need to learn how `mod.json` is implemented. The critical keys are:  

```
{
  "Id":"energyMod",
  "DisplayName":"Graveyard Keeper Infinite Energy",
  "Author":"Oldark",
  "Version":"1.0.0",
  "Requires":[],
  "Enable":true,
  "AssemblyName":"InfiniteEnergy.dll",
  "EntryMethod":"InfiniteEnergy.MainPatcher.Patch",
  "Config":{},
  "LoadOrder": -1
}
```

`AssemblyName` must be the case sensitive name of the dll file containing your patching method

`EntryMethod` is the entry method for your patch

```cs
using Harmony;

namespace YOURNAMESPACE
{
    class QPatch()
    {
        public static void Patch()
        {
            // Harmony.PatchAll() or equivalent
        }
    }
}
```
