// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{    
    public class TexlTests : PowerFxTest
    {
        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + 5", "d")]
        [InlineData("Date(2000,1,1) + Time(2000,1,1)", "d")]
        [InlineData("Date(2000,1,1) + 5", "D")]
        [InlineData("Time(2000,1,1) + Date(2000,1,1)", "d")]
        [InlineData("Time(2000,1,1) + 5", "T")]
        [InlineData("5 + DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 + Date(2000,1,1)", "D")]
        [InlineData("5 + Time(2000,1,1)", "T")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Date(2000,1,1)", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - 5", "d")]
        [InlineData("Date(2000,1,1) - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("Date(2000,1,1) - Date(1999,1,1)", "n")]
        [InlineData("Time(2,1,1) - Time(2,1,1)", "n")]
        [InlineData("5 - DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 - Date(2000,1,1)", "D")]
        [InlineData("5 - Time(2000,1,1)", "T")]
        [InlineData("-Date(2001,1,1)", "D")]
        [InlineData("-Time(2,1,1)", "T")]
        [InlineData("-DateTimeValue(\"1 Jan 2015\")", "d")]
        public void TexlDateOverloads(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out var type), script);
            Assert.True(type.IsValid, script);

            TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
        }

        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Date(2000,1,1)")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Time(2000,1,1)")]
        [InlineData("Date(2000,1,1) + Date(1999,1,1)")]
        [InlineData("Date(2000,1,1) + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(2000,1,1) + Time(1999,1,1)")]
        [InlineData("Time(2000,1,1) + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Time(2000,1,1)")]
        [InlineData("DateValue(\"1 Jan 2015\") - Time(2000,1,1)")]
        [InlineData("Time(2000,1,1) - DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(2000,1,1) - Date(2000,1,1)")]
        public void TexlDateOverloads_Negative(string script)
        {
            // TestBindingErrors(script, DType.Error);
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(script);
            
            Assert.Equal(DType.Error, result._binding.ResultType);            
            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd(DateValue(\"1 Jan 2015\"), 2)", "D")]
        [InlineData("DateAdd(DateValue(\"1 Jan 2015\"), 2, TimeUnit!Years)", "D")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), 2)", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), 2, TimeUnit!Years)", "d")]
        [InlineData("DateAdd(DateValue(\"1 Jan 2015\"), \"hello\")", "D")]
        [InlineData("DateAdd(DateValue(\"1 Jan 2015\"), \"hello\", 3)", "D")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), \"hello\")", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), \"hello\", 3)", "d")]
        public void TexlDateAdd(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out var type));
            Assert.True(type.IsValid);

            TestSimpleBindingSuccess(script, type);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:D]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:D]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Result:D]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Result:d]")]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Result:n]")]
        public void TexlDateTableFunctions(string script, string expectedType)
        {
            TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
        }

        [Theory]
        [InlineData("Average(\"3\")")]
        [InlineData("Average(\"3\", 4)")]
        [InlineData("Average(true, 4)")]
        [InlineData("Average(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsAverageWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsAverageTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);
            TestSimpleBindingSuccess("Average(1, 2, A, B, C)", DType.Number, symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsChar()
        {
            TestSimpleBindingSuccess("Char(65)", DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Value:n]")));
            TestSimpleBindingSuccess("Char(T)", TestUtils.DT("*[Result:s]"), symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsConcatenate()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("myString", FormulaType.String);
            TestSimpleBindingSuccess(
                "Concatenate(\"abcdef\", myString)",
                DType.String,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "Concatenate(Table!B, \" ending\")",
                TestUtils.DT("*[Result:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Concatenate(\" Begining\", myString, \" simple\", \"\", \" ending\")",
                DType.String,
                symbol);

            symbol.RemoveVariable("Table");
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:s]")));
            TestSimpleBindingSuccess(
                "Concatenate(Table!B, \" ending\", Table!D)",
                TestUtils.DT("*[Result:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCount()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Count(Table)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "Count(Table!A)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountA()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "CountA(Table)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:s]")));
            TestSimpleBindingSuccess(
                "CountA(Table2)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:s, B:n, C:b]")));
            TestSimpleBindingSuccess(
                "CountA(Table3!C)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountIf()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                "CountIf(Table, A < 10)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "CountIf(Table, A < 10, A > 0, A <> 2)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountRows()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:b, C:s]")));

            TestSimpleBindingSuccess(
                "CountRows(Table)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:b, C:s, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "CountRows(Table2)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "CountRows(First(Table2)!D)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFilter()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Filter(Table, A < 10)",
                TestUtils.DT("*[A:n]"),
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b]")));
            TestSimpleBindingSuccess(
                "Filter(Table2, A < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b]"),
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));
            TestSimpleBindingSuccess(
                "Filter(Table3, D!X < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]"),
                symbol);

            symbol.AddVariable("Table4", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "Filter(Table4, CountRows(D!X) < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]"),
                symbol);

            symbol.AddVariable("Table5", new TableType(TestUtils.DT("*[A:g]")));
            TestSimpleBindingSuccess(
                "Filter(Table5, A = GUID(\"43cb2147-c701-4981-b8ed-f0dd56e3fdde\"))",
                TestUtils.DT("*[A:g]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFilter_Negative()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:g]")));
            TestBindingErrors(
                "Filter(Table, A = \"43cb2147-c701-4981-b8ed-f0dd56e3fdde\")",
                TestUtils.DT("*[A:g]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFirst()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "First(Table)",
                TestUtils.DT("![A:n]"),
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:*[C:n]]")));
            TestSimpleBindingSuccess(
                "First(Table2)",
                TestUtils.DT("![A:n, B:*[C:n]]"),
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:n, B:*[C:*[D:*[E:![F:s, G:n]]]]]")));
            TestSimpleBindingSuccess(
                "First(Table3)",
                TestUtils.DT("![A:n, B:*[C:*[D:*[E:![F:s, G:n]]]]]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFirstN()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                "FirstN(Table)",
                TestUtils.DT("*[A:n]"),
                symbol);
            TestSimpleBindingSuccess(
                "FirstN(Table, 1234)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIf()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("B", new TableType(TestUtils.DT("*[X:n, Y:s, Z:b]")));
            symbol.AddVariable("C", new TableType(TestUtils.DT("*[Y:s, XX:n, Z:b]")));

            TestSimpleBindingSuccess(
                "If(A < 10, 1, 2)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, B, C)",
                TestUtils.DT("*[Y:s, Z:b]"),
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, 1, A < 20, 2, A < 30, 3)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, 1, A < 20, 2, A < 30, 3, 4)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, [[1,2,3],[3,2,1]], [[1,3,2],[3,2,3],[1,1,3]])",
                TestUtils.DT("*[Value:*[Value:n]]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIfWithArgumentCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            
            TestSimpleBindingSuccess(
                "If(A < 10, 1, \"2\")",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 1, \"one\", A < 2, 2, A < 3, true, false)",
                DType.String,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 1, true, A < 2, 2, A < 3, false, \"true\")",
                DType.Boolean,
                symbol);

            // Negative cases -- when args cannot be coerced.

            TestBindingErrors(
                "If(A < 10, 1, [1,2,3])",
                DType.Number,
                symbol);

            TestBindingErrors(
                "If(A < 10, 1, {Value: 2})",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsBlank()
        {
            TestSimpleBindingSuccess("IsBlank(\"foo\")", DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsBlank(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsBlank(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsBlankOrError()
        {
            TestSimpleBindingSuccess(
                "IsBlankOrError(\"foo\")",
                DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsBlankOrError(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsBlankOrError(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsNumeric()
        {
            TestSimpleBindingSuccess(
                "IsNumeric(\"12\")",
                DType.Boolean);

            TestSimpleBindingSuccess(
                "IsNumeric(12)",
                DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsNumeric(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsNumeric(Table)",
                DType.Boolean,
                symbol);
        }

        private void TestBindingErrors(string script, DType expectedType, SymbolTable resolver = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = resolver
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.Equal(expectedType, result._binding.ResultType);
            Assert.False(result.IsSuccess);
        }

        internal static void TestSimpleBindingSuccess(string script, DType expectedType, SymbolTable resolver = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = resolver
            };
            var engine = new Engine(config);
            var result = engine.Check(script);
            Assert.Equal(expectedType, result._binding.ResultType);
            Assert.True(result.IsSuccess);

            // return TestSimpleBindingSuccess(script, false, false, expectedType, typedGlobals);
        }
    }
}
