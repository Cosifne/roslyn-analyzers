// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
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
        [Fact]
        public Task Test()
        {
            var code = @"
public class Bar
{
    public void Sub()
    {
        IEnumerable<int> i = Enumerable.Range(1, 10);
        {|#1:i|}|.Take(10);
        {|#2:i|}.Take(10);
    }
}";
            return VerifyCS.VerifyAnalyzerAsync(
                code,
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(1),
                VerifyCS.Diagnostic(AvoidMultipleEnumerations.MultipleEnumerableDescriptor).WithLocation(2));
        }
    }
}
