# StardewM1Mod

This project is a [Stardew Valley mod](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started) that adds the **"MiniScript M-1 Home Computer"**, as well as programmable **bots**.

This is a computer that connects to the TV in your cabin.  Despite its early-80s appearance, it actually runs a very modern and elegant language, [MiniScript](https://miniscript.org).  (See [Why MiniScript](https://luminaryapps.com/blog/miniscript-why/), if you're curious.)


![Screen shot of the M-1 Home Computer](img/Demo-1.gif)

## How to Play
1. The mod is not yet available in precompiled form.  So you will need to install Visual Studio or MonoDevelop, clone this repo, and Build it yourself.
2. To use the **M-1 Home Computer**:
  - Activate the TV in your house.
  - Select the bottom-most option, *MiniScript M-1 Home Computer*.
  - Type code at the prompt.  See https://miniscript.org for documentation on the language (and in particular, be sure to keep the [Quick Reference](https://miniscript.org/files/MiniScript-QuickRef.pdf) handy).
  4. Press **Esc** to exit.
3. To create and use a **bot**:
  - Make sure you have a free space to your left.
  - Press the **Page Up** key to spawn a bot.  (For now!)
  - Right-click a bot to access its computer console.
  - Type code at the prompt.  This is the same code as on the Home Computer, but allows for some additional commands, like `position`, `left`, `right`, `forward`, `inventory`, `currentToolIndex` (which can be assigned to), and `useTool`.


## Road Map

See [ROADMAP.md](ROADMAP.md) for our development plan, including what features are expected in which future versions of the mod.

