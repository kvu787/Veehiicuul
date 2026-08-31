# NVIDIA frame limiting from 1,000 to 5,000 FPS

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> currently, in the nvidia app, i see a max frame rate cap of 1,000. i prefer to use nvidia's frame rate limiter rather than implementing my own, but i want to be able to set a frame rate cap in the 1,000 to 5,000 range. is 1,000 the hard limit for nvidia's frame rate limiter, or just a UI limit? in general, what are my best options if i want frame rate limiting in the 1k to 5k range?

Attached image: [`Attachments/2026-08-30-nvidia-app-1000-fps-limit.png`](Attachments/2026-08-30-nvidia-app-1000-fps-limit.png)

## Assistant

The NVIDIA App's 1,000-FPS ceiling is a UI/product limit, but it is very close to the hard limit of that particular driver-profile limiter. NVIDIA documents the slider as 20-1,000 FPS. In the current public NVAPI header, the underlying `FRL_FPS` setting (`0x10835002`) has `FRL_FPS_MAX = 0x000003ff`, or 1,023 FPS. Therefore:

- 1,000 is not the field's absolute limit.
- 1,023 is the public setting's representable maximum.
- 1,024-5,000 cannot be expressed through NVIDIA App's Max Frame Rate profile setting; no registry or profile-editor UI bypass can enlarge that 10-bit range.

The current stock NVIDIA Profile Inspector list also stops at 1,000. A direct NVAPI DRS write or a custom Profile Inspector value can potentially reach 1,001-1,023, but that tiny extension is not a useful solution for the requested range and should be validated against the installed driver.

For this DirectX 12 application, the best NVIDIA-native option is NVIDIA's separate Reflex/NVAPI sleep-based frame limiter. `NV_SET_SLEEP_MODE_PARAMS::minimumIntervalUs` specifies a minimum frame interval in microseconds, with zero meaning uncapped. NVIDIA explicitly documents this as a frame-rate limiter and recommends calling `NvAPI_D3D_Sleep` once at the start of every frame so the driver waits at the lowest-latency point. Representative settings are:

| Target | `minimumIntervalUs` |
|---:|---:|
| 1,000 FPS | 1,000 us |
| 2,000 FPS | 500 us |
| 4,000 FPS | 250 us |
| 5,000 FPS | 200 us |

For rates such as 3,000 FPS, the integer-microsecond field cannot represent the exact 333.333-us interval; choose 333 or 334 us and measure the resulting rate. Direct NVAPI is the smallest integration when only this limiter is wanted. Streamline's Reflex integration exposes the same concept as `ReflexOptions::frameLimitUs`, but adds more framework machinery.

Turn the NVIDIA App profile cap off when testing the application-requested limit so the 1,000-FPS profile cap cannot remain the effective lower limit. At 200-1,000-us budgets, Windows and driver scheduling are not hard real-time, so verify the distribution of `MsBetweenPresents` with PresentMon rather than checking only average FPS.

Fallback order:

1. Use NVIDIA App at 1,000 FPS if that target is sufficient and zero code changes matter most.
2. Use direct NVAPI/Reflex sleep for NVIDIA-managed limiting across 1,000-5,000 FPS.
3. Add a QPC-deadline hybrid wait/spin limiter for vendor-neutral behavior and as a fallback when NVAPI is unavailable.
4. Treat injected external limiters such as RTSS or Special K as experiments at multi-kilohertz rates; their hook overhead, compatibility, and supported input range need measurement, and they are less attractive when the application is under direct control.

VSync, VRR, and DXGI maximum-frame-latency controls are not substitutes for an arbitrary 1,000-5,000-FPS cap. They synchronize to display/queue behavior rather than impose the requested present interval.

Sources: [NVIDIA Control Panel Max Frame Rate reference](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/3D%20Settings/Manage_3D_Settings_%28reference%29.htm), [NVIDIA's current NVAPI driver-setting definitions](https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h), [NVIDIA's NVAPI sleep-mode API](https://github.com/NVIDIA/nvapi/blob/main/nvapi.h), and [NVIDIA Streamline Reflex guide](https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuideReflex.md).
