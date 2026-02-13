using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Computes a straight skeleton via iterative polygon shrinking with both edge and split events.
/// Adapted from AggroBird/StraightSkeleton (MIT License, C# Unity).
/// </summary>
internal static class StraightSkeletonAlgorithm
{
    private struct ChainVert<T>
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public int Index;
        public T X, Y;           // current position
        public T DirX, DirY;     // normalized direction to next vertex
        public T Len;             // distance to next vertex
        public T BisX, BisY;     // bisector direction
        public T Velocity;        // sin(halfAngle)
        public bool IsReflex;    // angle > pi
        public int Prev, Next;   // circular chain links
        public T OrigX, OrigY;   // start of current skeleton edge trace
        public int Pass;          // iteration marker

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecalcSegment(T nextX, T nextY, T eps)
        {
            DirX = nextX - X;
            DirY = nextY - Y;
            Len = T.Sqrt(DirX * DirX + DirY * DirY);
            if (Len <= eps)
            {
                DirX = T.Zero; DirY = T.Zero; Len = T.Zero;
            }
            else
            {
                DirX /= Len; DirY /= Len;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecalcBisector(T prevDirX, T prevDirY, T pi2, T pi)
        {
            var ax = DirX; var ay = DirY;
            var bx = -prevDirX; var by = -prevDirY;
            var cross = bx * ay - by * ax;
            var dot = bx * ax + by * ay;
            var signedAngle = T.Atan2(cross, dot);
            var angle = (signedAngle + pi2) % pi2;
            var half = angle / (T.One + T.One);

            // Clockwise rotation of outgoing direction by half-angle
            var sinA = T.Sin(-half);
            var cosA = T.Cos(-half);
            BisX = ax * cosA - ay * sinA;
            BisY = ax * sinA + ay * cosA;
            Velocity = T.Sin(half);
            IsReflex = angle > pi;
        }
    }

    internal static Skeleton<T> Compute<T>(in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        var span = polygon.Span;
        int n = span.Length;
        if (n < 3) return new Skeleton<T>(Array.Empty<Point<T>>(), Array.Empty<Segment<T>>());

        var highEps = T.CreateTruncating(1e-12);
        var medEps = T.CreateTruncating(1e-5);
        var lowEps = T.CreateTruncating(1e-3);
        var pi = T.Pi;
        var pi2 = pi + pi;

        // Reference algorithm assumes CW winding
        var pts = EnsureCW(span);
        n = pts.Length;

        // Initialize chain vertices
        var v = new ChainVert<T>[n * 8];
        int vc = n;
        for (int i = 0; i < n; i++)
        {
            v[i] = new ChainVert<T>
            {
                Index = i,
                X = pts[i].X, Y = pts[i].Y,
                OrigX = pts[i].X, OrigY = pts[i].Y,
                Prev = (i - 1 + n) % n,
                Next = (i + 1) % n
            };
        }

        // Compute initial segments
        for (int i = 0; i < n; i++)
        {
            ref var cur = ref v[i];
            cur.RecalcSegment(v[cur.Next].X, v[cur.Next].Y, highEps);
        }

        // Compute initial bisectors
        for (int i = 0; i < n; i++)
        {
            ref var prev = ref v[i];
            ref var next = ref v[prev.Next];
            next.RecalcBisector(prev.DirX, prev.DirY, pi2, pi);
        }

        var activeChains = new List<int> { 0 };
        var splitChains = new List<int>();
        var skeletonEdges = new List<Segment<T>>();
        var nodeSet = new HashSet<(double, double)>();
        var nodeList = new List<Point<T>>();

        // Reusable buffers for intersection processing
        var incidentBuf = new List<int>();
        var unresolvedBuf = new List<int>();
        var resolvedBuf = new List<int>();

        int pass = 0;
        while (activeChains.Count > 0)
        {
            if (pass++ > n * 4) break; // safety limit

            splitChains.Clear();
            for (int ci = 0; ci < activeChains.Count; ci++)
            {
                int chain = activeChains[ci];

                // Find shortest shrink distance (edge + split events)
                T dist = FindShortestDistance(v, chain, highEps, medEps);
                if (T.IsPositiveInfinity(dist) || T.IsNaN(dist))
                    continue;

                dist = T.Max(dist, T.Zero);

                // Apply shrink distance
                if (dist > T.Zero)
                    ApplyShrinkDistance(v, chain, dist, highEps, pi2, pi);

                // Process intersection events and emit skeleton edges
                ProcessIntersections(ref v, ref vc, chain, pass,
                    highEps, medEps, lowEps, pi2, pi,
                    skeletonEdges, nodeSet, nodeList,
                    incidentBuf, unresolvedBuf, resolvedBuf, splitChains);
            }

            activeChains.Clear();
            activeChains.AddRange(splitChains);
        }

        return new Skeleton<T>(nodeList.ToArray(), skeletonEdges.ToArray());
    }

    /// <summary>
    /// Finds the shortest distance before the next collision event (edge or split).
    /// Edge event: two adjacent bisectors collide.
    /// Split event: a reflex vertex's bisector hits a non-adjacent segment.
    /// </summary>
    private static T FindShortestDistance<T>(ChainVert<T>[] v, int chain, T highEps, T medEps)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T distance = T.PositiveInfinity;

        int vertIdx = chain;
        do
        {
            ref var vert = ref v[vertIdx];
            ref var next = ref v[vert.Next];

            // Edge event: project bisectors onto perpendiculars of each other
            T n0x = -vert.BisY, n0y = vert.BisX;
            T n1x = next.BisY, n1y = -next.BisX;

            T dx = next.X - vert.X;
            T dy = next.Y - vert.Y;

            T denom0 = vert.BisX * n1x + vert.BisY * n1y;
            T denom1 = next.BisX * n0x + next.BisY * n0y;

            if (T.Abs(denom0) >= highEps && T.Abs(denom1) >= highEps)
            {
                T entry0 = (dx * n1x + dy * n1y) / denom0;
                T entry1 = ((-dx) * n0x + (-dy) * n0y) / denom1;
                T entry = T.Min(entry0 * vert.Velocity, entry1 * next.Velocity);
                if (!T.IsInfinity(entry) && entry >= T.Zero && entry < distance)
                    distance = entry;
            }

            // Split event: only for reflex vertices
            if (vert.IsReflex)
            {
                int segIdx = vertIdx;
                do
                {
                    ref var segBeg = ref v[segIdx];
                    ref var segEnd = ref v[segBeg.Next];

                    // Skip adjacent segments
                    if (segIdx != vertIdx && segBeg.Next != vertIdx)
                    {
                        T perpX = segBeg.DirY, perpY = -segBeg.DirX;

                        if (TracePlane(segBeg.X, segBeg.Y, perpX, perpY,
                                vert.X, vert.Y, vert.BisX, vert.BisY,
                                highEps, out T entry))
                        {
                            // Calculate time of collision
                            T angleBetween = AngleBetween(vert.BisX, vert.BisY,
                                segBeg.DirX, segBeg.DirY);
                            if (angleBetween > T.Zero)
                            {
                                entry /= (T.One / T.Sin(angleBetween) + T.One / vert.Velocity);
                                if (!T.IsInfinity(entry) && entry >= T.Zero)
                                {
                                    // Verify the collision point actually falls on the grown segment
                                    T s0x = segBeg.X + segBeg.BisX * (entry / segBeg.Velocity);
                                    T s0y = segBeg.Y + segBeg.BisY * (entry / segBeg.Velocity);
                                    T s1x = segEnd.X + segEnd.BisX * (entry / segEnd.Velocity);
                                    T s1y = segEnd.Y + segEnd.BisY * (entry / segEnd.Velocity);
                                    T length = (s1x - s0x) * segBeg.DirX + (s1y - s0y) * segBeg.DirY;
                                    if (length > T.Zero)
                                    {
                                        T px = vert.X + vert.BisX * (entry / vert.Velocity);
                                        T py = vert.Y + vert.BisY * (entry / vert.Velocity);
                                        T project = (px - s0x) * segBeg.DirX + (py - s0y) * segBeg.DirY;
                                        if (project >= -medEps && project <= length + medEps)
                                        {
                                            if (entry < distance)
                                                distance = entry;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    segIdx = segBeg.Next;
                }
                while (segIdx != vertIdx);
            }

            vertIdx = vert.Next;
        }
        while (vertIdx != chain);

        return distance;
    }

    /// <summary>
    /// Moves all vertices in the chain along their bisectors by the given distance.
    /// Then recalculates segments and bisectors.
    /// </summary>
    private static void ApplyShrinkDistance<T>(ChainVert<T>[] v, int chain, T distance,
        T highEps, T pi2, T pi)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        // Move vertices
        int vertIdx = chain;
        do
        {
            ref var vert = ref v[vertIdx];
            if (vert.Velocity > highEps)
            {
                T scale = distance / vert.Velocity;
                vert.X += vert.BisX * scale;
                vert.Y += vert.BisY * scale;
            }
            vertIdx = vert.Next;
        }
        while (vertIdx != chain);

        // Recalculate segments
        vertIdx = chain;
        do
        {
            ref var cur = ref v[vertIdx];
            ref var nxt = ref v[cur.Next];
            cur.RecalcSegment(nxt.X, nxt.Y, highEps);
            vertIdx = cur.Next;
        }
        while (vertIdx != chain);

        // Recalculate bisectors
        vertIdx = chain;
        do
        {
            ref var cur = ref v[vertIdx];
            ref var nxt = ref v[cur.Next];
            nxt.RecalcBisector(cur.DirX, cur.DirY, pi2, pi);
            vertIdx = cur.Next;
        }
        while (vertIdx != chain);
    }

    /// <summary>
    /// Detects split events (vertex on non-adjacent segment) and incident vertices (collisions).
    /// Emits skeleton edges and splits chains accordingly.
    /// </summary>
    private static void ProcessIntersections<T>(ref ChainVert<T>[] v, ref int vc,
        int chain, int pass,
        T highEps, T medEps, T lowEps, T pi2, T pi,
        List<Segment<T>> skeletonEdges, HashSet<(double, double)> nodeSet, List<Point<T>> nodeList,
        List<int> incidentBuf, List<int> unresolvedBuf, List<int> resolvedBuf,
        List<int> splitChains)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        // Phase 1: Split event detection — find vertices near non-adjacent segments
        int vertIdx = chain;
        do
        {
            ref var vert = ref v[vertIdx];

            // Skip vertices already processed in this pass (from split insertions)
            if (vert.Pass != pass)
            {
                // Find closest non-adjacent segment
                T bestDistSq = T.PositiveInfinity;
                int bestSeg = -1;
                T bestPx = T.Zero, bestPy = T.Zero;

                int segIdx = vertIdx;
                do
                {
                    ref var seg = ref v[segIdx];
                    if (segIdx != vertIdx && seg.Next != vertIdx)
                    {
                        TraceSegment(ref vert, ref seg, ref v[seg.Next], lowEps,
                            ref bestDistSq, ref bestSeg, ref bestPx, ref bestPy);
                    }
                    segIdx = seg.Next;
                }
                while (segIdx != vertIdx);

                if (bestSeg >= 0)
                {
                    // Snap vertex to intersection point
                    vert.X = bestPx;
                    vert.Y = bestPy;

                    // Insert new vertex on the segment
                    EnsureCapacity(ref v, vc + 1);
                    v[vc] = new ChainVert<T>
                    {
                        Index = vc,
                        X = bestPx, Y = bestPy,
                        OrigX = bestPx, OrigY = bestPy,
                        Pass = pass
                    };
                    int newIdx = vc++;

                    ref var segBeg = ref v[bestSeg];
                    int segEndIdx = segBeg.Next;

                    // Link: segBeg -> newVert -> segEnd
                    v[newIdx].Prev = bestSeg;
                    v[newIdx].Next = segEndIdx;
                    segBeg.Next = newIdx;
                    v[segEndIdx].Prev = newIdx;

                    // Recalculate segments and bisectors around insertion point
                    RecalcSurrounding(v, newIdx, highEps, pi2, pi);
                }
            }

            vertIdx = v[vertIdx].Next;
        }
        while (vertIdx != chain);

        // Phase 2: Incident vertex resolution — find vertices at the same position
        resolvedBuf.Clear();
        unresolvedBuf.Clear();
        unresolvedBuf.Add(chain);

        while (unresolvedBuf.Count > 0)
        {
            int last = unresolvedBuf.Count - 1;
            int unresolved = unresolvedBuf[last];
            unresolvedBuf.RemoveAt(last);

            bool foundIncident = false;
            vertIdx = unresolved;
            do
            {
                ref var vert = ref v[vertIdx];
                if (vert.Pass != pass)
                {
                    vert.Pass = pass;
                    incidentBuf.Clear();

                    // Scan for vertices at the same position
                    int otherIdx = vertIdx;
                    do
                    {
                        ref var other = ref v[otherIdx];
                        if (otherIdx != vertIdx)
                        {
                            T dx = vert.X - other.X;
                            T dy = vert.Y - other.Y;
                            T distSq = dx * dx + dy * dy;
                            T threshold = lowEps * (T.One + T.One);
                            if (distSq < threshold * threshold)
                            {
                                other.Pass = pass;
                                incidentBuf.Add(otherIdx);
                            }
                        }
                        otherIdx = other.Next;
                    }
                    while (otherIdx != vertIdx);

                    if (incidentBuf.Count > 0)
                    {
                        foundIncident = true;
                        incidentBuf.Add(vertIdx); // include self

                        // Emit skeleton edges from all incident vertices to the event point
                        var eventPt = new Point<T>(vert.X, vert.Y);
                        foreach (int iv in incidentBuf)
                        {
                            ref var inc = ref v[iv];
                            var orig = new Point<T>(inc.OrigX, inc.OrigY);
                            EmitEdge(skeletonEdges, nodeSet, nodeList, orig, eventPt, medEps);
                        }

                        // Split chains at incident vertices
                        int incCount = incidentBuf.Count;
                        for (int i = 0; i < incCount; i++)
                        {
                            int prevIncIdx = incidentBuf[i];
                            int nextIncIdx = incidentBuf[(i + 1) % incCount];
                            ref var prevInc = ref v[prevIncIdx];
                            ref var nextInc = ref v[nextIncIdx];

                            if (prevInc.Next != nextIncIdx)
                            {
                                // There are non-incident vertices between these two —
                                // extract them as a sub-chain with a new vertex at the event point
                                EnsureCapacity(ref v, vc + 1);
                                v[vc] = new ChainVert<T>
                                {
                                    Index = vc,
                                    X = vert.X, Y = vert.Y,
                                    OrigX = vert.X, OrigY = vert.Y,
                                    Pass = pass
                                };
                                int newIdx = vc++;

                                int beforeNextInc = nextInc.Prev;
                                int afterPrevInc = prevInc.Next;

                                // Link: beforeNextInc -> newVert -> afterPrevInc
                                v[newIdx].Prev = beforeNextInc;
                                v[newIdx].Next = afterPrevInc;
                                v[beforeNextInc].Next = newIdx;
                                v[afterPrevInc].Prev = newIdx;

                                RecalcSurrounding(v, newIdx, highEps, pi2, pi);
                                unresolvedBuf.Add(newIdx);
                            }
                            // else: adjacent incident vertices = triangle closure, already emitted
                        }

                        break; // restart from unresolvedBuf
                    }
                }

                vertIdx = v[vertIdx].Next;
            }
            while (vertIdx != unresolved);

            if (!foundIncident)
                resolvedBuf.Add(unresolved);
        }

        // Phase 3: Filter resolved chains
        foreach (int resolved in resolvedBuf)
        {
            int count = ChainLength(v, resolved);

            if (count > 2)
            {
                // Chain is still alive — continue shrinking
                splitChains.Add(resolved);
            }
            else if (count == 2)
            {
                // Final 2-vertex chain — emit edges to midpoint and close
                ref var a = ref v[resolved];
                ref var b = ref v[a.Next];
                var two = T.One + T.One;
                var mid = new Point<T>((a.X + b.X) / two, (a.Y + b.Y) / two);
                EmitEdge(skeletonEdges, nodeSet, nodeList,
                    new Point<T>(a.OrigX, a.OrigY), mid, medEps);
                EmitEdge(skeletonEdges, nodeSet, nodeList,
                    new Point<T>(b.OrigX, b.OrigY), mid, medEps);
            }
            else if (count == 1)
            {
                // Degenerate single vertex — emit edge from origin
                ref var a = ref v[resolved];
                EmitEdge(skeletonEdges, nodeSet, nodeList,
                    new Point<T>(a.OrigX, a.OrigY), new Point<T>(a.X, a.Y), medEps);
            }
        }
    }

    // ════════════════════════════════════════════════
    //  Helper methods
    // ════════════════════════════════════════════════

    private static Point<T>[] EnsureCW<T>(ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        T signedArea = T.Zero;
        for (int i = 0; i < n; i++)
        {
            var cur = vertices[i];
            var next = vertices[(i + 1) % n];
            signedArea += cur.X * next.Y - next.X * cur.Y;
        }

        var result = vertices.ToArray();
        if (signedArea > T.Zero) // CCW -> reverse to CW
            Array.Reverse(result);
        return result;
    }

    /// <summary>
    /// Trace a plane from a ray. Returns false if the ray is behind the plane or parallel.
    /// Adapted from AggroBird reference.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TracePlane<T>(
        T planeX, T planeY, T normalX, T normalY,
        T originX, T originY, T dirX, T dirY,
        T eps, out T entry)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        entry = T.Zero;
        T relX = originX - planeX;
        T relY = originY - planeY;
        T sideDot = relX * normalX + relY * normalY;
        if (sideDot >= T.Zero)
        {
            T denom = dirX * normalX + dirY * normalY;
            if (T.Abs(denom) >= eps)
            {
                T toPlaneX = planeX - originX;
                T toPlaneY = planeY - originY;
                entry = (toPlaneX * normalX + toPlaneY * normalY) / denom;
                return entry >= T.Zero;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if a vertex is near a segment. If closer than current best, update result.
    /// Adapted from AggroBird reference.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TraceSegment<T>(
        ref ChainVert<T> vert,
        ref ChainVert<T> segBeg, ref ChainVert<T> segEnd,
        T accuracy,
        ref T bestDistSq, ref int bestSeg, ref T bestPx, ref T bestPy)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T accSq = accuracy * accuracy;

        // Not near segment start
        T dx = vert.X - segBeg.X;
        T dy = vert.Y - segBeg.Y;
        if (dx * dx + dy * dy <= accSq) return;

        // Not near segment end
        dx = vert.X - segEnd.X;
        dy = vert.Y - segEnd.Y;
        if (dx * dx + dy * dy <= accSq) return;

        if (segBeg.Len <= T.Zero) return;

        // Project point onto segment
        T relX = vert.X - segBeg.X;
        T relY = vert.Y - segBeg.Y;
        T project = relX * segBeg.DirX + relY * segBeg.DirY;

        if (project >= T.Zero && project <= segBeg.Len)
        {
            // Check perpendicular distance
            T perpX = segBeg.DirY;
            T perpY = -segBeg.DirX;
            T perpDist = relX * perpX + relY * perpY;

            if (T.Abs(perpDist) <= accuracy)
            {
                // Project point onto segment line to avoid warping
                T px = segBeg.X + segBeg.DirX * project;
                T py = segBeg.Y + segBeg.DirY * project;
                dx = vert.X - px;
                dy = vert.Y - py;
                T distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestSeg = segBeg.Index;
                    bestPx = px;
                    bestPy = py;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T AngleBetween<T>(T ax, T ay, T bx, T by)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T lenSq = (ax * ax + ay * ay) * (bx * bx + by * by);
        T sqrtLen = T.Sqrt(lenSq);
        if (sqrtLen < T.CreateTruncating(1e-15)) return T.Zero;
        T dot = ax * bx + ay * by;
        T clamped = T.Clamp(dot / sqrtLen, -T.One, T.One);
        return T.Acos(clamped);
    }

    private static void RecalcSurrounding<T>(ChainVert<T>[] v, int idx, T highEps, T pi2, T pi)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        ref var vert = ref v[idx];
        ref var prev = ref v[vert.Prev];
        ref var next = ref v[vert.Next];

        prev.RecalcSegment(vert.X, vert.Y, highEps);
        vert.RecalcSegment(next.X, next.Y, highEps);

        ref var prevPrev = ref v[prev.Prev];
        prev.RecalcBisector(prevPrev.DirX, prevPrev.DirY, pi2, pi);
        vert.RecalcBisector(prev.DirX, prev.DirY, pi2, pi);
        next.RecalcBisector(vert.DirX, vert.DirY, pi2, pi);
    }

    private static int ChainLength<T>(ChainVert<T>[] v, int chain)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int count = 0;
        int idx = chain;
        do
        {
            count++;
            idx = v[idx].Next;
        }
        while (idx != chain && count < 10000);
        return count;
    }

    private static void EnsureCapacity<T>(ref ChainVert<T>[] v, int needed)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (needed <= v.Length) return;
        int newSize = v.Length;
        while (newSize < needed) newSize <<= 1;
        var newArr = new ChainVert<T>[newSize];
        Array.Copy(v, newArr, v.Length);
        v = newArr;
    }

    private static void EmitEdge<T>(List<Segment<T>> edges, HashSet<(double, double)> nodeSet,
        List<Point<T>> nodeList, Point<T> a, Point<T> b, T eps)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        if (T.Abs(dx) < eps && T.Abs(dy) < eps) return;

        AddNode(nodeSet, nodeList, a);
        AddNode(nodeSet, nodeList, b);
        edges.Add(new Segment<T>(a, b));
    }

    private static void AddNode<T>(HashSet<(double, double)> nodeSet, List<Point<T>> nodeList,
        Point<T> pt)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var key = (Math.Round(Convert.ToDouble(pt.X), 7), Math.Round(Convert.ToDouble(pt.Y), 7));
        if (nodeSet.Add(key))
            nodeList.Add(pt);
    }
}
