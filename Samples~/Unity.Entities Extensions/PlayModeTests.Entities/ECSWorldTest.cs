using System;
using System.Collections;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.PerformanceTestReportViewer.Extensions;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace PerformanceTestReportViewer.Samples_.PlayModeTests.Entities
{
    public struct RunTest : IComponentData
    {
        public uint seed;
        public int state;
    }

    public struct TestComponent : IComponentData
    {
        public int intValue;
        public float floatValue;
        public double doubleValue;
    }

    public partial struct TestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunTest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref RunTest runTest = ref SystemAPI.GetSingletonRW<RunTest>().ValueRW;
            int stateValue = runTest.state;
            var random = new Unity.Mathematics.Random(runTest.seed);
            foreach (TestComponent testComponent in SystemAPI.Query<TestComponent>())
            {
                unchecked
                {
                    stateValue = (int)((stateValue + testComponent.intValue + testComponent.floatValue) * testComponent.doubleValue);
                }
            }

            runTest.state = stateValue;
            runTest.seed = random.state;
        }
    }

    public class ECSWorldTest
    {
        [Test, Performance, UnityTest]
        public IEnumerator Test([Values(100, 1000, 10000, 50000, 100000, 500000, 1000000)] int entityCount)
        {
            using var testWorld = DefaultWorldInitialization.Initialize("TestWorld", editorWorld: false);
            using WorldProxyManagerTestProxy testProxy = new(testWorld);
            using WorldSampler worldSampler = new();

            int systemCount = testProxy.GetAllSystemCount(testWorld);
            ExposedSystemFrameData[] frameDatas = new ExposedSystemFrameData[systemCount];

            var archeType = testWorld.EntityManager.CreateArchetype(typeof(TestComponent));
            testWorld.EntityManager.CreateEntity(archeType, entityCount: entityCount);
            {
                using var testComponentQuery = testWorld.EntityManager.CreateEntityQuery(typeof(TestComponent));
                using var entities = testComponentQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; ++i)
                {
                    testWorld.EntityManager.SetComponentData(entities[i], new TestComponent()
                    {
                        intValue = i,
                        floatValue = i,
                        doubleValue = i
                    });
                }
            }

            testWorld.EntityManager.CreateSingleton(new RunTest()
            {
                state = 0,
                seed = 1
            });

            for (int i = 0; i < 60 * 10; ++i)
            {
                yield return null;
                testProxy.Update();

                testProxy.GetFrameData(testWorld, frameDatas);

                worldSampler.RecordFrameData(i, frameDatas, sampleTarget: null);
            }
        }
    }
}