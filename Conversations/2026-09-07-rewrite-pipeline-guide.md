# Conversation: Rewrite the pipeline configuration guide

## User

we need to rewrite C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game\Documentation\Pipeline.md

these are the most important sections. they must appear first in this order:

1. Explanation of each pipeline mode preset
2. Explanation of how `Mode = Custom` works.
3. Detailed explanation of each individual setting: 

VSync
MaxGpuFramesInFlight 
MaxPresentLatency 
WaitForPresentation 
BackBufferCount 
AllowTearing 
WaitStrategy

the rest of the file contents is up to your discretion.

## Assistant

I’ll check the current settings and implementation, then rewrite `Pipeline.md` in your requested order.

## Assistant

The code treats GPU frames, presentation latency, and back buffers as separate controls. I’ll explain what each limits and how they interact, including why disabling presentation waits leaves resource waits in place and how `AllowTearing` behaves with VSync off.

## Assistant

I’ve confirmed the preset values and Custom validation rules. The rewrite will put presets first, Custom second, and the seven settings third in your exact order, with accepted values, tradeoffs, and interactions for each.

## Assistant

The rewrite is in place, and the existing configuration tests pass. I’ve checked the explanations against the code and Microsoft’s DXGI documentation. I’m finishing the Markdown checks, then committing the guide and conversation log separately as the repository requires.

## Assistant

Rewrote [Pipeline.md](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Documentation/Pipeline.md) in your requested order, with detailed explanations, accepted values, tradeoffs, and interactions for all seven settings.

Configuration tests and Markdown checks passed. Documentation and conversation were committed separately.
