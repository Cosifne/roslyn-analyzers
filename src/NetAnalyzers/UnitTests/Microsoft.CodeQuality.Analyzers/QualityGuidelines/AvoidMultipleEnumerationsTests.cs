// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeQuality.Analyzers.QualityGuidelines;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeQuality.Analyzers.QualityGuidelines.AvoidMultipleEnumerations,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeQuality.Analyzers.QualityGuidelines.AvoidMultipleEnumerations,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.NetAnalyzers.UnitTests.Microsoft.CodeQuality.Analyzers.QualityGuidelines
{
    public class AvoidMultipleEnumerationsTests
    {
        public static IEnumerable<object[]> KnownMethodsCauseEnumeration =
            ImmutableArray.Create(
                new object[] { "System.Linq.Enumerable.Aggregate" },
                new object[] { "System.Linq.Enumerable.All" },
                new object[] { "System.Linq.Enumerable.Any" },
                new object[] { "System.Linq.Enumerable.Average" },
                new object[] { "System.Linq.Enumerable.Contains" },
                new object[] { "System.Linq.Enumerable.Count" },
                new object[] { "System.Linq.Enumerable.ElementAt" },
                new object[] { "System.Linq.Enumerable.ElementAtOrDefault" },
                new object[] { "System.Linq.Enumerable.First" },
                new object[] { "System.Linq.Enumerable.FirstOrDefault" },
                new object[] { "System.Linq.Enumerable.Last" },
                new object[] { "System.Linq.Enumerable.LastOrDefault" },
                new object[] { "System.Linq.Enumerable.LongCount" },
                new object[] { "System.Linq.Enumerable.Max" },
                new object[] { "System.Linq.Enumerable.Min" },
                new object[] { "System.Linq.Enumerable.SequenceEqual" },
                new object[] { "System.Linq.Enumerable.Single" },
                new object[] { "System.Linq.Enumerable.SingleOrDefault" },
                new object[] { "System.Linq.Enumerable.Sum" },
                new object[] { "System.Linq.Enumerable.ToArray" },
                new object[] { "System.Linq.Enumerable.ToDictionary" },
                new object[] { "System.Linq.Enumerable.ToHashSet" },
                new object[] { "System.Linq.Enumerable.ToList" },
                new object[] { "System.Linq.Enumerable.ToLookup"});

        private static string CreateCodeForKnownLinqMethod(string methodName)
        {
            return methodName switch
            {
                "System.Linq.Enumerable.Aggregate" => "Aggregate((value1, value2) => value1 + value2)",
                "System.Linq.Enumerable.All" => "All(value => value == 1)",
                "System.Linq.Enumerable.Contains" => "Contains(1)",
                "System.Linq.Enumerable.ElementAt" => "ElementAt(1)",
                "System.Linq.Enumerable.ElementAtOrDefault" => "ElementAtOrDefault(1)",
                "System.Linq.Enumerable.SequenceEqual" => "SequenceEqual(new [] { 1, 2, 3})",
                "System.Linq.Enumerable.ToDictionary" => "ToDictionary(value => value)",
                "System.Linq.Enumerable.ToLookup" => "ToLookup(value => value)",
                _ => methodName.Substring("System.Linq.Enumerable.".Length) + "()"
            };
        }

        [Fact]
        public void TestNoMissingLinqEnumerationMethod()
        {
            Assert.Equal(
                KnownMethodsCauseEnumeration.Select(objectArray => (string)objectArray[0]),
                AvoidMultipleEnumerations.ExecutedImmediateMethods);
        }

        [Theory]
        [MemberData(nameof(KnownMethodsCauseEnumeration))]
        public Task TestLinqMethodInBasicMethod(string methodName)
        {
            var codeSnippet = CreateCodeForKnownLinqMethod(methodName);
            var code = $@"
using System;
using System.Linq;
using System.Collections.Generic;

public class Bar
{{
    public void Sub()
    {{
        IEnumerable<int> i = Enumerable.Range(1, 10);
        var c = {{|#1:i|}}.{codeSnippet};
        var d = {{|#2:i|}}.{codeSnippet};
    }}
}}";
            return VerifyCS.VerifyAnalyzerAsync(
                code,
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(1),
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(2));
        }

        [Theory]
        [MemberData(nameof(KnownMethodsCauseEnumeration))]
        public Task TestLinqMethodInForLoop(string methodName)
        {
            var codeSnippet = CreateCodeForKnownLinqMethod(methodName);
            var code = $@"
using System;
using System.Linq;
using System.Collections.Generic;

public class Bar
{{
    public void Sub()
    {{
        IEnumerable<int> i = Enumerable.Range(1, 10);
        for (int j = 0; j < 100; j++)
        {{
            {{|#1:i|}}.{codeSnippet};
        }}
    }}
}}";
            return VerifyCS.VerifyAnalyzerAsync(
                code,
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(1));
        }

        [Theory]
        [MemberData(nameof(KnownMethodsCauseEnumeration))]
        public Task TestLinqMethodInForEachLoop(string methodName)
        {
            var codeSnippet = CreateCodeForKnownLinqMethod(methodName);
            var code = $@"
using System;
using System.Linq;
using System.Collections.Generic;

public class Bar
{{
    public void Sub()
    {{
        IEnumerable<int> i = Enumerable.Range(1, 10);
        foreach (var c in Enumerable.Range(1, 10))
        {{
            {{|#1:i|}}.{codeSnippet};
        }}
    }}
}}";
            return VerifyCS.VerifyAnalyzerAsync(
                code,
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(1));
        }

        [Theory]
        [MemberData(nameof(KnownMethodsCauseEnumeration))]
        public Task TestLinqMethodInWhileLoop(string methodName)
        {
            var codeSnippet = CreateCodeForKnownLinqMethod(methodName);
            var code = $@"
using System;
using System.Linq;
using System.Collections.Generic;

public class Bar
{{
    public void Sub()
    {{
        IEnumerable<int> i = Enumerable.Range(1, 10);
        while (true)
        {{
            {{|#1:i|}}.{codeSnippet};
        }}
    }}
}}";
            return VerifyCS.VerifyAnalyzerAsync(
                code,
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(1));
        }

        [Theory]
        [MemberData(nameof(KnownMethodsCauseEnumeration))]
        public Task TestLinqMethodAfterUnreachableCode(string methodName)
        {
            var codeSnippet = CreateCodeForKnownLinqMethod(methodName);
            var code = $@"
using System;
using System.Linq;
using System.Collections.Generic;

public class Bar
{{
    public void Sub()
    {{
        IEnumerable<int> i = Enumerable.Range(1, 10);
        if (false)
        {{
            i.{codeSnippet}();
        }}

        i.{codeSnippet}();
    }}
}}";
            return VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Theory]
        [MemberData(nameof(KnownMethodsCauseEnumeration))]
        public Task TestLinqMethodAfterIfStatement(string methodName)
        {
            var codeSnippet = CreateCodeForKnownLinqMethod(methodName);
            var code = $@"
using System;
using System.Linq;
using System.Collections.Generic;

public class Bar
{{
    public void Sub(bool f)
    {{
        IEnumerable<int> i = Enumerable.Range(1, 10);
        if (f)
        {{
            {{|#1:i|}}.{codeSnippet}();
        }}

        {{|#2:i|}}.{codeSnippet}();
    }}
}}";
            return VerifyCS.VerifyAnalyzerAsync(
                code,
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(1),
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(2));
        }
    }
}
