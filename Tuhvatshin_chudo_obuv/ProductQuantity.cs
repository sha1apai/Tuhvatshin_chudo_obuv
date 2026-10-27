using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuhvatshin_chudo_obuv
{
    public partial class Products
    {
        public int TotalQuantity => ProductItems?.Sum(s => s.Quantity) ?? 0;
    }
}
