//
// Submission.cs
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

namespace Instant
{
	public class Submission
	{
		public Submission (int submissionId, IInstrumentationSink instrumentationSink)
		{
			SubmissionId = submissionId;
			Sink = instrumentationSink;
		}

		public int SubmissionId
		{
			get;
			private set;
		}

		public IInstrumentationSink Sink
		{
			get;
			private set;
		}

		public bool IsCanceled
		{
			get;
			private set;
		}

		public void Cancel()
		{
			IsCanceled = true;
		}
	}

	public static class SubmissionExtensions
	{
		public static void ThrowIfCancellationRequested (this Submission self)
		{
			if (self == null)
				throw new ArgumentNullException ("self");

			if (self.IsCanceled)
				throw new OperationCanceledException();
		}
	}
}