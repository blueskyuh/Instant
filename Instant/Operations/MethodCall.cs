﻿//
// MethodCall.cs
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

using System.Collections.Generic;

namespace Instant.Operations
{
	public class MethodCall
		: OperationContainer
	{
		public MethodCall (int id, string name, IEnumerable<StateChange> arguments)
			: base (id)
		{
			MethodName = name;
			Arguments = arguments;
		}

		public string MethodName
		{
			get;
			private set;
		}

		public IEnumerable<StateChange> Arguments
		{
			get;
			private set;
		}
	}
}
