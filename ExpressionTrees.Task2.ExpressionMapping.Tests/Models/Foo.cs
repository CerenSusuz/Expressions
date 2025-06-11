using System.Collections.Generic;
using System.Net;

namespace ExpressionTrees.Task2.ExpressionMapping.Tests.Models
{
    internal class Foo
    {
        // add here some properties
        public string Name { get; set; }
        public int Age { get; set; }

        // Complex property 
        public Address Address { get; set; }

        // Collection property 
        public List<Address> AddressList { get; set; }
    }
}
