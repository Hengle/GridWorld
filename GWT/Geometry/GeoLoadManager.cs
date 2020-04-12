﻿using GridWorld.Test.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Urho;

namespace GridWorld.Test.Geometry
{
    public static class GeoLoadManager
    {
        public delegate void GeoLoadCallback(Cluster theCluster, object tag);

        public static int ForceLoadRadius = 4;

        private static bool UseThreads = true;

        public static void GenerateGeometry(Cluster theCluster, object tag, GeoLoadCallback callback)
        {
            if (UseThreads)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
                {
                    ClusterGeometry.BuildGeometry(theCluster);
                    callback?.Invoke(theCluster, tag);
                }));
            }
            else
            {
                ClusterGeometry.BuildGeometry(theCluster);
                callback?.Invoke(theCluster, tag);
            }
        }

        public delegate void CuslterPosCallback(int h, int v);
        public static event CuslterPosCallback NeedCluster = null;

        public static float ClusterZombieTime = 10;

        private static void ForceClusterLoad(ClusterPos origin, int h, int v)
        {
            var cluster = World.ClusterFromPosition(origin.Offset(h, v));
            if (cluster == null)
            {
                // it's off the current map, flag it for generation, another cycle will pick it up
                NeedCluster?.Invoke(h, v);
                return;
            }

            cluster.AliveCount = ClusterZombieTime;

            var status = cluster.GetStatus();
            if (status == Cluster.Statuses.Raw || status == Cluster.Statuses.GeometryBound || status == Cluster.Statuses.GeometryPending)
                return; // it's good to go, or is waiting on generation

            if (status == Cluster.Statuses.GeometryCreated)
            {
                // the geo was created, it just isn't bound, tell the Urho component to put a load in the pipe
                cluster.RequestBinding();
                LoadLimiter.AddLoadPriority(cluster.Origin);
                return;
            }

            GeoLoadManager.GenerateGeometry(cluster, null, (c, t) => { LoadLimiter.AddLoadPriority(cluster.Origin); });
        }

        private static void ForceRingLoad(int radius, ClusterPos origin)
        {
            int radUnits = radius * Cluster.HVSize;
            for (int i = 0; i <= radUnits; i+= Cluster.HVSize)
            {
                ForceClusterLoad(origin, radUnits, i);
                ForceClusterLoad(origin, -radUnits, i);

                ForceClusterLoad(origin, i, radUnits);
                ForceClusterLoad(origin, i,-radUnits);

               // if (i != 0)
                {
                    ForceClusterLoad(origin, radUnits, -i);
                    ForceClusterLoad(origin, -radUnits, -i);

                    ForceClusterLoad(origin, -i, radUnits);
                    ForceClusterLoad(origin, -i, -radUnits);
                }
            }
        }

        public static void UpdateGeoForPosition(Vector3 cameraPos)
        {
            var rootCluster = World.ClusterFromPosition(cameraPos);
            if (rootCluster == null)
                return;

            ForceClusterLoad(rootCluster.Origin,0,0);

            for (int i = 1; i <= ForceLoadRadius; i++)
            {
                ForceRingLoad(i, rootCluster.Origin);
            }
        }
    }
}