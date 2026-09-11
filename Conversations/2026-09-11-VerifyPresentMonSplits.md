# Conversation: Verify PresentMon Splits

## User

check that "C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game\SavedLogOutput\2026-09-11 MinimumInputLatency\2026-09-11_13-59-13\PresentMon.csv" and "C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game\SavedLogOutput\2026-09-11 MinimumInputLatency\2026-09-11_15-18-36\PresentMon.csv" were correctly split into 45 parts

## Assistant

I’ll locate the split files and check that each set has 45 parts whose rows exactly reconstruct the original CSV.

## Assistant

I’m using the Spreadsheets skill’s read-only guidance for the CSV checks.

## Assistant

Both sets contain all 45 parts, and joining their text in order reproduces every original line exactly. Only part 01 contains the column header. I’m checking the encoding difference that prevents a raw byte-for-byte match.

## Assistant

Yes—both files were correctly split into 45 sequential parts, with no missing, duplicated, altered, or reordered rows.

| Session  | Data rows preserved |
| -------- | ------------------- |
| 13-59-13 | 1,692,056           |
| 15-18-36 | 1,699,754           |

Two details:

- Only part 01 contains the column header; parts 02–45 contain data only.
- The original UTF-8 BOM (3 bytes) was removed. Apart from that, concatenating the parts reproduces each original byte-for-byte.
