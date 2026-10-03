# Godot Free And QueueFree

## User

explain the difference between object.free() and node.queue_free() in godot

## Assistant

I’ll check Godot’s documentation for the exact timing and behavior of each method.

## Assistant

**`free()` deletes immediately; `queue_free()` schedules deletion for the end of the current frame.** In C#, these are `Free()` and `QueueFree()`.

- **`Object.free()`**: Immediately destroys the object. Existing references become invalid, so further access fails. A `Node` inherits this method and can use it too. [Object documentation](https://docs.godotengine.org/en/stable/classes/class_object.html#class-object-method-free)
- **`Node.queue_free()`**: Marks a node for deletion after deferred calls have finished at the end of the frame. The node remains accessible until then. Calling it multiple times before deletion is safe. This makes it the usual choice for removing nodes during gameplay. [Node documentation](https://docs.godotengine.org/en/stable/classes/class_node.html#class-node-method-queue-free)

**Both delete a node’s children recursively**, so freeing a scene’s root also frees its descendant nodes. [Node documentation](https://docs.godotengine.org/en/stable/classes/class_node.html#description)

For example, in C#:

```csharp
node.QueueFree();
// The node still exists here, but is scheduled for deletion.
bool pendingDeletion = node.IsQueuedForDeletion(); // true
```

`QueueFree()` does not return from your method or immediately stop the node’s behavior. If the current method should stop, explicitly `return` after calling it. Use `Free()` when you specifically need immediate destruction and know nothing is still using the object.
