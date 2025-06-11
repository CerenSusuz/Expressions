using ExpressionTrees.Task2.ExpressionMapping.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ExpressionTrees.Task2.ExpressionMapping.Tests
{
    [TestClass]
    public class ExpressionMappingTests
    {
        // todo: add as many test methods as you wish, but they should be enough to cover basic scenarios of the mapping generator

        [TestMethod]
        public void TestMethod1()
        {
            var mapGenerator = new MappingGenerator();
            var mapper = mapGenerator.Generate<Foo, Bar>();

            var res = mapper.Map(new Foo());
        }


        [TestMethod]
        public void Should_Map_Foo_To_Bar_Correctly()
        {
            var mapGenerator = new MappingGenerator();
            var mapper = mapGenerator.Generate<Foo, Bar>();

            var foo = new Foo
            {
                Name = "Example",
                Age = 25
            };

            var bar = mapper.Map(foo);

            Assert.AreEqual("Example", bar.Name);
            Assert.AreEqual("25", bar.Age);
        }

        [TestMethod]
        public void Should_Map_Complex_Properties()
        {
            var mapGenerator = new MappingGenerator();
            var mapper = mapGenerator.Generate<Foo, Bar>();

            var foo = new Foo
            {
                Name = "John",
                Age = 30,
                Address = new Address { Street = "123 Street", City = "CityX" },
                AddressList = new List<Address>
                {
                    new Address { Street = "S1", City = "C1" },
                    new Address { Street = "S2", City = "C2" }
                }
            };

            var bar = mapper.Map(foo);

            Assert.AreEqual("John", bar.Name);
            Assert.AreEqual("30", bar.Age);
            Assert.IsNotNull(bar.Address);
            Assert.AreEqual("123 Street", bar.Address.Street);
            Assert.AreEqual("CityX", bar.Address.City);

            Assert.IsNotNull(bar.AddressList);
            Assert.AreEqual(2, bar.AddressList.Count);
            Assert.AreEqual("S1", bar.AddressList[0].Street);
            Assert.AreEqual("C1", bar.AddressList[0].City);
            Assert.AreEqual("S2", bar.AddressList[1].Street);
            Assert.AreEqual("C2", bar.AddressList[1].City);
        }

        [TestMethod]
        public void Should_Handle_Null_Source()
        {
            var mapGenerator = new MappingGenerator();
            var mapper = mapGenerator.Generate<Foo, Bar>();

            Foo foo = null;

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                var result = mapper.Map(foo);
            });
        }

        [TestMethod]
        public void Should_Cache_Mappers()
        {
            var mapGenerator = new MappingGenerator();

            var mapper1 = mapGenerator.Generate<Foo, Bar>();
            var mapper2 = mapGenerator.Generate<Foo, Bar>();

            Assert.AreSame(mapper1, mapper2);
        }

        [TestMethod]
        public void Should_Convert_Int_To_String()
        {
            var mapGenerator = new MappingGenerator();
            var mapper = mapGenerator.Generate<Foo, Bar>();

            var foo = new Foo
            {
                Name = "Test",
                Age = 42
            };

            var bar = mapper.Map(foo);

            Assert.AreEqual("42", bar.Age);
        }
    }
}
