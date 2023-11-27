using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Entities.Editor;

public class WorldProxyManagerTestProxy : IDisposable
{
    private readonly List<WorldProxy> proxies = new();
    private readonly Dictionary<WorldProxy, LocalWorldProxyUpdater> proxyUpdaters = new();
    public WorldProxyManagerTestProxy(params World[] worlds)
    {
        foreach (World world in worlds)
        {
            var proxy = new WorldProxy(world.SequenceNumber);
            proxies.Add(proxy);
            var updater = new LocalWorldProxyUpdater(world, proxy);
            proxyUpdaters[proxy] = updater;

            updater.PopulateWorldProxy();
        }
    }

    public void Dispose()
    {
        proxies.Clear();
        proxyUpdaters.Clear();
    }

    public void Update()
    {
        foreach (var (worldProxy, updater) in proxyUpdaters)
        {
            updater.UpdateFrameData();
        }
    }

    public int GetAllSystemCount(World world)
    {
        var proxy = proxies.Find(p => p.SequenceNumber == world.SequenceNumber);
        if (proxy == null)
            throw new Exception($"Proxy not found : {world}");

        return proxy.m_AllSystems.Count;
    }

    public void GetFrameData(World world, in Span<ExposedSystemFrameData> output)
    {
        var proxy = proxies.Find(p => p.SequenceNumber == world.SequenceNumber);
        if (proxy == null)
            throw new Exception($"Proxy not found : {world}");

        for (int i = 0; i < proxy.m_AllSystems.Count; ++i)
        {
            SystemProxy systemProxy = proxy.m_AllSystems[i];
            output[i] = new ExposedSystemFrameData()
            {
                SystemName = systemProxy.TypeName,
                EntityCount = systemProxy.TotalEntityMatches,
                LastFrameRuntimeMilliseconds = systemProxy.RunTimeMillisecondsForDisplay
            };
        }
    }
}

public struct ExposedSystemFrameData
{
    public string SystemName;
    public int EntityCount;
    public float LastFrameRuntimeMilliseconds;
}

public static class Extensions
{
    
}