# Conversation: Godot C# platforms

## User

what platforms can a godot 4.7.2 .net c# game run on

## Assistant

I’ll check Godot’s documentation for C# export support and the version you named.

## Assistant

A **Godot 4.7.2 .NET/C# game** can target:

| Platform        | Support                                                     |
| --------------- | ----------------------------------------------------------- |
| Windows         | Supported                                                   |
| Linux           | Supported                                                   |
| macOS           | Supported                                                   |
| Android         | Supported, but C# support is still experimental              |
| iOS / iPadOS    | Supported, but experimental; exporting requires a Mac        |
| Web browsers    | **No official C# export support**                           |

These are the platforms listed in [Godot 4.7’s C# documentation](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/index.html#c-platform-support).

**Consoles (PlayStation, Xbox, Nintendo Switch)** require a separate port through a third-party provider or your own licensed integration. C# support depends on the provider; they are not built-in export targets. See [Godot’s console support](https://godotengine.org/consoles/).

## User

how do i get godot 4.7.2 gdscript game running on consoles

## Assistant

I’ll check the current console porting options and what they require for a Godot 4.7.2 GDScript project.

## Assistant

**You can keep your game in GDScript.** To run it on consoles, you need an approved console developer account and a console-specific Godot port/export toolchain. The standard Godot download does not include console exports. [Godot’s console guide](https://godotengine.org/consoles/)

**Check 4.7.2 compatibility first:** W4’s current FAQ explicitly lists Godot **4.6, 4.5 and 4.4**, despite broader wording elsewhere on its page. I could not verify a ready-made **4.7.2** port. Ask the provider whether it supports your exact version, renderer and plugins before purchasing or changing your project. [W4 Consoles](https://www.w4games.com/w4consoles)

Your practical options are:

| Route                       | What you do                                                                                         |
| --------------------------- | --------------------------------------------------------------------------------------------------- |
| **W4 Consoles**              | License its console tools and do the port yourself. Supports PS5, Xbox Series X/S and Switch; Switch 2 is advertised as beta. |
| **RAWRLAB’s Nintendo port**  | Use its GDScript-only Switch/Switch 2 port. Requires Nintendo approval and “Godot Ecosystem Contributor” eligibility for no-cost access. No GDExtension support or free support. |
| **Hire a porting studio**    | Pay a provider to adapt and test your game. Godot lists providers including Pineapple Works and other studios. |

Sources: [W4](https://www.w4games.com/w4consoles), [RAWRLAB](https://www.rawrlab.com/godot_nintendo_switch_free_port.html), [Godot’s provider directory](https://godotengine.org/consoles/).

For the self-porting route, the process is:

1. **Choose a console and apply for access:** [Nintendo Developer Portal](https://developer.nintendo.com/), [PlayStation Partners](https://partners.playstation.net/), or [ID@Xbox](https://developer.microsoft.com/en-us/games/).
2. **Get platform approval, SDK access and development hardware.** Registering an account alone does not complete this process.
3. **Obtain a compatible Godot console port.** Follow its private instructions to install/build the engine and export templates.
4. **Export and deploy to the development hardware.** Adapt your game to the console’s platform requirements and test it there.
5. **For a public release**, complete the platform’s submission/certification process, age ratings and store setup. Getting a build running is an earlier milestone. [Godot’s process overview](https://godotengine.org/consoles/)

My suggested first step is to **choose one target console and confirm 4.7.2 support with a provider**. You generally keep your GDScript gameplay code; the work is in the console toolchain, platform integration, performance and release requirements.
