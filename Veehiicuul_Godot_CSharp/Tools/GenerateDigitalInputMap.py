"""Write a DigitalInputMap C# class to stdout using Python 3.14.x."""

import sys


def main() -> None:
    codes = sys.argv[1:]
    if not codes:
        raise ValueError("Supply at least one Godot input code as a command-line argument.")

    properties = []
    registrations = []
    cleanup = []
    for code in codes:
        if code.startswith("JoyButton."):
            event = f"InputEventJoypadButton binding = new() {{ ButtonIndex = {code}, Device = 0 }}"
        elif code.startswith("Key."):
            event = f"InputEventKey binding = new() {{ Keycode = {code} }}"
        elif code.startswith("MouseButton."):
            event = f"InputEventMouseButton binding = new() {{ ButtonIndex = {code} }}"
        else:
            raise ValueError(f"Unsupported digital input code: {code}")

        name = code.replace(".", "")
        properties.append(f'    public StringName {name} {{ get; }} = new("{name}");')
        registrations.append(
            f"        InputMap.AddAction(this.{name});\n"
            f"        using ({event}) {{\n"
            f"            InputMap.ActionAddEvent(this.{name}, binding);\n"
            "        }"
        )
        cleanup.append(
            f"        InputMap.EraseAction(this.{name});\n"
            f"        this.{name}.Dispose();"
        )

    print("\n".join([
        "using Godot;",
        "using System;",
        "",
        "namespace Veehiicuul_Godot_CSharp;",
        "",
        "public sealed class DigitalInputMap : IDisposable {",
        "\n".join(properties),
        "",
        "    private bool IsDisposed;",
        "",
        "    public DigitalInputMap() {",
        "\n".join(registrations),
        "    }",
        "",
        "    public void Dispose() {",
        "        if (this.IsDisposed) {",
        "            return;",
        "        }",
        "",
        "\n".join(cleanup),
        "        this.IsDisposed = true;",
        "    }",
        "}",
    ]))


if __name__ == "__main__":
    main()
