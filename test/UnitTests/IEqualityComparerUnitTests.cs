﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Nito.Comparers;
using Xunit;
using UnitTests.Util;
using static UnitTests.Util.DataUtility;
// ReSharper disable UseStringInterpolation

namespace UnitTests
{
    public class IEqualityComparerUnitTests
    {
        // For each general class of comparers, include tests for:
        //  Value type (int)
        //  Nullable value type (int?)
        //  Sequence type (int[])
        //  Non-sequence reference type (HierarchyBase)
        //  string
        //  object

        private static readonly ExpressionDictionary<IEqualityComparer> EqualityComparers = new ExpressionDictionary<IEqualityComparer>
        {
            // Default
            () => EqualityComparerBuilder.For<int>().Default(),
            () => EqualityComparerBuilder.For<int?>().Default(),
            () => EqualityComparerBuilder.For<int[]>().Default(),
            () => EqualityComparerBuilder.For<HierarchyBase>().Default(),
            () => EqualityComparerBuilder.For<string>().Default(),
            () => EqualityComparerBuilder.For<object>().Default(),

            // Null
            () => EqualityComparerBuilder.For<int>().Null(),
            () => EqualityComparerBuilder.For<int?>().Null(),
            () => EqualityComparerBuilder.For<int[]>().Null(),
            () => EqualityComparerBuilder.For<HierarchyBase>().Null(),
            () => EqualityComparerBuilder.For<string>().Null(),
            () => EqualityComparerBuilder.For<object>().Null(),

            // Reference
            () => EqualityComparerBuilder.For<int[]>().Reference(),
            () => EqualityComparerBuilder.For<HierarchyBase>().Reference(),
            () => EqualityComparerBuilder.For<string>().Reference(),
            () => EqualityComparerBuilder.For<object>().Reference(),

            // Key
            () => EqualityComparerBuilder.For<int>().EquateBy(x => x % 13, null, false),
            () => EqualityComparerBuilder.For<int?>().EquateBy(x => x % 13, null, false),
            () => EqualityComparerBuilder.For<int[]>().EquateBy(x => x.Length, null, false),
            () => EqualityComparerBuilder.For<HierarchyBase>().EquateBy(x => x.Id % 13, null, false),
            () => EqualityComparerBuilder.For<string>().EquateBy(x => x.ToLowerInvariant(), null, false),
            () => EqualityComparerBuilder.For<object>().EquateBy(x => x.GetHashCode(), null, false),

            // Sequence
            () => EqualityComparerBuilder.For<int>().Default().EquateSequence(),
            () => EqualityComparerBuilder.For<int?>().Default().EquateSequence(),
            () => EqualityComparerBuilder.For<int[]>().Default().EquateSequence(),
            () => EqualityComparerBuilder.For<HierarchyBase>().Default().EquateSequence(),
            () => EqualityComparerBuilder.For<string>().Default().EquateSequence(),
            () => EqualityComparerBuilder.For<object>().Default().EquateSequence(),

            // Hierarchy
            () => HierarchyComparers.BaseEqualityComparer,
            () => HierarchyComparers.Derived1EqualityComparer,
        };

        public static readonly List<KeyValuePair<string, IEqualityComparer>> EqualityComparersExceptObject =
            EqualityComparers.Where(x => ComparedType(x.Value) != typeof(object)).ToList();

        public static readonly TheoryData<string> All = EqualityComparers.Keys.ToTheoryData();

        public static readonly TheoryData<string> AllExceptObject = EqualityComparersExceptObject.Select(x => x.Key).ToTheoryData();


        [Theory]
        [MemberData(nameof(ReflexiveData))]
        public void Equals_IsReflexive(string comparerKey, Type comparedType)
        {
            var comparer = EqualityComparers[comparerKey];
            var instance = Fake(comparedType);
            Assert.True(comparer.Equals(instance, instance));
            Assert.Equal(comparer.GetHashCode(instance), comparer.GetHashCode(instance));
        }
        public static readonly TheoryData<string, Type> ReflexiveData =
            EqualityComparers
                .Select(x => (x.Key, ComparedType(x.Value)))
                .Append((Key(() => HierarchyComparers.BaseEqualityComparer), typeof(HierarchyDerived1)))
                .ToTheoryData();

        [Theory]
        [MemberData(nameof(All))]
        public void Equals_BothParametersNull_IsReflexive(string comparerKey)
        {
            var comparer = EqualityComparers[comparerKey];
            Assert.True(comparer.Equals(null, null));
            Assert.Equal(comparer.GetHashCode(null), comparer.GetHashCode(null));
        }

