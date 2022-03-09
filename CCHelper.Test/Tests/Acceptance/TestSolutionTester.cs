﻿using CCHelper.Test.Framework;
using CCHelper.Test.Framework.Abstractions;
using CCHelper.Test.Framework.Abstractions.SolutionContext;
using CCHelper.Test.Framework.Abstractions.SolutionMethod;
using CCHelper.Test.Framework.TestData;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace CCHelper.Test.Tests.Acceptance;

public class TestSolutionTester : DynamicContextFixture
{
    Action SUT_SolutionTesterConstructor<TResult>(TResult _)
    {
        static void callConstructor<TSolutionContainer>(TSolutionContainer solutionContainer)
            where TSolutionContainer : class, new() =>
            new SolutionTester<TSolutionContainer, TResult>(solutionContainer);
        // Must call the constructor with the dynamically pre-instantiated SolutionContainer.Instance.
        // The default constructor instantiates a new TSolutionContainer object statically which won't work.

        return () => callConstructor(_context.SolutionContainer.Instance);
    }

    [Theory]
    [MemberData(nameof(ValidSolutionMethods))]
    public void ShouldNotThrow_WhenSolutionContainerDefinesValidSolutionMethod(SolutionMethodStub solutionMethodStub)
    {
        solutionMethodStub.PutInContext(_context);

        Assert.True(SUT_SolutionTesterConstructor(TypeData.DummyValue).DoesNotThrow());
    }

    [Theory]
    [InlineData(AccessModifier.Internal)]
    [InlineData(AccessModifier.Protected)]
    [InlineData(AccessModifier.Private)]
    public void ShouldThrowEntryPointNotFoundException_WhenNoSolutionMethodsWereDiscovered(AccessModifier accessModifier)
    {
        SolutionMethodStub
            .NewStub
            .WithAccessModifier(accessModifier)
            .WithSolutionLabel
            .Accepting(TypeData.DummyType)
            .WithResultLabelAppliedToParameter(1)
            .Returning(TypeData.DummyType)
            .PutInContext(_context);

        Assert.Throws<EntryPointNotFoundException>(SUT_SolutionTesterConstructor(TypeData.DummyType));
    }

    [Fact]
    public void ShouldThrowAmbiguousMatchException_WhenMultipleSolutionMethodsWereDiscovered()
    {
        SolutionMethodStub
            .NewStub
            .Returning(TypeData.DummyType)
            .WithSolutionLabel
            .PutInContext(_context);
        SolutionMethodStub
            .NewStub
            .Accepting(TypeData.DummyType)
            .WithResultLabelAppliedToParameter(1)
            .Returning(typeof(void))
            .PutInContext(_context);

        Assert.Throws<AmbiguousMatchException>(SUT_SolutionTesterConstructor(TypeData.DummyType));
    }

    [Fact]
    public void ShouldThrowAmbiguousMatchException_WhenBothAttributesApplied()
    {
        SolutionMethodStub
            .NewStub
            .WithSolutionLabel
            .Accepting(TypeData.DummyType)
            .WithResultLabelAppliedToParameter(1)
            .Returning(TypeData.DummyType)
            .PutInContext(_context);

        Assert.Throws<AmbiguousMatchException>(SUT_SolutionTesterConstructor(TypeData.DummyType));
    }

    [Fact]
    public void ShouldThrowAmbiguousMatchException_WhenMultipleResultAttributesApplied()
    {
        SolutionMethodStub
            .NewStub
            .Accepting(TypeData.DummyType, TypeData.DummyType)
            .WithResultLabelAppliedToParameter(1, 2)
            .Returning(typeof(void))
            .PutInContext(_context);

        Assert.Throws<AmbiguousMatchException>(SUT_SolutionTesterConstructor(TypeData.DummyType));
    }

    [Fact]
    public void ShouldThrowFormatException_WhenOutputSolutionReturnsVoid()
    {
        SolutionMethodStub
            .NewStub
            .WithSolutionLabel
            .Returning(typeof(void))
            .PutInContext(_context);

        Assert.Throws<FormatException>(SUT_SolutionTesterConstructor(TypeData.DummyType));
    }

    [Theory]
    [MemberData(nameof(TypeData.Types), MemberType = typeof(TypeData))]
    public void ShouldThrowFormatException_WhenInputSolutionDoesNotReturnVoid(Type nonVoidType)
    {
        SolutionMethodStub
            .NewStub
            .Accepting(TypeData.DummyType)
            .WithResultLabelAppliedToParameter(1)
            .Returning(nonVoidType)
            .PutInContext(_context);

        Assert.Throws<FormatException>(SUT_SolutionTesterConstructor(TypeData.DummyValue));
    }

    public static IEnumerable<object[]> ValidSolutionMethods
    {
        get
        {
            yield return new object[] {
            SolutionMethodStub
                .NewStub
                .WithSolutionLabel
                .Returning(TypeData.DummyType)
                .Build()
            };

            yield return new object[] {
            SolutionMethodStub
                .NewStub
                .Accepting(TypeData.DummyType)
                .WithResultLabelAppliedToParameter(1)
                .Returning(typeof(void))
                .Build()
            };
        }
    }
}
