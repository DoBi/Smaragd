using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smaragd.Tests.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var c = new AutoPropertyTest();

            Console.WriteLine($"Id = {c.Id}");

            // the viewmodel will automatically implement INotifyPropertyChanged
            c.PropertyChanged += (o, e) => Console.WriteLine($"Property {e.PropertyName} was changed");
            c.Id = 125;

            c.Count = "Test";

            Console.WriteLine($"Id = {c.Id}");

            // Try adding fields to the ExampleViewModel class above and tagging them with the [AutoNotify] attribute
            // You'll see the matching generated properties visibile in IntelliSense in realtime
        }
    }
}
