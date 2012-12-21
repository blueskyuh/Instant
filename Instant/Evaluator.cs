﻿//
// Evaluator.cs
//
// Copyright 2012 Eric Maupin
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp;

namespace Instant
{
	public class EvaluationCompletedEventArgs
		: EventArgs
	{
		public EvaluationCompletedEventArgs (Submission submission)
		{
			if (submission == null)
				throw new ArgumentNullException ("submission");

			Submission = submission;
		}

		public EvaluationCompletedEventArgs (Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException ("exception");

			Exception = exception;
		}

		public Exception Exception
		{
			get;
			private set;
		}

		public Submission Submission
		{
			get;
			private set;
		}
	}

	public sealed class Evaluator
		: IDisposable
	{
		public event EventHandler<EvaluationCompletedEventArgs> EvaluationCompleted;

		public void Start()
		{
			this.running = true;
			new Thread (EvaluatorRunner)
			{
				Name = "Evaluator"
			}.Start();
		}

		public void PushSubmission (Submission submission)
		{
			Submission bumped = Interlocked.Exchange (ref this.nextSubmission, submission);
			if (bumped != null)
				bumped.Cancel();

			this.submissionWait.Set();
		}

		public void Dispose()
		{
			this.running = false;
			this.submissionWait.Dispose();
		}

		private bool running;

		private Submission nextSubmission;
		private readonly AutoResetEvent submissionWait = new AutoResetEvent (false);

		private readonly string RefPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles), "Reference Assemblies");

		static Evaluator()
		{
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				if (args.Name.StartsWith ("Instant"))
					return typeof (Instantly).Assembly;

				return args.RequestingAssembly;
			};
		}

		private void EvaluatorRunner()
		{
			while (this.running)
			{
				this.submissionWait.WaitOne();

				Submission next = Interlocked.Exchange (ref this.nextSubmission, null);
				if (next == null)
					continue;

				AppDomain evalDomain = null;
				try
				{
					AppDomainSetup setup = new AppDomainSetup
					{
						ApplicationBase = GetInstantDir(),
						LoaderOptimization = LoaderOptimization.MultiDomainHost
					};
					evalDomain = AppDomain.CreateDomain ("Instant Evaluation", null, setup);

					bool error = false;

					string[] references = next.Project.References.ToArray();
					for (int i = 0; i < references.Length; i++)
					{
						// HACK: We don't need to copy reference assemblies.
						if (references[i].StartsWith (RefPath))
							continue;

						string reference = Path.Combine (setup.ApplicationBase, Path.GetFileName (references[i]));

						if (!File.Exists (references[i]))
						{
							error = true;
							break;
						}

						File.Copy (references[i], reference);

						references[i] = reference;
					}

					if (error)
						continue;

					var cparams = new CompilerParameters();
					cparams.OutputAssembly = Path.Combine (setup.ApplicationBase, Path.GetRandomFileName());
					cparams.GenerateInMemory = false;
					cparams.IncludeDebugInformation = false;
					cparams.ReferencedAssemblies.AddRange (references);
					cparams.ReferencedAssemblies.Add (typeof (Instantly).Assembly.Location);
					cparams.CompilerOptions = next.Project.GetCompilerOptions();

					// HACK: Wrap test code into a proper method
					string evalSource = "namespace Instant.User { static class Evaluation { static void Evaluate() {" + next.EvalCode + " } } }";
 
					List<string> sources = next.Project.Sources.AsParallel().Select (
							e => e.Fold (async f => await f.OpenText().ReadToEndAsync(), Task.FromResult)
						).ToListAsync().Result;

					sources.Add (evalSource);

					CSharpCodeProvider provider = new CSharpCodeProvider();
					CompilerResults results = provider.CompileAssemblyFromSource (cparams, sources.ToArray());
					if (results.Errors.HasErrors)
						continue;

					DomainEvaluator domainEvaluator = (DomainEvaluator)evalDomain.CreateInstanceAndUnwrap ("Instant", "Instant.Evaluator+DomainEvaluator");
					Exception ex = domainEvaluator.Evaluate (next, cparams, sources.ToArray());
					if (ex == null)
						OnEvaluationCompleted (new EvaluationCompletedEventArgs (next));
					else
						OnEvaluationCompleted (new EvaluationCompletedEventArgs (ex));
				}
				finally
				{
					if (evalDomain != null)
					{
						string dir = evalDomain.BaseDirectory;
						AppDomain.Unload (evalDomain);
						try
						{
							Directory.Delete (dir, true);
						}
						catch (UnauthorizedAccessException)
						{
							// Every now and then we can't delete the directory.
							// We should try to find out why and fix it,
							// but in the mean time it's not the end of the world.
							Trace.WriteLine ("Unable to delete Instant dir " + dir);
						}
					}
				}
			}
		}

		private class DomainEvaluator
			: MarshalByRefObject
		{
			public Exception Evaluate (Submission submission, CompilerParameters cparams, string[] sources)
			{
				CSharpCodeProvider provider = new CSharpCodeProvider();
				CompilerResults results = provider.CompileAssemblyFromSource (cparams, sources);
				if (results.Errors.HasErrors)
					return null;

				MethodInfo method = results.CompiledAssembly.GetType ("Instant.User.Evaluation").GetMethod ("Evaluate", BindingFlags.NonPublic | BindingFlags.Static);
				if (method == null)
					return new InvalidOperationException ("Evaluation method not found");

				Hook.LoadSubmission (submission);

				try
				{
					method.Invoke (null, null);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					return ex;
				}

				return null;
			}
		}

		private static string GetInstantDir()
		{
			string temp = Path.GetTempPath();
			string path = Path.Combine (temp, Path.GetRandomFileName());

			bool created = false;
			while (!created)
			{
				try
				{
					Directory.CreateDirectory (path);
					created = true;
				}
				catch (IOException)
				{
					path = Path.Combine (temp, Path.GetRandomFileName());
				}
			}

			File.Copy (typeof (Instantly).Assembly.Location, Path.Combine (path, "Instant.dll"));

			return path;
		}

		private void OnEvaluationCompleted (EvaluationCompletedEventArgs e)
		{
			var handler = this.EvaluationCompleted;
			if (handler != null)
				handler (this, e);
		}
	}
}