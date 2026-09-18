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
