//
// MainWindowViewModel.cs
//
// Copyright 2012 Eric Maupin
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cadenza;
using ICSharpCode.NRefactory.TypeSystem;

namespace Instant.Standalone
{
	public class MainWindowViewModel
		: INotifyPropertyChanged
	{
		public MainWindowViewModel()
		{
			this.evaluator.EvaluationCompleted += OnEvalCompleted;
			this.evaluator.Start();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public string Input
		{
			get { return this.input; }
			set
			{
				if (this.input == value)
					return;

				this.input = value;
				OnPropertyChanged ("Input");
				ProcessInput();
			}
		}

		public string Debug
		{
			get { return this.debug; }
			private set
			{
				if (this.debug == value)
					return;

				this.debug = value;
				OnPropertyChanged ("Debug");
			}
		}

		public string Output
		{
			get { return this.output; }
			private set
			{
				if (this.output == value)
					return;

				this.output = value;
				OnPropertyChanged ("Output");
			}
		}

		public string Status
		{
			get { return this.status; }
			private set
			{
				if (this.status == value)
					return;

				this.status = value;
				OnPropertyChanged ("Status");
			}
		}

		public bool DebugTree
		{
			get { return this.showDebugTree; }
			set
			{
				if (this.showDebugTree == value)
					return;

				this.showDebugTree = value;
				OnPropertyChanged ("DebugTree");

				if (value)
					ProcessInput();
			}
		}

		public bool IdentTree
		{
			get { return this.showIdentTree; }
			set
			{
				if (this.showIdentTree == value)
					return;

				this.showIdentTree = value;
				OnPropertyChanged ("IdentTree");

				if (value)
					ProcessInput();
			}
		}

		public bool ShowCompilerErrors
		{
			get { return this.showCompilerErrors; }
			set
			{
				if (this.showCompilerErrors == value)
					return;

				this.showCompilerErrors = value;
				OnPropertyChanged ("ShowCompilerErrors");

				if (value)
					ProcessInput();
			}
		}

		public MethodCall RootCall
		{
			get { return this.rootCall; }
			set
			{
				if (this.rootCall == value)
					return;

				this.rootCall = value;
				OnPropertyChanged ("RootCall");
			}
		}

		public double FontSize
		{
			get { return this.fontSize; }
			set
			{
				if (this.fontSize == value)
					return;

				this.fontSize = value;
				OnPropertyChanged ("FontSize");
			}
		}

		public string TestCode
		{
			get { return this.testCode; }
			set
			{
				if (this.testCode == value)
					return;

				this.testCode = value;
				OnPropertyChanged ("TestCode");
			}
		}

		private bool showDebugTree, showCompilerErrors, showIdentTree;
		private string input, output, debug = "Initializing";

		private void OnPropertyChanged (string property)
		{
			var changed = this.PropertyChanged;
			if (changed != null)
				changed (this, new PropertyChangedEventArgs (property));
		}

		private string lastOutput = String.Empty;
		private MethodCall rootCall;
		private string status;
		private double fontSize = 16;
		private string testCode;

		private Submission submission;

		private readonly Evaluator evaluator = new Evaluator();

		private int submissionId;

		private void OnEvalCompleted (object sender, EvaluationCompletedEventArgs e)
		{
			MemoryInstrumentationSink sink = (MemoryInstrumentationSink)e.Submission.Sink;

			var methods = sink.GetRootCalls();
			if (methods == null || methods.Count == 0)
				return;

			RootCall = methods.Values.First();
			Status = null;
		}

		private async void ProcessInput()
		{
			var source = Interlocked.Exchange (ref this.submission, null);

			if (source != null)
				source.Cancel();

			if (String.IsNullOrEmpty (input) || String.IsNullOrEmpty (TestCode))
				return;

			int id = Interlocked.Increment (ref this.submissionId);

			Either<string, Error> result = await Instantly.Instrument (input, id);
			string instrumented = result.Fold (i => i, e => null);
			if (instrumented == null)
				return;

			Project project = new Project();
			project.Sources.Add (Either<FileInfo, string>.B (instrumented));

			Submission s = null;
			var sink = new MemoryInstrumentationSink (() => s.IsCanceled);
			s = new Submission (id, project, sink, TestCode);

			if (DebugTree)
				Debug = instrumented;

			this.evaluator.PushSubmission (s);
		}
	}
}
