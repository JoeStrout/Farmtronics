# StardewM1Mod

This project is a [Stardew Valley mod](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started) that adds the **"MiniScript M-1 Home Computer"**.

This is a computer that connects to the TV in your cabin.  Despite its early-80s appearance, it actually runs a very modern and elegant language, [MiniScript](https://miniscript.org).  (See [Why MiniScript](https://luminaryapps.com/blog/miniscript-why/), if you're curious.)


![Screen shot of the M-1 Home Computer](img/Demo-1.gif)


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
  - inventory (9 spaces)
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

  