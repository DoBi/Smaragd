﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nkristek.MVVMBase.ViewModels;

namespace nkristek.MVVMBase.Tests.ViewModels
{
    /// <summary>
    /// Summary description for ValidatingViewModelTests
    /// </summary>
    [TestClass]
    public class ValidatingViewModelTests
    {
        private class TestValidatingModel
            : ValidatingViewModel
        {
            private int _testProperty;
            public int TestProperty
            {
                get => _testProperty;
                set
                {
                    if (SetProperty(ref _testProperty, value))
                        SetValidationError(TestProperty < 5 ? "Value has to be at least 5" : null);
                }
            }
            
            public override void Validate()
            {
                ValidateTestProperty();
            }

            private void ValidateTestProperty()
            {
                SetValidationError(TestProperty < 5 ? "Value has to be at least 5" : null, nameof(TestProperty));
            }
        }

        [TestMethod]
        public void TestIsValid()
        {
            var validatingModel = new TestValidatingModel();
            validatingModel.Validate();
            Assert.IsFalse(validatingModel.IsValid, "ViewModel should not be valid after initialization and calling Validate()");

            validatingModel.TestProperty = 5;
            Assert.IsTrue(validatingModel.IsValid, "ViewModel should be valid when the property has a valid value");

            validatingModel.TestProperty = 4;
            Assert.IsFalse(validatingModel.IsValid, "ViewModel should not be valid when the property has not a valid value");
        }

        [TestMethod]
        public void TestNotifyIsValid()
        {
            var invokedPropertyChangedEvents = new List<string>();

            var viewModel = new TestValidatingModel();
            viewModel.PropertyChanged += (sender, e) =>
            {
                invokedPropertyChangedEvents.Add(e.PropertyName);
            };

            viewModel.TestProperty = 5;

            // 4 PropertyChanged events should have happened, TestProperty, IsDirty (due to TestProperty), HasErrors and IsValid
            Assert.AreEqual(4, invokedPropertyChangedEvents.Count, "Invalid count of invocations of the PropertyChanged event. Invocations: " + String.Join(", ", invokedPropertyChangedEvents));
            Assert.IsTrue(invokedPropertyChangedEvents.Contains("HasErrors"), "The PropertyChanged event wasn't raised for the HasErrors property");
            Assert.IsTrue(invokedPropertyChangedEvents.Contains("IsValid"), "The PropertyChanged event wasn't raised for the IsValid property");
        }

        [TestMethod]
        public void TestError()
        {
            var validatingModel = new TestValidatingModel
            {
                TestProperty = 4
            };
            Assert.IsTrue(validatingModel.HasErrors, "HasError is not set");
            Assert.IsNotNull(validatingModel.Error, "Error is null");
        }

        [TestMethod]
        public void TestErrorSubscript()
        {
            var validatingModel = new TestValidatingModel
            {
                TestProperty = 4
            };
            Assert.IsFalse(String.IsNullOrEmpty(validatingModel[nameof(TestValidatingModel.TestProperty)]), "Specific property error could not be retrieved");
        }

        [TestMethod]
        public void TestGetError()
        {
            var validatingModel = new TestValidatingModel
            {
                TestProperty = 4
            };
            Assert.IsNotNull(validatingModel.GetErrors(nameof(TestValidatingModel.TestProperty)), "Specific property errors could not be retrieved");
        }
    }
}
