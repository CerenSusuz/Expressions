using System.Collections.Generic;
using System.Net;

namespace ExpressionTrees.Task2.ExpressionMapping.Tests.Models
{
    internal class Bar
    {
        // add here some other properties
        public string Name { get; set; }
        public string Age { get; set; }
        public Address Address { get; set; }

        public List<Address> AddressList { get; set; }
    }
}
