# Runtime source map

Every runtime C# file from ZoomTracks/Assets/Scripts is mapped below.
Source snapshot: b58ac18a28ee3f72712cdaf79d6d4ac9752e29d5.

Count: 40 source files. Paths on the right are relative to this Godot application.

| Unity source                                                     | Godot counterpart                                                       |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------- |
| DataStructures/TransformStruct.cs                                | Source/DataStructures/TransformStruct.cs                                |
| Extensions/EnumExtensions.cs                                     | Source/Extensions/EnumExtensions.cs                                     |
| Extensions/FloatExtensions.cs                                    | Source/Extensions/FloatExtensions.cs                                    |
| Extensions/IntExtensions.cs                                      | Source/Extensions/IntExtensions.cs                                      |
| Extensions/TransformExtensions.cs                                | Source/Extensions/TransformExtensions.cs                                |
| Extensions/Vector3Extensions.cs                                  | Source/Extensions/Vector3Extensions.cs                                  |
| GameDataAndLogic/Camera/CameraController.cs                      | Source/GameDataAndLogic/Camera/CameraController.cs                      |
| GameDataAndLogic/Camera/CameraFollowSettings.cs                  | Source/GameDataAndLogic/Camera/CameraFollowSettings.cs                  |
| GameDataAndLogic/Camera/CameraPivotManager.cs                    | Source/GameDataAndLogic/Camera/CameraPivotManager.cs                    |
| GameDataAndLogic/Car/Car.cs                                      | Source/GameDataAndLogic/Car/Car.cs                                      |
| GameDataAndLogic/Car/CarAccelerationMap.cs                       | Source/GameDataAndLogic/Car/CarAccelerationMap.cs                       |
| GameDataAndLogic/Car/CarDynamic.cs                               | Source/GameDataAndLogic/Car/CarDynamic.cs                               |
| GameDataAndLogic/Car/CarState.cs                                 | Source/GameDataAndLogic/Car/CarState.cs                                 |
| GameDataAndLogic/Car/CarSwitcher.cs                              | Source/GameDataAndLogic/Car/CarSwitcher.cs                              |
| GameDataAndLogic/CollisionDetection/ColliderJson.cs              | Source/GameDataAndLogic/CollisionDetection/ColliderJson.cs              |
| GameDataAndLogic/CollisionDetection/CoordinateXY.cs              | Source/GameDataAndLogic/CollisionDetection/CoordinateXY.cs              |
| GameDataAndLogic/CollisionDetection/Guard.cs                     | Source/GameDataAndLogic/CollisionDetection/Guard.cs                     |
| GameDataAndLogic/CollisionDetection/Outline.cs                   | Source/GameDataAndLogic/CollisionDetection/Outline.cs                   |
| GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs    | Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs    |
| GameDataAndLogic/CollisionDetection/VehicleCollisionFootprint.cs | Source/GameDataAndLogic/CollisionDetection/VehicleCollisionFootprint.cs |
| GameDataAndLogic/CollisionManager.cs                             | Source/GameDataAndLogic/CollisionManager.cs                             |
| GameDataAndLogic/CollisionManager2.cs                            | Source/GameDataAndLogic/CollisionManager2.cs                            |
| GameDataAndLogic/GraphicsSettingsManager.cs                      | Source/GameDataAndLogic/GraphicsSettingsManager.cs                      |
| GameDataAndLogic/InputManager.cs                                 | Source/GameDataAndLogic/InputManager.cs                                 |
| GameDataAndLogic/StutterLogger.cs                                | Source/GameDataAndLogic/StutterLogger.cs                                |
| GameDataAndLogic/TimeManager.cs                                  | Source/GameDataAndLogic/TimeManager.cs                                  |
| GameDataAndLogic/TrackJson.cs                                    | Source/GameDataAndLogic/TrackJson.cs                                    |
| GameDataAndLogic/TrackObjects.cs                                 | Source/GameDataAndLogic/TrackObjects.cs                                 |
| GameDataAndLogic/TrackSwitcher.cs                                | Source/GameDataAndLogic/TrackSwitcher.cs                                |
| GameDataAndLogic/UiManager.cs                                    | Source/GameDataAndLogic/UiManager.cs                                    |
| Main.cs                                                          | Main.cs                                                                 |
| UnityEngineModification/QuitOnException.cs                       | Main.cs callback catch blocks                                           |
| Utility/AwaitableUtility.cs                                      | Source/Utility/SceneLoadingUtility.cs                                   |
| Utility/DateTimeUtility.cs                                       | Source/Utility/DateTimeUtility.cs                                       |
| Utility/GarbageCollectionUtility.cs                              | Source/Utility/GarbageCollectionUtility.cs                              |
| Utility/InputUtility.cs                                          | Source/Utility/InputUtility.cs                                          |
| Utility/JsonUtility.cs                                           | Source/Utility/JsonUtility.cs                                           |
| Utility/ParseUtility.cs                                          | Source/Utility/ParseUtility.cs                                          |
| Utility/PrintInfoUtility.cs                                      | Source/Utility/PrintInfoUtility.cs                                      |
| Utility/QuaternionUtility.cs                                     | Source/Utility/QuaternionUtility.cs                                     |

Unity editor tools under Assets/Editor and the external collision test executable are
not game runtime code. The existing collision test executable was adapted separately
under Verification/CollisionDetection. All ported files compile into the app; no
Unity types, compatibility shims, excluded source copies, or NotImplementedException
placeholders are used.

SessionLog is an additional logging helper. AwaitableUtility becomes a synchronous
scene loader so it cannot introduce a second frame loop. The Unity QuitOnException hook is replaced
by simple catch blocks in Main.cs. QuaternionUtility also corrects the original class
spelling QuaterionUtility. The pre-existing DebugInfo.cs is retained and not called.
