using System;
using System.Linq;
using Xunit;

namespace Test
{
    public class Main
    {
        [Theory]
        [InlineData(@"C:\Users\iwih\source\repos\iwih\PizzaSlicer\PizzaSlicer\input")]
        public void GetAvailablePizzas_Test(string path)
        {
            var pizza = PizzaSlicer.Program.GetAvailablePizzas(path);
            Assert.True(pizza.Any());
        }
    }
}
