using System.Collections;
using NUnit.Framework;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Random = Unity.Mathematics.Random;

namespace PlayModeTests
{
    [SampleDefinitionContainer]
    public static class Definitions
    {
        public static ISampleDefinition PhysicsUpdate
            = new DefaultSampleDefinition(nameof(PhysicsUpdate), "Samples.GameObject", SampleUnit.Millisecond);
    }

    internal static class Utils
    {
        private static SimulationMode originalSimulationMode;
        private static bool originalAutoSyncTransforms;

        public static void Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scripts/PlayModeTests/TestEnvironment.unity", new LoadSceneParameters()
            {
                loadSceneMode = LoadSceneMode.Single,
                localPhysicsMode = LocalPhysicsMode.Physics3D
            });
            originalSimulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            originalAutoSyncTransforms = Physics.autoSyncTransforms;
            Physics.autoSyncTransforms = false;
        }

        public static void TearDown()
        {
            Physics.simulationMode = originalSimulationMode;
            Physics.autoSyncTransforms = originalAutoSyncTransforms;
        }

        public static GameObject CreateColliderObject(Vector3 position, PrimitiveType primitiveType)
        {
            var gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.transform.position = position;
            gameObject.AddComponent<Rigidbody>();

            return gameObject;
        }

        public static IEnumerator TestRoutine(int objectCount, PrimitiveType primitiveType)
        {
            using var sampleGroups = new SampleGroups();
            var random = new Random(seed: 1);
            for (int i = 0; i < objectCount; i++)
            {
                float2 xz = random.NextFloat2(new float2(-5, -5), new float2(5, 5));
                Vector3 pos = new Vector3(xz.x, random.NextFloat(8, 10), xz.y);
                Utils.CreateColliderObject(pos, primitiveType);
            }

            const float dt = 1f / 60f;
            var physicsScene = SceneManager.GetActiveScene().GetPhysicsScene();
            for (int i = 0; i < 60 * 10; ++i)
            {
                using (sampleGroups.CreateScope(Definitions.PhysicsUpdate))
                {
                    physicsScene.Simulate(dt);
                }

                yield return null;
            }
        }
    }

    public class GameObjectTest
    {
        [SetUp] public void Setup() => Utils.Setup();

        [TearDown] public void TearDown() => Utils.TearDown();

        [UnityTest, Performance] public IEnumerator TestObjectCount([Values(1, 100, 300, 500, 1000, 1500, 2000)] int objectCount)
        {
            yield return Utils.TestRoutine(objectCount, PrimitiveType.Sphere);
        }
    }

    [ComparableTest]
    public class PrimitiveTypeTest
    {
        [SetUp] public void Setup() => Utils.Setup();

        [TearDown] public void TearDown() => Utils.TearDown();

        private const int objectCount = 1000;

        [UnityTest, Performance] public IEnumerator Sphere()
        {
            yield return Utils.TestRoutine(objectCount, PrimitiveType.Sphere);
        }

        [UnityTest, Performance] public IEnumerator Capsule()
        {
            yield return Utils.TestRoutine(objectCount, PrimitiveType.Capsule);
        }

        [UnityTest, Performance] public IEnumerator Cylinder()
        {
            yield return Utils.TestRoutine(objectCount, PrimitiveType.Cylinder);
        }

        [UnityTest, Performance] public IEnumerator Cube()
        {
            yield return Utils.TestRoutine(objectCount, PrimitiveType.Cube);
        }
    }
}