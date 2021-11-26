# StardewM1Mod

This project is a [Stardew Valley mod](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started) that adds the **"MiniScript M-1 Home Computer"**.

This is a computer that connects to the TV in your cabin.  Despite its early-80s appearance, it actually runs a very modern and elegant language, [MiniScript](https://miniscript.org).  (See [Why MiniScript](https://luminaryapps.com/blog/miniscript-why/), if you're curious.)


![Screen shot of the M-1 Home Computer](img/Demo-1.gif)


## Near-Term To-Do List
- add a /usr disk for user files
	- under save folder; see https://stardewvalleywiki.com/Saves#Find_your_save_files
- add basic bot movement: `forward`, `turn`
- add basic bot sensing: `position`, `facing`, `itemAt`, `itemAhead`
- add inventory APIs: `inventory`, `select`
- add `useTool` API
- add a /sys disk with a startup script and demo directory; add `dir`, `load`, and `run`
- implement the `text` module (`text.row`, `text.color`, etc.)
- implement the `key` module (at least `key.available` and `key.get`)
- create a code editor (using the text display, perhaps modeled after [nano](https://www.nano-editor.org/)); add `edit`, `source`, `save`


## Feature Ideas

- Display object: can display color/message in the world; tileable to make any size Jumbotron
- M-1 Home Computer (accessed via your TV)
  - 40x20 text display
  - can change screen color, border color, text color, and text background
  - PixelDisplay for drawing?
  - APIs to get information about what's in the world (crops, rocks, trees, NPCs, etc.)
  - file system for saving files
    - /usr for files local to  just the M-1
    - /net for files shared by all computers/bots in the game
  - network messaging system for communicating with bots
- Bots
  - craftable (out of what?)
  - able to move wherever a farmer can walk
  - able to use any tool they are equipped with
    - farming, mining, chopping, planting seeds, watering, etc.
  - inventory (12 spaces)
  - built-in computer terminal
    - same screen size & capabilities as the M-1 Home Computer
      - screen color is visible in world view
    - additional APIs to interact with the world:
      - pickUp
      - drop
      - select (tool)
      - use (tool, or whatever's selected)
      - turn N/E/S/W
      - forward/backup (number of tiles)
      - status light (settable; automatically turns red if error)
  - certain kinds of interaction with vendor NPCs:
  	- buy/sell seeds and other goods at Pierre's
  	- buy/sell goods, upgrade tools, and crack geodes at Clint's
    - buy/sell goods from Willy at the Fish Shop
  - can craft things (same as the player)
  - can attach/remove bait on fishing rod
  - speak to the player: "!" icon appears above the bot, and when left-clicked, starts a programmable dialogue (like speaking to an NPC)
  