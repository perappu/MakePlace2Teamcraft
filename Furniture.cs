using System;

namespace MakePlace2Teamcraft
{
    public class Furniture : IEquatable<Furniture>
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public int ID { get; set; }

        public Furniture(string name, int count, int iD)
        {
            Name = name;
            Count = count;
            ID = iD;
        }

        // Overrides and required methods for IEquatable
        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            else if (other is Furniture)
            {
                return this.Equals(other);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(Furniture other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return this.ID == other.ID;
            }

        }

        public override int GetHashCode()
        {
            return ID;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public string ToImportString()
        {
            return this.ID.ToString() + "," + "null," + this.Count.ToString();
        }
    }
}