        [Theory]
        [MemberData(nameof(DifferentTypesData))]
        public void Equals_OneInstanceIsIncompatible_ReturnsFalse(string comparerKey, Type comparedType)
        {
            var comparer = EqualityComparers[comparerKey];
            comparedType = comparedType ?? ComparedType(comparer);
            var a = Fake(comparedType);
            var b = FakeNot(comparedType);
            Assert.False(comparer.Equals(a, b));
            Assert.False(comparer.Equals(b, a));
        }
        public static readonly TheoryData<string, Type> DifferentTypesData =
            EqualityComparersExceptObject
                .Select(x => (x.Key, ComparedType(x.Value)))
                .Append((Key(() => HierarchyComparers.BaseEqualityComparer), typeof(HierarchyDerived1)))
                .ToTheoryData();

        [Theory]
        [MemberData(nameof(AllExceptObject))]
        public void Equals_BothInstancesAreIncompatible_Throws(string comparerKey)
        {
            var comparer = EqualityComparers[comparerKey];
            var comparedType = ComparedType(comparer);
            var a = FakeNot(comparedType);
            var b = FakeNot(comparedType);
            Assert.ThrowsAny<ArgumentException>(() => comparer.Equals(a, b));
        }

        [Theory]
        [MemberData(nameof(AllExceptObject))]
        public void GetHashCode_IncompatibleInstance_Throws(string comparerKey)
        {
            var comparer = EqualityComparers[comparerKey];
            var comparedType = ComparedType(comparer);
            var a = FakeNot(comparedType);
            Assert.ThrowsAny<ArgumentException>(() => comparer.GetHashCode(a));
        }

        [Theory]
        [MemberData(nameof(DifferentInstancesAndEqualData))]
        public void EqualsAndGetHashCode_DifferentEquivalentInstances_AreEqual(string comparerKey, object a, object b)
        {
            if (object.ReferenceEquals(a, b))
                throw new ArgumentException("Unit test error: objects must be different instances.");

            var comparer = EqualityComparers[comparerKey];
            Assert.True(comparer.Equals(a, b));
            Assert.True(comparer.Equals(b, a));
            Assert.Equal(comparer.GetHashCode(a), comparer.GetHashCode(b));
        }
        public static readonly TheoryData<string, object, object> DifferentInstancesAndEqualData = new TheoryData<string, object, object>
        {
            // Default comparer cannot test <HierarchyBase> or <object>, since they cannot have different instances that are equivalent.
            { Key(() => EqualityComparerBuilder.For<int>().Default()), 13, 13 },
            { Key(() => EqualityComparerBuilder.For<int?>().Default()), 13, 13 },
            { Key(() => EqualityComparerBuilder.For<int[]>().Default()), new[] { 13 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<string>().Default()), "test", Duplicate("test") },

            // Null
            { Key(() => EqualityComparerBuilder.For<int>().Null()), 13, 13 },
            { Key(() => EqualityComparerBuilder.For<int?>().Null()), 13, 13 },
            { Key(() => EqualityComparerBuilder.For<int[]>().Null()), new[] { 13 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<HierarchyBase>().Null()), new HierarchyBase { Id = 13 }, new HierarchyBase { Id = 13 } },
            { Key(() => EqualityComparerBuilder.For<string>().Null()), "test", Duplicate("test") },
            { Key(() => EqualityComparerBuilder.For<object>().Null()), "test", Duplicate("test") },

            // Reference comparer cannot be tested here, since no reference comparer can have different instances that are equivalent.

            // Key
            { Key(() => EqualityComparerBuilder.For<int>().EquateBy(x => x % 13, null, false)), 13, 13 },
            { Key(() => EqualityComparerBuilder.For<int?>().EquateBy(x => x % 13, null, false)), 13, 13 },
            { Key(() => EqualityComparerBuilder.For<int[]>().EquateBy(x => x.Length, null, false)), new[] { 13 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<HierarchyBase>().EquateBy(x => x.Id % 13, null, false)), new HierarchyBase { Id = 13 }, new HierarchyBase { Id = 13 } },
            { Key(() => EqualityComparerBuilder.For<string>().EquateBy(x => x.ToLowerInvariant(), null, false)), "test", "Test" },
            { Key(() => EqualityComparerBuilder.For<object>().EquateBy(x => x.GetHashCode(), null, false)), "test", Duplicate("test") },

            // Sequence comparer cannot test <HierarchyBase>, since the xUnit serialization cannot restore equivalent references.
            { Key(() => EqualityComparerBuilder.For<int>().Default().EquateSequence()), new[] { 13 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<int?>().Default().EquateSequence()), new int?[] { 13 }, new int?[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<int[]>().Default().EquateSequence()), new[] { new[] { 13 } }, new[] { new[] { 13 } } },
            { Key(() => EqualityComparerBuilder.For<string>().Default().EquateSequence()), new[] { "test" }, new[] { "test" } },
            { Key(() => EqualityComparerBuilder.For<object>().Default().EquateSequence()), new[] { "test" }, new[] { "test" } },

            // Hierarchy
            { Key(() => HierarchyComparers.BaseEqualityComparer), new HierarchyBase { Id = 13 }, new HierarchyBase { Id = 13 } },
            { Key(() => HierarchyComparers.BaseEqualityComparer), new HierarchyDerived1 { Id = 13 }, new HierarchyDerived2 { Id = 13 } },
            { Key(() => HierarchyComparers.Derived1EqualityComparer), new HierarchyDerived1 { Id = 13 }, new HierarchyDerived1 { Id = 13 } },
        };

