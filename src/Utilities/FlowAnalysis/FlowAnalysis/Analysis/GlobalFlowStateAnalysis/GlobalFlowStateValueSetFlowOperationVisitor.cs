﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis.GlobalFlowStateAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;

    /// <summary>
    /// Operation visitor to flow the GlobalFlowState values across a given statement in a basic block.
    /// </summary>
    internal abstract class GlobalFlowStateValueSetFlowOperationVisitor
        : GlobalFlowStateDataFlowOperationVisitor<GlobalFlowStateAnalysisData, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateAnalysisValueSet>
    {
        protected GlobalFlowStateValueSetFlowOperationVisitor(GlobalFlowStateAnalysisContext analysisContext, bool hasPredicatedGlobalState)
            : base(analysisContext, hasPredicatedGlobalState)
        {
        }

        private void EnsureInitialized(GlobalFlowStateAnalysisData input)
        {
            if (input.Count == 0)
            {
                input[GlobalEntity] = ValueDomain.Bottom;
            }
            else
            {
                Debug.Assert(input.ContainsKey(GlobalEntity));
            }
        }

        public sealed override (GlobalFlowStateAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, GlobalFlowStateAnalysisData input)
        {
            EnsureInitialized(input);
            var result = base.FlowBranch(fromBlock, branch, input);

            if (HasPredicatedGlobalState &&
                branch.ControlFlowConditionKind != ControlFlowConditionKind.None &&
                branch.BranchValue != null &&
                result.isFeasibleBranch)
            {
                var branchValue = GetCachedAbstractValue(branch.BranchValue);
                var negate = branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse;
                MergeAndSetGlobalState(branchValue, negate);
            }

            return result;
        }

        protected void MergeAndSetGlobalState(GlobalFlowStateAnalysisValueSet value, bool negate = false)
        {
            Debug.Assert(HasPredicatedGlobalState || !negate);

            if (value.Kind == GlobalFlowStateAnalysisValueSetKind.Known)
            {
                var currentGlobalValue = GetAbstractValue(GlobalEntity);
                if (currentGlobalValue.Kind != GlobalFlowStateAnalysisValueSetKind.Unknown)
                {
                    var newGlobalValue = currentGlobalValue.WithAdditionalAnalysisValues(value, negate);
                    SetAbstractValue(GlobalEntity, newGlobalValue);
                }
            }
        }

        protected sealed override void AddTrackedEntities(GlobalFlowStateAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
            => builder.UnionWith(analysisData.Keys);

        protected sealed override void StopTrackingEntity(AnalysisEntity analysisEntity, GlobalFlowStateAnalysisData analysisData)
            => analysisData.Remove(analysisEntity);

        protected sealed override GlobalFlowStateAnalysisValueSet GetAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

        protected sealed override GlobalFlowStateAnalysisValueSet GetAbstractDefaultValue(ITypeSymbol type)
            => GlobalFlowStateAnalysisValueSet.Unset;

        protected sealed override bool HasAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.ContainsKey(analysisEntity);

        protected sealed override bool HasAnyAbstractValue(GlobalFlowStateAnalysisData data)
            => data.Count > 0;

        protected sealed override void SetAbstractValue(AnalysisEntity analysisEntity, GlobalFlowStateAnalysisValueSet value)
        {
            Debug.Assert(HasPredicatedGlobalState || value.Parents.IsEmpty);
            SetAbstractValue(CurrentAnalysisData, analysisEntity, value);
        }

        private static void SetAbstractValue(GlobalFlowStateAnalysisData analysisData, AnalysisEntity analysisEntity, GlobalFlowStateAnalysisValueSet value)
        {
            // PERF: Avoid creating an entry if the value is the default unknown value.
            if (value.Kind != GlobalFlowStateAnalysisValueSetKind.Known &&
                !analysisData.ContainsKey(analysisEntity))
            {
                return;
            }

            analysisData[analysisEntity] = value;
        }

        protected sealed override void ResetCurrentAnalysisData()
            => ResetAnalysisData(CurrentAnalysisData);

        protected sealed override GlobalFlowStateAnalysisData MergeAnalysisData(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2)
            => GlobalFlowStateAnalysisDomainInstance.Merge(value1, value2);
        protected sealed override GlobalFlowStateAnalysisData MergeAnalysisData(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2, BasicBlock forBlock)
            => HasPredicatedGlobalState && forBlock.DominatesPredecessors(DataFlowAnalysisContext.ControlFlowGraph) ?
            GlobalFlowStateAnalysisDomainInstance.Intersect(value1, value2, GlobalFlowStateAnalysisValueSetDomain.Intersect) :
            GlobalFlowStateAnalysisDomainInstance.Merge(value1, value2);
        protected sealed override void UpdateValuesForAnalysisData(GlobalFlowStateAnalysisData targetAnalysisData)
            => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);
        protected sealed override GlobalFlowStateAnalysisData GetClonedAnalysisData(GlobalFlowStateAnalysisData analysisData)
            => new(analysisData);
        public override GlobalFlowStateAnalysisData GetEmptyAnalysisData()
            => new();
        protected sealed override GlobalFlowStateAnalysisData GetExitBlockOutputData(GlobalFlowStateAnalysisResult analysisResult)
            => new(analysisResult.ExitBlockOutput.Data);
        protected sealed override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(GlobalFlowStateAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData, throwBranchWithExceptionType);
        protected sealed override bool Equals(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2)
            => GlobalFlowStateAnalysisDomainInstance.Equals(value1, value2);
        protected sealed override void ApplyInterproceduralAnalysisResultCore(GlobalFlowStateAnalysisData resultData)
            => ApplyInterproceduralAnalysisResultHelper(resultData);
        protected sealed override GlobalFlowStateAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData, SetAbstractValue);
        protected override GlobalFlowStateAnalysisData GetInitialInterproceduralAnalysisData(
            IMethodSymbol invokedMethod,
            (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? invocationInstance,
            (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCaller,
            ImmutableDictionary<IParameterSymbol, ArgumentInfo<GlobalFlowStateAnalysisValueSet>> argumentValuesMap,
            IDictionary<AnalysisEntity, PointsToAbstractValue>? pointsToValues,
            IDictionary<AnalysisEntity, CopyAbstractValue>? copyValues,
            IDictionary<AnalysisEntity, ValueContentAbstractValue>? valueContentValues,
            bool isLambdaOrLocalFunction,
            bool hasParameterWithDelegateType)
            => GetClonedCurrentAnalysisData();

        #region Visitor methods

        public override GlobalFlowStateAnalysisValueSet VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation? visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            GlobalFlowStateAnalysisValueSet defaultValue)
        {
            var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

            if (HasPredicatedGlobalState &&
                IsAnyAssertMethod(method))
            {
                var argumentValue = GetCachedAbstractValue(visitedArguments[0]);
                MergeAndSetGlobalState(argumentValue);
            }

            return value;
        }

        public override GlobalFlowStateAnalysisValueSet VisitUnaryOperatorCore(IUnaryOperation operation, object? argument)
        {
            var value = base.VisitUnaryOperatorCore(operation, argument);
            if (HasPredicatedGlobalState &&
                operation.OperatorKind == UnaryOperatorKind.Not &&
                value.Kind == GlobalFlowStateAnalysisValueSetKind.Known)
            {
                return value.GetNegatedValue();
            }

            return value;
        }

        #endregion
    }
}
