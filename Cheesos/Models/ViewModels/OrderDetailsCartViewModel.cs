using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cheesos.Models.ViewModels
{
    public class OrderDetailsCartViewModel
    {
        public List<ShoppingCart> ListCart { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}