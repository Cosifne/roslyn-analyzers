﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeQuality.Analyzers.QualityGuidelines.AvoidMultipleEnumerations.FlowAnalysis
{
    internal static class InvocationSetHelpers
    {
        public static TrackingInvocationSet Merge(TrackingInvocationSet set1, TrackingInvocationSet set2)
        {
            var builder = ImmutableHashSet.CreateBuilder<IOperation>();
            var totalCount = AddInvocationCount(set1.EnumerationCount, set2.EnumerationCount);
            foreach (var operation in set1.Operations)
            {
                builder.Add(operation);
            }

            foreach (var operation in set2.Operations)
            {
                builder.Add(operation);
            }

            return new TrackingInvocationSet(builder.ToImmutable(), totalCount);
        }

        public static TrackingInvocationSet Intersect(TrackingInvocationSet set1, TrackingInvocationSet set2)
        {
            var builder = ImmutableHashSet.CreateBuilder<IOperation>();

            // Get the min of two count.
            // Example:
            // if (a)
            // {
            //    Bar.First();
            //    Bar.First();
            // }
            // else
            // {
            //    Bar.First();
            // }
            // Then 'Bar' is only guaranteed to be enumerated once after the if-else statement
            var totalCount = Min(set1.EnumerationCount, set2.EnumerationCount);
            foreach (var operation in set1.Operations)
            {
                builder.Add(operation);
            }

            foreach (var operation in set2.Operations)
            {
                builder.Add(operation);
            }

            return new TrackingInvocationSet(builder.ToImmutable(), totalCount);
        }

        private static InvocationCount Min(InvocationCount count1, InvocationCount count2)
        {
            // Unknown = -1, Zero = 0, One = 1, TwoOrMoreTime = 2
            var min = Math.Min((int)count1, (int)count2);
            return (InvocationCount)min;
        }

        private static InvocationCount AddInvocationCount(InvocationCount count1, InvocationCount count2)
            => (count1, count2) switch
            {
                (InvocationCount.None, _) => InvocationCount.None,
                (_, InvocationCount.None) => InvocationCount.None,
                (InvocationCount.Zero, _) => count2,
                (_, InvocationCount.Zero) => count1,
                (InvocationCount.One, InvocationCount.One) => InvocationCount.TwoOrMoreTime,
                (InvocationCount.TwoOrMoreTime, _) => InvocationCount.TwoOrMoreTime,
                (_, InvocationCount.TwoOrMoreTime) => InvocationCount.TwoOrMoreTime,
                (_, _) => InvocationCount.None,
            };
    }
}
