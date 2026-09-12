# Conversation: Rename the project to Veehiicuul

## User

This project should now be known as "Veehiicuul". Do a complete repo-wide rename except for the conversation logs. Don't rename the repo folder or github repo; I will rename those myself. all renaming must be constrained to things inside of this folder.

## Assistant

I’ll check the project names and repository constraints, then rename references inside this folder to “Veehiicuul,” leaving existing conversation logs and the repository folder name unchanged.

## Assistant

The old name appears in the build target, executable, window title, logging environment variable, tests, documentation, and saved PresentMon captures. I’ll include those in the rename and update the saved-file fingerprints so the capture verification remains consistent.

## Assistant

Veehiicuul builds successfully, and all 13 Release tests pass, including hardware, WARP, and window lifecycle checks. I also made the current documentation’s folder examples independent of the checkout name so they’ll work after you rename the folder.

## Assistant

All 13 Debug tests pass as well. The saved captures now use Veehiicuul, with a note documenting the relabeling. I’m checking the capture changes against Git, then I’ll commit the application rename and archive updates separately.

## Assistant

`Run.cmd` launches Veehiicuul, displays the renamed window title, and writes its session logs correctly. All 3,391,810 capture rows retain their original measurement data; only the application-name field changed. The final name scan found no old project names outside the excluded conversations and ignored local build/cache files.

## Assistant

Renamed and committed the tracked repository content to **Veehiicuul**, including saved reports and capture labels.

All 13 tests pass in both Release and Debug, and `Run.cmd` launches successfully. Existing conversation logs, the repository folder, and the GitHub repository remain unchanged.
