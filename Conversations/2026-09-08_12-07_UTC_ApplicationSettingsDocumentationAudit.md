# Conversation: Audit documentation removed from ApplicationSettings.h

## User

in commit ba1f79bc92d4c42b0942c2959fb4036f5f56bbf5, i removed documentation that shouldn't be in the applicationsettings.h file. ensure that the documentation already exists in the right places, and if not, make the necessary updates.

## Assistant

I’ll review the removed documentation, check where it belongs, and add anything missing.

## Assistant

The preset options, count ranges, wait strategies, and paint limits are already documented in the pipeline, usage, and specification guides. I found one stale reference telling readers to look in the header for those limits. I’ll replace it with links to the guides and clarify why the settings model preserves the full color array until validation.

## Assistant

All removed documentation is covered by the existing guides. Updated [Settings.md](Documentation/Settings.md) to link to the correct sections and clarify color-array validation.

Verified against the validators; `git diff --check` passed. Committed the documentation and conversation log separately.
