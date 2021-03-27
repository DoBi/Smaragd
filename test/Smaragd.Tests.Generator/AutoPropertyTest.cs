using NKristek.Smaragd.Generators.Attributes;
using NKristek.Smaragd.ViewModels;
using System;

namespace Smaragd.Tests.Generator
{
    public partial class AutoPropertyTest : ViewModel
    {
        [AutoProperty]
        private int _id;

        [AutoProperty(PropertyName = "Count", NotifyMethod = nameof(NotifyTest))]
        private string _test;

        private void NotifyTest()
        {
            Console.WriteLine($"{nameof(NotifyTest)} called!");
        }
    }
}
