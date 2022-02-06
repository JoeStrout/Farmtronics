# Farmtronics Development Road Map

This document lays out the plan for when various features will be added to the mod, starting with the first official release (version 1.0).

All plans are subject to change, of course.  But we'll try to update this document when that happens.

## Version 1.0

This is our "minimum viable product".

### Computer Features
- ☑ 40x20 text display
- ☑ full [text](https://miniscript.org/wiki/TextDisplay) functionality: color, back color, inverse, ability to get/set cursor and line delimiter
- ☑ `print` and `input`
- ☑ [`key` module](https://miniscript.org/wiki/Key) with at least `key.available`, `key.get`, and `key.clear`
- ☑ file system (including `cd`, `dir`, etc.), with /sys and /usr disks
- ☑ [`import`](https://miniscript.org/wiki/Import)
- ☑ code editor, so you can write/edit/save your own programs on /usr
- ☑ `Location` class for getting info about a location (map)
- ☑ `farm` and `here` accessors for getting a Location describing your farm, or the location of the computer
- ☑ built-in help and some demo/utility programs on /sys
- ☐ no errors spewed to SMAPI under any conditions

### Bot Features
- ☑ bots can be saved and reloaded with the game
- ☑ initial bot received in mail after completing `toDo` tasks on home computer
- ☑ additional bots can be purchased at Pierre's
- ☑ inventory that can hold any items the farmer can hold
- ☑ player can move items between their own inventory and the bot's
- ☑ bot can move, turn, and update its screen and status light color
- ☑ bot can select and use any basic tool: axe, hoe, pickaxe, scythe, watering can, seeds
- ☑ bot energy starts full (270) every day, and is depleted as it moves and use tools
- ☑ bots can be saved & restored with the game, even in player inventory or chests

### Support & Documentation
- ☑ mod page at https://www.nexusmods.com/stardewvalley/mods/
- ☑ basic documentation on how to install and use the mod
- ☑ API reference for all classes/functions added to MiniScript

## Known Issues

These will be addressed ASAP.

- When a bot tries to break something its tools aren't strong enough for (e.g. a large stump with the default axe), it causes a dialog to briefly appear and the player farmer to jitter in place, even if they are nowhere near the bot.

## Coming Soon

- Bots no longer lose their inventory when picked up.
- You can get or set the name of a bot as `bot.name`, and these names persist across saves and pick-up/set-down.
- Tile information (e.g. from `bot.ahead` or `Location.tile` now includes characters (players and NPCs).

## Unscheduled Future Version

The following features are definitely things we want to include, but they have not yet been scheduled for a particular version.

- multiplayer support
- improved copy/paste support in the editor
- console autocompletion (as in [https://miniscript.org/MiniMicro](MiniMicro)
- `mouse` module (allowing for point-and-click UI)
- bots can be recharged (energy refilled), perhaps via some craftable charging station
- bots can use weapons to fight monsters
- bots and Home Computer can communicate via networking
- bots can pick up and drop items
- allow a bot to stay on top of a crop plant overnight without squashing (destroying) it

## Features of Uncertain Fate

The features below are under consideration, but may not make it if they unbalance the game, are impractical to implement, etc.

- bots can craft things (including other bots)
- bots can buy/sell goods at shops
- bots can upgrade tools and crack geodes at Clint's
- Home Computer can be used for emailing NPCs
- Home Computer can be used for online mail-order shopping
- Display object: can display color/message in the world; tileable to make any size Jumbotron
- PixelDisplay for drawing graphics
- bots can fish (including handling bait)
- bots can speak to the player: "!" icon appears above the bot, and when left-clicked, starts a programmable dialogue (like speaking to an NPC)
  
