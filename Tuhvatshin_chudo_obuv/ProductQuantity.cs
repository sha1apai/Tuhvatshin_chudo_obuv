using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuhvatshin_chudo_obuv
{
    public partial class Products
    {
        public int TotalQuantity => ProductItems?.Sum(item => item.Quantity) ?? 0;

        public string QuantityAvailability =>
            (TotalQuantity > 5 ? "много" : "мало");
    }
}
