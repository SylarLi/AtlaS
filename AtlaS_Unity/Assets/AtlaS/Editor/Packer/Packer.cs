#if AtlaS_ON
using System;
using System.Collections.Generic;

namespace UnityEditor.UI.Atlas
{
    public class Packer
    {
        public enum Algorithm
        {
            HorizontalSkyline,
            RectangleSlice,
            AdvancedHorizontalSkyline,
        }

        private Algorithm mAlgorithm;

        private IPackAlgorithm packer;

        public Packer(Algorithm algorithm, Bin bin, int padding)
        {
            mAlgorithm = algorithm;
            switch (mAlgorithm)
            {
                case Algorithm.HorizontalSkyline:
                    {
                        packer = new HorizontalSkyline(bin, padding);
                        break;
                    }
                case Algorithm.RectangleSlice:
                    {
                        packer = new RectangleSlice(bin, padding);
                        break;
                    }
                case Algorithm.AdvancedHorizontalSkyline:
                    {
                        packer = new AdvancedHorizontalSkyline(bin, padding);
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        public bool Push(Area area, out Rect rect)
        {
            return packer.Pack(area, out rect);
        }

        public void Extend(Bin bin)
        {
            packer.Extend(bin);
        }

        public Bin bin
        {
            get
            {
                return packer.bin;
            }
        }

        public class Area
        {
            public int width;

            public int height;

            public Area(int width, int height)
            {
                this.width = width;
                this.height = height;
            }
        }

        public class Bin : Area
        {
            public Bin(int width, int height) : base(width, height)
            {

            }
        }

        public class Rect
        {
            public int x;

            public int y;

            public int width;

            public int height;

            public int xMax
            {
                get
                {
                    return x + width - 1;
                }
            }

            public int yMax
            {
                get
                {
                    return y + height - 1;
                }
            }

            public int area
            {
                get
                {
                    return width * height;
                }
            }

            private List<Rect> mTemps;

            public Rect()
            {

            }

            public Rect(int x, int y, int width, int height)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
            }

            public bool Overlaps(Rect rect)
            {
                return !(xMax < rect.x ||
                    x > rect.xMax ||
                    yMax < rect.y ||
                    y > rect.yMax);
            }

            public bool Contains(Rect rect)
            {
                return x <= rect.x &&
                    xMax >= rect.xMax &&
                    y <= rect.y &&
                    yMax >= rect.yMax;
            }
        }

        protected interface IPackAlgorithm
        {
            bool Pack(Area area, out Rect rect);

            void Extend(Bin bin);

            Bin bin { get; }
        }

        protected abstract class PackAlgorithm : IPackAlgorithm
        {
            protected Bin mBin;

            protected int mPadding;

            protected List<Area> mAreas;

            public PackAlgorithm(Bin bin, int padding)
            {
                mBin = bin;
                mPadding = padding;
                mAreas = new List<Area>();
            }

            public virtual bool Pack(Area area, out Rect rect)
            {
                throw new NotImplementedException();
            }

            public virtual void Extend(Bin bin)
            {
                if (mBin.width < bin.width)
                {
                    mBin.width = bin.width;
                }
                if (mBin.height < bin.height)
                {
                    mBin.height = bin.height;
                }
            }

            public Bin bin
            {
                get
                {
                    return mBin;
                }
            }
        }

        protected class HorizontalSkyline : PackAlgorithm
        {
            protected List<Skyline> mEdges;

            protected List<Rect> mPatches;

            public HorizontalSkyline(Bin bin, int padding) : base(bin, padding)
            {
                mEdges = new List<Skyline>();
                mEdges.Add(new Skyline(0, mBin.width - 1, 0));
                mPatches = new List<Rect>();
            }

            public override bool Pack(Area area, out Rect rect)
            {
                rect = null;
                if (area.width > mBin.width ||
                    area.height > mBin.height)
                {
                    return false;
                }
                var paddingArea = new Area(
                    Math.Min(area.width + mPadding, mBin.width),
                    Math.Min(area.height + mPadding, mBin.height));
                var fromIndex = -1;
                var toIndex = -1;
                var indexHeight = -1;
                for (int i = 0; i < mEdges.Count; i++)
                {
                    var currentHeight = -1;
                    var areaTo = mEdges[i].from + paddingArea.width - 1;
                    if (areaTo < mBin.width)
                    {
                        for (var j = i; j < mEdges.Count; j++)
                        {
                            var areaHeight = mEdges[j].height + paddingArea.height;
                            if (areaHeight > mBin.height)
                            {
                                break;
                            }
                            else if (areaTo <= mEdges[j].to)
                            {
                                currentHeight = Math.Max(currentHeight, areaHeight);
                                if (fromIndex == -1 ||
                                    currentHeight < indexHeight ||
                                    mEdges[i].from < mEdges[fromIndex].from)
                                {
                                    fromIndex = i;
                                    toIndex = j;
                                    indexHeight = currentHeight;
                                }
                                break;
                            }
                        }
                    }
                }
                if (fromIndex != -1 && toIndex != -1)
                {
                    var edgeFrom = mEdges[fromIndex];
                    var edgeTo = mEdges[toIndex];
                    var areaFrom = edgeFrom.from;
                    var areaTo = edgeFrom.from + paddingArea.width - 1;
                    var areaHeight = 0;
                    for (int i = fromIndex; i <= toIndex; i++)
                    {
                        areaHeight = Math.Max(areaHeight, mEdges[i].height + paddingArea.height);
                    }
                    var startHeight = areaHeight - paddingArea.height;
                    for (int i = fromIndex; i <= toIndex; i++)
                    {
                        var edge = mEdges[i];
                        if (startHeight > edge.height)
                        {
                            mPatches.Add(new Rect(edge.from, edge.height,
                                Math.Min(edge.to, areaTo) - edge.from + 1, startHeight - edge.height));
                        }
                    }
                    mEdges.RemoveRange(fromIndex, toIndex - fromIndex + 1);
                    mEdges.Insert(fromIndex, new Skyline(areaFrom, areaTo, areaHeight));
                    if (areaTo < edgeTo.to)
                    {
                        mEdges.Insert(fromIndex + 1, new Skyline(areaTo + 1, edgeTo.to, edgeTo.height));
                    }
                    rect = new Rect(areaFrom, startHeight, area.width, area.height);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Extend(Bin bin)
            {
                if (mBin.width < bin.width)
                {
                    mEdges.Add(new Skyline(mBin.width, bin.width - 1, 0));
                    mBin.width = bin.width;
                }
                if (mBin.height < bin.height)
                {
                    mBin.height = bin.height;
                }
            }

            protected class Skyline
            {
                public int from;

                public int to;

                public int height;

                public Skyline(int from, int to, int height)
                {
                    this.from = from;
                    this.to = to;
                    this.height = height;
                }
            }
        }

        protected class RectangleSlice : PackAlgorithm
        {
            private List<Rect> mSlices;

            private List<Rect> mIdles;

            private List<Rect> mTemps;

            public RectangleSlice(Bin bin, int padding) : base(bin, padding)
            {
                mSlices = new List<Rect>();
                mSlices.Add(new Rect(0, 0, bin.width, bin.height));
                mTemps = new List<Rect>();
                mIdles = new List<Rect>();
            }

            public override bool Pack(Area area, out Rect rect)
            {
                rect = null;
                if (area.width > mBin.width ||
                    area.height > mBin.height)
                {
                    return false;
                }
                var paddingArea = new Area(
                    Math.Min(area.width + mPadding, mBin.width),
                    Math.Min(area.height + mPadding, mBin.height));
                for (int i = 0; i <= mSlices.Count - 1; i++)
                {
                    var slice = mSlices[i];
                    if (slice.width >= paddingArea.width &&
                        slice.height >= paddingArea.height)
                    {
                        rect = new Rect(slice.x, slice.y, paddingArea.width, paddingArea.height);
                        break;
                    }
                }
                if (rect != null)
                {
                    mIdles.Clear();
                    for (int i = mSlices.Count - 1; i >= 0; i--)
                    {
                        var slice = mSlices[i];
                        if (slice.Overlaps(rect))
                        {
                            mSlices.RemoveAt(i);
                            mIdles.AddRange(Substract(slice, rect));
                        }
                    }
                    for (int i = mIdles.Count - 1; i >= 0; i--)
                    {
                        var index = -1;
                        for (int j = mSlices.Count - 1; j >= 0; j--)
                        {
                            // 按区域面积排序
                            if (mIdles[i].area < mSlices[j].area)
                            {
                                index = j;
                                break;
                            }
                        }
                        mSlices.Insert(index + 1, mIdles[i]);
                    }
                    rect.width = area.width;
                    rect.height = area.height;
                    return true;
                }
                return false;
            }

            private Rect[] Substract(Rect main, Rect sub)
            {
                mTemps.Clear();
                if (main.y < sub.y)
                {
                    mTemps.Add(new Rect(main.x, main.y, main.width, sub.y - main.y));
                }
                if (main.yMax > sub.yMax)
                {
                    mTemps.Add(new Rect(main.x, sub.yMax + 1, main.width, main.yMax - sub.yMax));
                }
                if (main.x < sub.x)
                {
                    mTemps.Add(new Rect(main.x, main.y, sub.x - main.x, main.height));
                }
                if (main.xMax > sub.xMax)
                {
                    mTemps.Add(new Rect(sub.xMax + 1, main.y, main.xMax - sub.xMax, main.height));
                }
                return mTemps.ToArray();
            }

            public override void Extend(Bin bin)
            {
                if (mBin.width < bin.width)
                {
                    var height = Math.Max(mBin.height, bin.height);
                    mSlices.Add(new Rect(mBin.width, 0, bin.width - mBin.width, height));
                }
                if (mBin.height < bin.height)
                {
                    var width = Math.Max(mBin.width, bin.width);
                    mSlices.Add(new Rect(0, mBin.height, width, bin.height - mBin.height));
                }
                base.Extend(bin);
            }
        }

        protected class AdvancedHorizontalSkyline : HorizontalSkyline
        {
            private Dictionary<Rect, RectangleSlice> mSlices;

            public AdvancedHorizontalSkyline(Bin bin, int padding) : base(bin, padding)
            {
                mSlices = new Dictionary<Rect, RectangleSlice>();
            }

            public override bool Pack(Area area, out Rect rect)
            {
                rect = null;
                if (area.width > mBin.width ||
                    area.height > mBin.height)
                {
                    return false;
                }
                var paddingArea = new Area(
                    Math.Min(area.width + mPadding, mBin.width),
                    Math.Min(area.height + mPadding, mBin.height));
                var ret = false;
                foreach (var pair in mSlices)
                {
                    if (pair.Key.width >= paddingArea.width &&
                        pair.Key.height >= paddingArea.height)
                    {
                        ret = pair.Value.Pack(area, out rect);
                        if (ret)
                        {
                            rect.x += pair.Key.x;
                            rect.y += pair.Key.y;
                            break;
                        }
                    }
                }
                if (!ret)
                {
                    ret = base.Pack(area, out rect);
                    if (ret)
                    {
                        foreach (var patch in mPatches)
                        {
                            mSlices.Add(patch, new RectangleSlice(new Bin(patch.width, patch.height), mPadding));
                        }
                        mPatches.Clear();
                    }
                }
                return ret;
            }
        }
    }
}
#endif