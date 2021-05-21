﻿using System;
using System.Text;

namespace CodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(new ClosureGenerator().Generate());
        }
    }

    class ClosureGenerator
    {
        private readonly StringBuilder _codeBuilder = new StringBuilder();
        private int _indent;
        private int _column;

        public void Indent()
        {
            _indent++;
        }

        public void Unindent()
        {
            _indent--;
        }

        public string Generate()
        {
            Write($@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:{Environment.Version}
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------");
            WriteLine("");
            WriteLine("");

            for (int arity = 0; arity <= 1; arity++)
            {
                GenerateDelegateClosure(arity, hasReturnType: false);
                GenerateDelegateClosure(arity, hasReturnType: true);
                GenerateTypeOnlyDelegateClosure(arity, hasReturnType: false);
                GenerateTypeOnlyDelegateClosure(arity, hasReturnType: true);
            }

            return _codeBuilder.ToString();
        }

        private void GenerateTypeOnlyDelegateClosure(int arity, bool hasReturnType = true)
        {
            var typeName = hasReturnType ? "TypeOnlyFuncDelegateClosure" : "TypeOnlyActionDelegateClosure";

            Write($"sealed class {typeName}<");
            for (int j = 0; j < arity; j++)
            {
                if (j > 0)
                {
                    Write(", ");
                }
                Write($"T{j}");
            }
            if (hasReturnType)
            {
                if (arity == 0)
                {
                    Write("R");
                }
                else
                {
                    Write(", R");
                }
            }
            Write("> : RequestDelegateClosure");
            WriteLine();
            WriteLine("{");
            Indent();
            Write("public override bool HasBody => ");
            for (int j = 0; j < arity; j++)
            {
                if (j > 0)
                {
                    Write(" || ");
                }
                Write($"ParameterBinder<T{j}>.HasBodyBasedOnType");
            }
            if (arity == 0)
            {
                Write("false");
            }
            Write(";");
            WriteLine();
            WriteLine();
            for (int j = 0; j < arity; j++)
            {
                WriteLine($"private readonly string _name{j};");
            }
            if (hasReturnType)
            {
                WriteLine("private readonly ResultInvoker<R> _resultInvoker;");
            }
            Write("private readonly ");
            WriteFuncOrActionType(arity, hasReturnType);

            WriteLine(" _delegate;");
            WriteLine();
            Write($"public {typeName}(");
            WriteFuncOrActionType(arity, hasReturnType);

            WriteLine(" @delegate, ParameterInfo[] parameters)");
            WriteLine("{");
            Indent();
            WriteLine("_delegate = @delegate;");
            if (hasReturnType)
            {
                WriteLine("_resultInvoker = ResultInvoker<R>.Create();");
            }
            for (int j = 0; j < arity; j++)
            {
                WriteLine($"_name{j} = parameters[{j}].Name!;");
            }
            Unindent();
            WriteLine("}"); //ctor

            WriteLine();
            WriteLine("public override Task ProcessRequestAsync(HttpContext httpContext)");
            WriteLine("{");
            Indent();
            for (int j = 0; j < arity; j++)
            {
                WriteLine($"if (!ParameterBinder<T{j}>.TryBindValueBasedOnType(httpContext, _name{j}, out var arg{j}))");
                WriteLine("{");
                Indent();
                WriteLine($"ParameterLog.ParameterBindingFailed<T{j}>(httpContext, _name{j});");
                WriteLine("httpContext.Response.StatusCode = 400;");
                WriteLine("return Task.CompletedTask;");
                Unindent();
                WriteLine("}");
                WriteLine();
            }

            WriteDelegateCall(arity, hasReturnType);

            WriteLine();

            if (hasReturnType)
            {
                WriteLine("return _resultInvoker.Invoke(httpContext, result);");
            }
            else
            {
                WriteLine("return Task.CompletedTask;");
            }

            Unindent();
            WriteLine("}"); // ProcessRequestAsync

            WriteLine();
            WriteLine("public override async Task ProcessRequestWithBodyAsync(HttpContext httpContext)");
            WriteLine("{");
            Indent();

            if (arity > 0)
            {
                WriteLine("var success = false;");
            }

            for (int j = 0; j < arity; j++)
            {
                WriteLine($"(T{j}? arg{j}, success) = await ParameterBinder<T{j}>.BindBodyBasedOnType(httpContext, _name{j});");
                WriteLine();
                WriteLine("if (!success)");
                WriteLine("{");
                Indent();
                WriteLine($"ParameterLog.ParameterBindingFailed<T{j}>(httpContext, _name{j});");
                WriteLine("httpContext.Response.StatusCode = 400;");
                WriteLine("return;");
                Unindent();
                WriteLine("}");
                WriteLine();
            }

            WriteDelegateCall(arity, hasReturnType);

            if (hasReturnType)
            {
                WriteLine();
                WriteLine("await _resultInvoker.Invoke(httpContext, result);");
            }

            Unindent();
            WriteLine("}");

            Unindent();
            WriteLine("}");
            WriteLine();
        }

        private void WriteDelegateCall(int arity, bool hasReturnType)
        {
            if (hasReturnType)
            {
                Write("R? result = _delegate(");
            }
            else
            {
                Write("_delegate(");
            }
            for (int j = 0; j < arity; j++)
            {
                if (j > 0)
                {
                    Write(", ");
                }
                Write($"arg{j}!");
            }
            WriteLine(");");
        }

        private void WriteFuncOrActionType(int arity, bool hasReturnType)
        {
            if (hasReturnType)
            {
                Write("Func<");
            }
            else
            {
                Write("Action");
                if (arity > 0)
                {
                    Write("<");
                }
            }
            for (int j = 0; j < arity; j++)
            {
                if (j > 0)
                {
                    Write(", ");
                }
                Write($"T{j}");
            }

            if (hasReturnType)
            {
                if (arity == 0)
                {
                    Write("R>");
                }
                else
                {
                    Write(", R>");
                }
            }
            else
            {
                if (arity > 0)
                {
                    Write(">");
                }
            }
        }

        private void GenerateDelegateClosure(int arity, bool hasReturnType = false)
        {
            var typeName = hasReturnType ? "FuncDelegateClosure" : "ActionDelegateClosure";

            Write($"sealed class {typeName}<");
            for (int j = 0; j < arity; j++)
            {
                if (j > 0)
                {
                    Write(", ");
                }
                Write($"T{j}");
            }
            if (hasReturnType)
            {
                if (arity == 0)
                {
                    Write("R");
                }
                else
                {
                    Write(", R");
                }
            }
            Write("> : RequestDelegateClosure");
            WriteLine();
            WriteLine("{");
            Indent();
            Write("public override bool HasBody => ");
            for (int j = 0; j < arity; j++)
            {
                if (j > 0)
                {
                    Write(" || ");
                }
                Write($"_parameterBinder{j}.IsBody");
            }
            if (arity == 0)
            {
                Write("false");
            }
            Write(";");
            WriteLine();
            WriteLine();
            for (int j = 0; j < arity; j++)
            {
                WriteLine($"private readonly ParameterBinder<T{j}> _parameterBinder{j};");
            }
            if (hasReturnType)
            {
                WriteLine("private readonly ResultInvoker<R> _resultInvoker;");
            }
            Write("private readonly ");
            WriteFuncOrActionType(arity, hasReturnType);
            WriteLine(" _delegate;");
            WriteLine();
            Write($"public {typeName}(");
            WriteFuncOrActionType(arity, hasReturnType);
            WriteLine(" @delegate, ParameterInfo[] parameters)");
            WriteLine("{");
            Indent();
            WriteLine("_delegate = @delegate;");
            
            if (hasReturnType)
            {
                WriteLine("_resultInvoker = ResultInvoker<R>.Create();");
            }
            
            for (int j = 0; j < arity; j++)
            {
                WriteLine($"_parameterBinder{j} = ParameterBinder<T{j}>.Create(parameters[{j}]);");
            }
            Unindent();
            WriteLine("}"); //ctor

            WriteLine();
            WriteLine("public override Task ProcessRequestAsync(HttpContext httpContext)");
            WriteLine("{");
            Indent();
            for (int j = 0; j < arity; j++)
            {
                WriteLine($"if (!_parameterBinder{j}.TryBindValue(httpContext, _name{j}, out var arg{j}))");
                WriteLine("{");
                Indent();
                WriteLine($"ParameterLog.ParameterBindingFailed(httpContext, _parameterBinder{j});");
                WriteLine("httpContext.Response.StatusCode = 400;");
                WriteLine("return Task.CompletedTask;");
                Unindent();
                WriteLine("}");
                WriteLine();
            }

            WriteDelegateCall(arity, hasReturnType);

            WriteLine();

            if (hasReturnType)
            {
                WriteLine("return _resultInvoker.Invoke(httpContext, result);");
            }
            else
            {
                WriteLine("return Task.CompletedTask;");
            }

            Unindent();
            WriteLine("}"); // ProcessRequestAsync

            WriteLine();
            WriteLine("public override async Task ProcessRequestWithBodyAsync(HttpContext httpContext)");
            WriteLine("{");
            Indent();

            if (arity > 0)
            {
                WriteLine("var success = false;");
            }

            for (int j = 0; j < arity; j++)
            {
                WriteLine($"(T{j}? arg{j}, success) = await _parameterBinder{j}.BindBodyOrValueAsync(httpContext);");
                WriteLine();
                WriteLine("if (!success)");
                WriteLine("{");
                Indent();
                WriteLine($"ParameterLog.ParameterBindingFailed(httpContext, _parameterBinder{j});");
                WriteLine("httpContext.Response.StatusCode = 400;");
                WriteLine("return;");
                Unindent();
                WriteLine("}");
                WriteLine();
            }

            WriteDelegateCall(arity, hasReturnType);

            if (hasReturnType)
            {
                WriteLine();
                WriteLine("await _resultInvoker.Invoke(httpContext, result);");
            }

            Unindent();
            WriteLine("}");

            Unindent();
            WriteLine("}");
            WriteLine();
        }

        private void WriteLine()
        {
            WriteLine("");
        }

        private void WriteLineNoIndent(string value)
        {
            _codeBuilder.AppendLine(value);
        }

        private void WriteNoIndent(string value)
        {
            _codeBuilder.Append(value);
        }

        private void Write(string value)
        {
            if (_indent > 0 && _column == 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.Append(value);
            _column += value.Length;
        }

        private void WriteLine(string value)
        {
            if (_indent > 0 && _column == 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.AppendLine(value);
            _column = 0;
        }
    }
}
