# Conversation: Disable Godot Physics

## User

for C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp, if i am not using any of godot's provided physics engines at all, is there a way to fully turn off godot's physics to improve performance, or does godot's physics already do nothing?

## Assistant

I’ll check the project and Godot’s physics behavior to see what still runs when you aren’t using its physics engines.

## Assistant

Your project currently selects Jolt for 3D physics. Godot 4.7.2 also provides a “Dummy” option that disables physics functionality. I’m checking what overhead remains, including whether the fixed update loop still runs.

## Assistant

**Yes. For Godot 4.7.2, set both physics engines to `Dummy`.** Godot documents this as disabling their physics functionality. [Godot documentation](https://docs.godotengine.org/en/4.7/classes/class_projectsettings.html#class-projectsettings-property-physics-3d-physics-engine)

Your [project.godot](/C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/project.godot:28) currently selects `Jolt Physics`. To disable both engines, replace its existing `[physics]` section with:

```ini
[physics]

2d/physics_engine="Dummy"
3d/physics_engine="Dummy"
```

You can also select **Dummy** under **Project Settings → Physics → 2D / 3D → Physics Engine**. Restart the editor/project afterward.

There are three relevant distinctions:

- **Unused physics engines still have some overhead.** They are initialized and receive synchronization/update calls; active physics spaces can require bookkeeping even without bodies. They do not automatically become entirely inactive. I verified this in your local [Godot 4.7.2 source](/C:/Users/k/Repository/External/Godot_4-7-2/modules/jolt_physics/jolt_physics_server_3d.cpp:1605).
- **Dummy removes the actual physics work.** Its simulation, synchronization and query-flushing methods are empty. Collision detection, raycasts and other features relying on those servers consequently stop working. [Dummy implementation](/C:/Users/k/Repository/External/Godot_4-7-2/servers/physics_3d/physics_server_3d_dummy.h:430)
- **The fixed update loop still runs.** Your C# `_PhysicsProcess(double delta)` callbacks can still run, so you can use them for your own simulation. Dummy does not remove all work reported under “physics” in the profiler. [Main loop implementation](/C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:4970)

**For your intended setup, Dummy is the appropriate choice.** With no physics objects, I would expect a small saving rather than a dramatic improvement; I haven’t benchmarked the difference.
