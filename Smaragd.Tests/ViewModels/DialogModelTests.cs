﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NKristek.Smaragd.ViewModels;

namespace NKristek.Smaragd.Tests.ViewModels
{
    [TestClass]
    public class DialogModelTests
    {
        private class TestDialogModel
            : DialogModel
        {

        }

        [TestMethod]
        public void TestTitle()
        {
            const string title = "Test";
            var dialogModel = new TestDialogModel
            {
                Title = title
            };
            Assert.AreEqual(title, dialogModel.Title, "Title property wasn't set");
        }
    }
}
