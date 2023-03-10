//
// Hook.cs
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

namespace Instant
{
	public static class Hook
	{
		public static void LoadSubmission (Submission newSubmission)
		{
			if (newSubmission == null)
				throw new ArgumentNullException ("newSubmission");

			submission = newSubmission;
		}

		public static void BeginLoop (int submissionId, int id)
		{
			var sink = GetSink (submissionId);
			sink.BeginLoop (id);
		}

		public static void BeginInsideLoop (int submissionId, int id)
		{
			var s = GetSubmission (submissionId);
			if (s.IsCanceled)
			{
				EndLoop (submissionId, id);
				throw new OperationCanceledException();
			}

			s.Sink.BeginInsideLoop (submissionId);
		}

		public static void EndInsideLoop (int submissionId, int id)
		{
			var s = GetSubmission (submissionId);
			s.Sink.EndInsideLoop (id);

			if (s.IsCanceled)
			{
				EndLoop (submissionId, id);
				throw new OperationCanceledException();
			}
		}

		public static void EndLoop (int submissionId, int id)
		{
			var sink = GetSink (submissionId);
			sink.EndLoop (id);
		}

		public static void LogReturn (int submissionId, int id)
		{
			var sink = GetSink (submissionId);
			sink.LogReturn (id);
		}

		public static T LogReturn<T> (int submissionId, int id, T value)
		{
			var sink = GetSink (submissionId);
			sink.LogReturn (id, Display.Object (value));

			return value;
		}

		public static T LogObject<T> (int submissionId, int id, string name, T value)
		{
			var sink = GetSink (submissionId);
			sink.LogVariableChange (id, name, Display.Object (value));

			return value;
		}
		
		public static T LogPostfix<T> (int submissionId, int id, T expression, string name, T newValue)
		{
			var sink = GetSink (submissionId);
			sink.LogVariableChange (id, name, Display.Object (newValue));

			return expression;
		}

		public static void LogEnterMethod (int submissionId, int id, string name, params StateChange[] arguments)
		{
			var s = GetSubmission (submissionId);

			if (s.IsCanceled)
				throw new OperationCanceledException();

			s.Sink.LogEnterMethod (id, name, arguments);
		}

		private static Submission submission;

		private static Submission GetSubmission (int submissionId)
		{
			Submission s = submission;
			if (s == null || s.SubmissionId != submissionId)
				throw new OperationCanceledException();

			return s;
		}

		private static IInstrumentationSink GetSink (int submissionId)
		{
			return GetSubmission (submissionId).Sink;
		}
	}
}