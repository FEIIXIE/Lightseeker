using System;

namespace SeekerAutoChess.Core
{
    public struct SpatialPoint : IEquatable<SpatialPoint>
    {
        public int X { get; }
        public int Y { get; }

        public SpatialPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(SpatialPoint other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is SpatialPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return X + "," + Y;
        }
    }

    public static class SpatialBattleMath
    {
        public static long DistanceSquared(SpatialPoint first, SpatialPoint second)
        {
            var dx = (long)second.X - first.X;
            var dy = (long)second.Y - first.Y;
            return dx * dx + dy * dy;
        }

        public static int IntegerSqrt(long value)
        {
            if (value <= 0) return 0;
            long low = 1;
            long high = Math.Min(value, 3037000499L);
            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                if (middle <= value / middle) low = middle + 1;
                else high = middle - 1;
            }
            return high > int.MaxValue ? int.MaxValue : (int)high;
        }

        public static SpatialPoint ClampToField(
            SpatialPoint point, int radius, int width, int height)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            if (width <= radius * 2 || height <= radius * 2)
                throw new ArgumentOutOfRangeException(nameof(width), "Field must be larger than the unit diameter.");
            return new SpatialPoint(
                Clamp(point.X, radius, width - radius),
                Clamp(point.Y, radius, height - radius));
        }

        public static SpatialPoint MoveToward(
            SpatialPoint current,
            SpatialPoint target,
            int maximumStep,
            int desiredDistance,
            int radius,
            int fieldWidth,
            int fieldHeight)
        {
            if (maximumStep <= 0) return ClampToField(current, radius, fieldWidth, fieldHeight);
            var squaredDistance = DistanceSquared(current, target);
            var distance = IntegerSqrt(squaredDistance);
            if ((long)distance * distance < squaredDistance) distance++;
            var available = distance - Math.Max(0, desiredDistance);
            if (available <= 0) return ClampToField(current, radius, fieldWidth, fieldHeight);
            var travel = Math.Min(maximumStep, available);
            var dx = (long)target.X - current.X;
            var dy = (long)target.Y - current.Y;
            var moveX = (int)(dx * travel / Math.Max(1, distance));
            var moveY = (int)(dy * travel / Math.Max(1, distance));

            // Integer normalization can truncate both axes for a very small step.
            // Resolve that case by the dominant axis only; the rule is stable and
            // does not use floating point or collection order.
            if (moveX == 0 && moveY == 0)
            {
                if (Math.Abs(dx) >= Math.Abs(dy)) moveX = Math.Sign(dx);
                else moveY = Math.Sign(dy);
            }

            return ClampToField(new SpatialPoint(current.X + moveX, current.Y + moveY),
                radius, fieldWidth, fieldHeight);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }
    }
}
