﻿using System;
using System.Reflection;
using System.CodeDom.Compiler;
using SenseNet.ContentRepository.Storage.Scripting;

namespace SenseNet.ContentRepository
{

	[ScriptTagName("jScript")]
	public class JscriptEvaluator : IEvaluator
	{
		public static readonly string TagName = "jScript";
		private static Type _jsEvaluatorType;
	    private static Type JsEvaluatorType
	    {
	        get
	        {
                if (_jsEvaluatorType == null)
                    CreateJsEvaluatorAndType();

	            return _jsEvaluatorType;
	        }
	    }

		private static void CreateJsEvaluatorAndType()
		{
            using (var op = SenseNet.Diagnostics.SnTrace.Repository.StartOperation("CreateJsEvaluatorAndType"))
            {
                var jsCodeProvider = new Microsoft.JScript.JScriptCodeProvider();
                var compilerParam = new CompilerParameters();
                compilerParam.ReferencedAssemblies.Add("System.dll");
                compilerParam.ReferencedAssemblies.Add("System.Data.dll");
                compilerParam.ReferencedAssemblies.Add("System.Xml.dll");
                compilerParam.ReferencedAssemblies.Add("System.Web.dll");

                compilerParam.CompilerOptions = "/t:library";
                compilerParam.GenerateInMemory = true;

                string JScriptSource = @"import System;
            import System.Web;
            
            package Evaluator
            {
                class JsEvaluator
                {
					public function WhatIsTheAnswerToLifeTheUniverseAndEverything()
					{
						return 42;
					}
                    public function Eval(expr : String) : String
                    {
                        var result = eval(expr, ""unsafe"");
						if (typeof(result) != ""date"")
							return result;

						var d = new Date(result);
						return d.getUTCFullYear() + ""-"" + (d.getUTCMonth()+1) + ""-"" + d.getUTCDate() + "" "" + 
							d.getUTCHours() + "":"" + d.getUTCMinutes() + "":"" + d.getUTCSeconds();
                    }
                }
            }";

                CompilerResults compilerResult = jsCodeProvider.CompileAssemblyFromSource(compilerParam, JScriptSource);

                if (compilerResult.Errors.Count > 0)
                {
                    throw new ApplicationException($"Compiling JScript code failed and threw the exception: {compilerResult.Errors[0].ErrorText}");
                }

                var assembly = compilerResult.CompiledAssembly;

                SenseNet.Diagnostics.SnTrace.Repository.Write("JsEvaluator assembly compiled: FullName:{0}, CodeBase:{1}, Location:{2}",
                    assembly.FullName, assembly.CodeBase, assembly.Location);

                _jsEvaluatorType = assembly.GetType("Evaluator.JsEvaluator");

                op.Successful = true;
            }
		}

        public static void Init()
        {
            var jset = JsEvaluatorType;
        }

		public string Evaluate(string source)
		{
            var jsEvaluator = Activator.CreateInstance(JsEvaluatorType);
            var result = JsEvaluatorType.InvokeMember("Eval", BindingFlags.InvokeMethod, null, jsEvaluator, new object[] { source }).ToString();
			return result;
		}
	}
}