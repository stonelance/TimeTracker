
using System;

namespace TimeTracker
{
    public struct ActivityId : IEquatable<ActivityId>
    {
        public static readonly ActivityId NoData = new ActivityId() { Value = 0 };
        public static readonly ActivityId Unknown = new ActivityId() { Value = 1 };
        public static readonly ActivityId Away = new ActivityId() { Value = 65535 };

        public int Value;

        public bool Equals(ActivityId o)
        {
            return this.Value == o.Value;
        }

        public override bool Equals(object o)
        {
            return o is ActivityId && this.Value == ((ActivityId)o).Value;
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public static bool operator ==(ActivityId a, ActivityId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(ActivityId a, ActivityId b)
        {
            return a.Value != b.Value;
        }
    }
}
