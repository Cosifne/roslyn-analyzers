﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
Imports System.Collections.Immutable
Imports Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeQuality.VisualBasic.Analyzers.QualityGuidelines
    Partial Public NotInheritable Class BasicAvoidMultipleEnumerationsAnalyzer
        Private Class BasicInvocationCountDataFlowOperationVisitor
            Inherits AvoidMultipleEnumerationsFlowOperationVisitor

            Public Sub New(analysisContext As InvocationCountAnalysisContext,
                           wellKnownDelayExecutionMethods As ImmutableArray(Of IMethodSymbol),
                           wellKnownEnumerationMethods As ImmutableArray(Of IMethodSymbol),
                           getEnumeratorMethod As IMethodSymbol)
                MyBase.New(analysisContext, wellKnownDelayExecutionMethods, wellKnownEnumerationMethods, getEnumeratorMethod)
            End Sub

            Protected Overrides Function IsExpressionOfForEachStatement(node As SyntaxNode) As Boolean
                Dim parent = TryCast(node.Parent, ForEachStatementSyntax)
                Return parent IsNot Nothing AndAlso parent.Expression.Equals(node)
            End Function
        End Class
    End Class
End Namespace