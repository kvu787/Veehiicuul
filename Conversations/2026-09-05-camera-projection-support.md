# Camera projection support

## User

does this app have logic to handle both multiple projection types, such as perspective and orthographic? 

## Assistant

I’ll check how the app builds and selects its camera projections.

## Assistant final response

No. The app currently supports **only a fixed orthographic projection**.

- [Renderer.cpp](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/src/Renderer.cpp:1244) always creates an orthographic matrix. There is no perspective mode or projection switch.
- [SimplePaint.hlsl](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/shaders/SimplePaint.hlsl:4) also assumes an orthographic camera when calculating the paint appearance.

Adding perspective support would require a projection choice and adjusting the paint shader’s view-direction calculations.
