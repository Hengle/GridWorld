﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Urho;

namespace GridWorld
{
    public class Cluster : IOctreeObject
    {
        public class Block
        {
            public enum Geometry
            {
                Empty,
                Solid,
                Fluid,
                NorthFullRamp,
                SouthFullRamp,
                EastFullRamp,
                WestFullRamp,
                HalfUpper,
                HalfLower,
                NorthHalfLowerRamp,
                SouthHalfLowerRamp,
                EastHalfLowerRamp,
                WestHalfLowerRamp,
                NorthHalfUpperRamp,
                SouthHalfUpperRamp,
                EastHalfUpperRamp,
                WestHalfUpperRamp,
            }

            public int DefID;

            public Geometry Geom;

            public static Block Empty = new Block(World.BlockDef.EmptyID, Geometry.Empty);
            public static Block Invalid = new Block(World.BlockDef.EmptyID, Geometry.Empty);

            public Block() { }

            public Block(int id, Geometry geo)
            {
                DefID = id;
                Geom = geo;
            }

            public float GetDForLocalPosition(float h, float v)
            {
                if (Geom == Geometry.Empty)
                    return float.MinValue;

                float invH = 1 - h;
                float invV = 1 - v;

                switch (Geom)
                {
                    case Cluster.Block.Geometry.Solid:
                    case Cluster.Block.Geometry.HalfUpper:
                        return 1;

                    case Cluster.Block.Geometry.HalfLower:
                        return 0.5f;

                    case Cluster.Block.Geometry.NorthFullRamp:
                        return v;

                    case Cluster.Block.Geometry.SouthFullRamp:
                        return 1.0f - v;

                    case Cluster.Block.Geometry.EastFullRamp:
                        return h;

                    case Cluster.Block.Geometry.WestFullRamp:
                        return 1.0f - h;

                    case Cluster.Block.Geometry.NorthHalfLowerRamp:
                        return v * 0.5f;

                    case Cluster.Block.Geometry.SouthHalfLowerRamp:
                        return (1.0f - v) * 0.5f;

                    case Cluster.Block.Geometry.EastHalfLowerRamp:
                        return h * 0.5f;

                    case Cluster.Block.Geometry.WestHalfLowerRamp:
                        return (1.0f - h) * 0.5f;

                    case Cluster.Block.Geometry.NorthHalfUpperRamp:
                        return (v * 0.5f) + 0.5f;

                    case Cluster.Block.Geometry.SouthHalfUpperRamp:
                        return ((1.0f - v) * 0.5f) + 0.5f;

                    case Cluster.Block.Geometry.EastHalfUpperRamp:
                        return (h * 0.5f) + 0.5f;

                    case Cluster.Block.Geometry.WestHalfUpperRamp:
                        return ((1.0f - h) * 0.5f) + 0.5f;
                }

                return float.MinValue;
            }
        }

        public Block[] _Blocks = null;

        public Block[] Blocks
        {
            get
            {
                if (_Blocks == null)
                {
                    _Blocks = new Block[HVSize * HVSize * DSize];
                    for (int i = 0; i < _Blocks.Length; i++)
                        _Blocks[i] = Block.Empty;
                }
                return _Blocks;
            }
        }

        public void ClearAllBlocks()
        {
            for (int i = 0; i < Blocks.Length; i++)
                Blocks[i] = Block.Empty;
            Geometry = null;
        }

        public Block GetBlockRelative(int h, int v, int d)
        {
            try
            {
                return Blocks[(d * HVSize * HVSize) + (v * HVSize) + h];
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public Block GetBlockAbs(int h, int v, int d)
        {
            return GetBlockRelative(h - Origin.H, v - Origin.V, d);
        }

        public void SetBlockRelative(int h, int v, int d, Block block)
        {
            Blocks[(d * HVSize * HVSize) + (v * HVSize) + h] = block;
        }

        public void SetBlockAbs(int h, int v, int d, Block block)
        {
            SetBlockRelative(h - Origin.H, v - Origin.V, d, block);
        }

        public ClusterPos GetPositionRelative(Vector3 vec)
        {
            return new ClusterPos((int)vec.X - Origin.H, (int)vec.Z - Origin.V);
        }

        public Vector3 GetBlockRelativePostion(int index)
        {
            int d = index % (HVSize * HVSize);
            int planeStart = index - (d * (HVSize * HVSize));
            int v = planeStart % HVSize;
            int h = index - (v * HVSize);

            return new Vector3(h, d, v);
        }

        public Vector3 GetBlockRelativePostion(Block block)
        {
            return GetBlockRelativePostion(Array.IndexOf(Blocks, block));
        }

        public static int HVSize = 32;
        public static int DSize = 32;

        public class ClusterPos
        {
            public static ClusterPos Zero = new ClusterPos(0, 0);

            public int H = 0;
            public int V = 0;

            public ClusterPos() { }
            public ClusterPos(int h, int v) { H = h; V = v; }
            public ClusterPos(ClusterPos pos) { H = pos.H; V = pos.V; }

            public override int GetHashCode()
            {
                return H.GetHashCode() ^ V.GetHashCode();
            }

            public override string ToString()
            {
                return H.ToString() + "," + V.ToString();
            }


            public string ToString(string format)
            {
                return H.ToString(format) + "," + V.ToString(format);
            }

            public override bool Equals(object obj)
            {
                ClusterPos p = obj as ClusterPos;
                if (p == null)
                    return false;

                return p.H == H && p.V == V;
            }
        }

        public ClusterPos Origin = ClusterPos.Zero;
        private bool BoundsValid = false;
        private BoundingBox _Bounds = new BoundingBox(0,0);

        public BoundingBox Bounds
        {
            get
            {
                if (!BoundsValid)
                {
                    BoundsValid = true;
                    _Bounds = new BoundingBox(new Vector3(Origin.H, 0, Origin.V), new Vector3(Origin.H + HVSize, DSize, Origin.V + HVSize ));
                }

                return _Bounds;
            }
        }

        public BoundingBox GetOctreeBounds()
        {
            return Bounds;
        }

        public delegate void BlockCallback(int h, int v, int d, Block block);

        public void DoForEachBlock(BlockCallback callback)
        {
            if (callback == null)
                return;

            for (int d = 0; d < DSize; d++)
            {
                for (int v = 0; v < HVSize; v++)
                {
                    for (int h = 0; h < Cluster.HVSize; h++)
                        callback.Invoke(h, v, d, GetBlockRelative(h, v, d));
                }
            }
        }

        public delegate void BlockGeoCallback(Vector3 blockPos, Block block);

        public void DoForEachBlock(BlockGeoCallback callback)
        {
            if (callback == null)
                return;

            for (int d = 0; d < DSize; d++)
            {
                for (int v = 0; v < HVSize; v++)
                {
                    for (int h = 0; h < Cluster.HVSize; h++)
                        callback.Invoke(new Vector3(h,d,v), GetBlockRelative(h, v, d));
                }
            }
        }

        [XmlIgnore]
        public object Tag = null;

        [XmlIgnore]
        public object RenderTag = null;

        [XmlIgnore]
        public ClusterGeometry Geometry = null;
    }
}
