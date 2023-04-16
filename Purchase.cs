using System;
using System.Collections.Generic;
using System.Linq;

namespace LittleBasket
{
    public class Purchase
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public List<ProductInBasket> Products { get; set; } = new List<ProductInBasket>();

        public override string ToString()
        {
            return $"Дата: {Date.ToShortDateString()}\n" +
                $"{string.Join('\n', Products)}\n" +
                $"\t\t\tОбщее {Products.Sum(x => x.Price * x.Count)} р";
        }
    }

    public class ProductInBasket
    {
        public int Id { get; set; }
        public Product Product { get; set; }    
        public int Count { get; set; }
        public int Price { get; set; }

        public override string ToString()
        {
            return $"{Product} {Count} шт {Price} р";
        }
    }
}
