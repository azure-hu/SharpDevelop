﻿// 
// CreateField.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create field", Description = "Creates a field for a undefined variable.")]
	public class CreateFieldAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var identifier = context.GetNode<IdentifierExpression>();
			if (identifier == null) {
				yield break;
			}
			var statement = context.GetNode<Statement>();
			if (statement == null) {
				yield break;
			}

			if (!(context.Resolve(identifier).IsError)) {
				yield break;
			}
			var guessedType = CreateFieldAction.GuessAstType(context, identifier);
			if (guessedType == null) {
				yield break;
			}

			yield return new CodeAction(context.TranslateString("Create field"), script => {
				var decl = new FieldDeclaration() {
					ReturnType = guessedType,
					Variables = { new VariableInitializer(identifier.Identifier) }
				};
				script.InsertWithCursor(context.TranslateString("Create field"), decl, Script.InsertPosition.Before);
			});
		}

		#region Type guessing
		static int GetArgumentIndex(InvocationExpression invoke, AstNode parameter)
		{
			int argumentNumber = 0;
			foreach (var arg in invoke.Arguments) {
				if (arg == parameter) {
					return argumentNumber;
				}
				argumentNumber++;
			}
			return -1;
		}

		static IEnumerable<IType> GetAllValidTypesFromInvokation(RefactoringContext context, InvocationExpression invoke, AstNode parameter)
		{
			int index = GetArgumentIndex(invoke, parameter);
			if (index < 0) {
				yield break;
			}
					
			var targetResult = context.Resolve(invoke.Target);
			if (targetResult is MethodGroupResolveResult) {
				foreach (var method in ((MethodGroupResolveResult)targetResult).Methods) {
					if (index < method.Parameters.Count) {
						yield return method.Parameters [index].Type;
					}
				}
			}
		}

		internal static IEnumerable<IType> GetValidTypes(RefactoringContext context, Expression expr)
		{
			if (expr.Parent is DirectionExpression) {
				var parent = expr.Parent.Parent;
				if (parent is InvocationExpression) {
					var invoke = (InvocationExpression)parent;
					return GetAllValidTypesFromInvokation(context, invoke, expr.Parent);
				}
			}

			if (expr.Parent is InvocationExpression) {
				var parent = expr.Parent;
				if (parent is InvocationExpression) {
					var invoke = (InvocationExpression)parent;
					return GetAllValidTypesFromInvokation(context, invoke, expr);
				}
			}
			
			if (expr.Parent is VariableInitializer) {
				var initializer = (VariableInitializer)expr.Parent;
				return new [] { context.Resolve(initializer).Type };
			}
			
			if (expr.Parent is CastExpression) {
				var cast = (CastExpression)expr.Parent;
				return new [] { context.Resolve(cast.Type).Type };
			}
			
			if (expr.Parent is AsExpression) {
				var cast = (AsExpression)expr.Parent;
				return new [] { context.Resolve(cast.Type).Type };
			}

			if (expr.Parent is AssignmentExpression) {
				var assign = (AssignmentExpression)expr.Parent;
				var other = assign.Left == expr ? assign.Right : assign.Left;
				return new [] { context.Resolve(other).Type };
			}

			if (expr.Parent is BinaryOperatorExpression) {
				var assign = (BinaryOperatorExpression)expr.Parent;
				var other = assign.Left == expr ? assign.Right : assign.Left;
				return new [] { context.Resolve(other).Type };
			}
			
			if (expr.Parent is ReturnStatement) {
				var state = context.GetResolverStateBefore(expr);
				if (state != null) {
					return new [] { state.CurrentMember.ReturnType };
				}
			}

			if (expr.Parent is YieldReturnStatement) {
				var state = context.GetResolverStateBefore(expr);
				if (state != null && (state.CurrentMember.ReturnType is ParameterizedType)) {
					var pt = (ParameterizedType)state.CurrentMember.ReturnType;
					if (pt.FullName == "System.Collections.Generic.IEnumerable") {
						return new [] { pt.TypeArguments.First () };
					}
				}
			}

			return Enumerable.Empty<IType>();
		}
		static readonly IType[] emptyTypes = new IType[0];
		internal static AstType GuessAstType(RefactoringContext context, Expression expr)
		{
			var type = GetValidTypes(context, expr).ToArray();
			var inferedType = new TypeInference(context.Compilation).FindTypeInBounds(type, emptyTypes);
			if (inferedType.Kind == TypeKind.Unknown) {
				return new PrimitiveType("object");
			}
			return context.CreateShortType(inferedType);
		}

		internal static IType GuessType(RefactoringContext context, Expression expr)
		{
			var type = GetValidTypes(context, expr).ToArray();
			var inferedType = new TypeInference(context.Compilation).FindTypeInBounds(type, emptyTypes);
			return inferedType;

		}
		#endregion
	}
}

