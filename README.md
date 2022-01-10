# Farmtronics

This project is a [Stardew Valley mod](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started) that adds the **"Farmtronics Home Computer"**, as well as programmable **Farmtronics Bots**.

The Home Computer is a computer that connects to the TV in your cabin.  Despite its early-80s appearance, it actually runs a very modern and elegant language, [MiniScript](https://miniscript.org).  (See [Why MiniScript](https://luminaryapps.com/blog/miniscript-why/), if you're curious.)

![Screen shot of the Farmtronics Home Computer](img/Demo-1.gif)

Bots each carry the same computer, but also have the ability to move around in the world and get things done.  All you have to do is program them!

## How to Play
1. Download the mod zip file from the [Releases](https://github.com/JoeStrout/Farmtronics/releases) page, and install it in the [usual way](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started#Find_your_game_folder).
2. To use the **Farmtronics Home Computer**:
  - Activate the TV in your house.
  - Select the bottom-most option, *Farmtronics Home Computer*.
  - Type code at the prompt.  See https://miniscript.org for documentation on the language (and in particular, be sure to keep the [Quick Reference](https://miniscript.org/files/MiniScript-QuickRef.pdf) handy).
  - Also be sure to try the `help` command, and read through the various topics there.
  4. Press **Esc** to exit.
3. To obtain and use a **bot**:
  - On the Home Computer, use the `toDo` command to see the tasks you still need to complete.  Complete them.
  - Check your mail the next day (after completing all tasks).  You should have a letter with a Bot included.  (Once you have read this letter, you can purchase additional bots at Pierre's shop.)
  - Place the bot down in any empty spot on the map.
  - Right-click a bot to access its computer console.
  - Type code at the prompt.  This is the same code as on the Home Computer, but allows for some additional commands, like `bot.position`, `bot.left`, `bot.right`, `bot.forward`, `bot.inventory`, `bot.currentToolIndex` (which can be assigned to), and `bot.useTool`.

See the [Wiki](https://github.com/JoeStrout/Farmtronics/wiki) for more documentation and sample code.

## Questions? Issues? Things to share?

Best place to discuss this mod is on the [MiniScript Discord](https://discord.gg/7s6zajx).  There is a #farmtronics channel there.

## Road Map

See [ROADMAP.md](ROADMAP.md) for our development plan, including what features are expected in which future versions of the mod.

