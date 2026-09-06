# Centralize shared constraints

## User

i don't want constraints for devs/agents to be duplicated in agents.md and readme.md. the readme.md should be the the single source for constraints that apply to both devs and agents. agents.md should therefore a section on constraints that points to relevant section in readme.md. do this change.

## Assistant (commentary)

I’ll consolidate the shared constraints in README.md and have AGENTS.md point to that section. I’ll also record this conversation as required by the repository instructions.

## Assistant (commentary)

The platform and frame rate policies are duplicated. I’ll place them under a shared “Constraints” section in README.md, along with the Git, Markdown, compatibility, and launcher requirements currently in AGENTS.md. AGENTS.md will retain the agent-specific guidance and link to the shared constraints.

## Assistant

[README.md](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/README.md:33) now owns the shared constraints. [AGENTS.md](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/AGENTS.md:3) links to that section and retains agent-specific guidance.

Verified with `git diff --check`. Documentation and conversation log committed separately.
