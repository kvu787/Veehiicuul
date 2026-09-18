# Conversation: Cross-platform line endings

## User

should i use crlf or lf line-endings assuming that i'll be working on linux, macos, and modern windows 11 systems?

## Assistant

I’ll check the Git guidance and this repository’s settings so the recommendation fits your setup.

## Assistant

**Use LF as your default across Linux, macOS, and Windows 11.**

LF is the native convention on Linux and macOS, and modern Windows development tools support it. Even Windows Notepad supports LF. Using one convention also avoids unnecessary changes when switching systems. [Microsoft’s Notepad announcement](https://devblogs.microsoft.com/commandline/extended-eol-in-notepad/)

My recommendation:

- **Source code, Markdown, JSON, configuration files, PowerShell scripts, and shell scripts:** LF.
- **Windows batch files (`.bat` and `.cmd`):** keep CRLF for reliable `cmd.exe` behavior. Microsoft uses this exception in its own repositories. [Example](https://github.com/microsoft/tsdoc/blob/main/.gitattributes)
- **Files whose format or consuming tool specifically requires CRLF:** follow that requirement.

For Git, put these rules at the top of your repository’s `.gitattributes`:

```gitattributes
* text=auto eol=lf
*.bat text eol=crlf
*.cmd text eol=crlf
```

This gives automatically detected text files LF on checkout across all three systems, with CRLF for batch files. Explicit repository rules avoid depending on each contributor’s `core.autocrlf` setting. [Git documentation](https://git-scm.com/docs/gitattributes)

Your existing [.gitattributes](C:/Users/k/Repository/Veehiicuul/.gitattributes) contains Git LFS rules for CSV and Blender files; **keep those rules below these defaults**. I haven’t changed the settings.
