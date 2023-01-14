namespace MakePlace2Teamcraft
{
    public class Dye
    {

        public string Name { get; set; }
        public string Hex { get; set; }
        public int ID { get; set; }

        public Dye(string name, string hex, int iD)
        {
            Name = name;
            Hex = hex;
            ID = iD;
        }
    }
}