        [Theory]
        [MemberData(nameof(NotEqualData))]
        public void Equals_NonequivalentInstances_ReturnsFalse(string comparerKey, object a, object b)
        {
            var comparer = EqualityComparers[comparerKey];
            Assert.False(comparer.Equals(a, b));
            Assert.False(comparer.Equals(b, a));
        }
        public static readonly TheoryData<string, object, object> NotEqualData = new TheoryData<string, object, object>
        {
            // Default
            { Key(() => EqualityComparerBuilder.For<int>().Default()), 7, 13 },
            { Key(() => EqualityComparerBuilder.For<int?>().Default()), 7, 13 },
            { Key(() => EqualityComparerBuilder.For<int[]>().Default()), new[] { 7 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<HierarchyBase>().Default()), new HierarchyBase { Id = 7 }, new HierarchyBase { Id = 13 } },
            { Key(() => EqualityComparerBuilder.For<string>().Default()), "test", "other" },
            { Key(() => EqualityComparerBuilder.For<object>().Default()), "test", "other" },

            // Null comparer cannot be tested here, since no two instances are not equal.

            // Reference
            { Key(() => EqualityComparerBuilder.For<int[]>().Reference()), new[] { 7 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<HierarchyBase>().Reference()), new HierarchyBase { Id = 7 }, new HierarchyBase { Id = 13 } },
            { Key(() => EqualityComparerBuilder.For<string>().Reference()), "test", "other" },
            { Key(() => EqualityComparerBuilder.For<object>().Reference()), "test", "other" },

            // Key
            { Key(() => EqualityComparerBuilder.For<int>().EquateBy(x => x % 13, null, false)), 7, 13 },
            { Key(() => EqualityComparerBuilder.For<int?>().EquateBy(x => x % 13, null, false)), 7, 13 },
            { Key(() => EqualityComparerBuilder.For<int[]>().EquateBy(x => x.Length, null, false)), new[] { 7, 13 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<HierarchyBase>().EquateBy(x => x.Id % 13, null, false)), new HierarchyBase { Id = 7 }, new HierarchyBase { Id = 13 } },
            { Key(() => EqualityComparerBuilder.For<string>().EquateBy(x => x.ToLowerInvariant(), null, false)), "test", "other" },
            { Key(() => EqualityComparerBuilder.For<object>().EquateBy(x => x.GetHashCode(), null, false)), "test", "other" },

            // Sequence
            { Key(() => EqualityComparerBuilder.For<int>().Default().EquateSequence()), new[] { 7 }, new[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<int?>().Default().EquateSequence()), new int?[] { 7 }, new int?[] { 13 } },
            { Key(() => EqualityComparerBuilder.For<int[]>().Default().EquateSequence()), new[] { new[] { 7 } }, new[] { new[] { 13 } } },
            { Key(() => EqualityComparerBuilder.For<HierarchyBase>().Default().EquateSequence()), new[] { new HierarchyBase { Id = 7 } }, new[] { new HierarchyBase { Id = 13 } } },
            { Key(() => EqualityComparerBuilder.For<string>().Default().EquateSequence()), new[] { "test" }, new[] { "other" } },
            { Key(() => EqualityComparerBuilder.For<object>().Default().EquateSequence()), new[] { "test" }, new[] { "other" } },

            // Hierarchy
            { Key(() => HierarchyComparers.BaseEqualityComparer), new HierarchyBase { Id = 7 }, new HierarchyBase { Id = 13 } },
            { Key(() => HierarchyComparers.BaseEqualityComparer), new HierarchyDerived1 { Id = 7 }, new HierarchyDerived2 { Id = 13 } },
            { Key(() => HierarchyComparers.Derived1EqualityComparer), new HierarchyDerived1 { Id = 7 }, new HierarchyDerived1 { Id = 13 } },
        };
    }
}