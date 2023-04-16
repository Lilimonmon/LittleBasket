using System.Windows.Controls;

namespace LittleBasket
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
