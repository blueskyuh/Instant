﻿//
// LoopViewModel.cs
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
using System.Linq;
using System.Windows.Input;

namespace Instant.VisualStudio
{
	public class IterationChangedEventArgs
		: EventArgs
	{
		public IterationChangedEventArgs (LoopIteration oldValue, LoopIteration newValue)
		{
			PreviousIteration = oldValue;
			NewIteration = newValue;
		}

		public LoopIteration PreviousIteration
		{
			get;
			private set;
		}

		public LoopIteration NewIteration
		{
			get;
			private set;
		}
	}

	public class LoopViewModel
		: OperationViewModel
	{
		public LoopViewModel()
		{
			this.adjustIteration = new DelegatedCommand (Adjust, CanAdjust);
		}

		public event EventHandler<IterationChangedEventArgs> IterationChanged;

		public Loop Loop
		{
			get { return (Loop)Operation; }
		}

		public ICommand AdjustIteration
		{
			get { return this.adjustIteration; }
		}

		public LoopIteration[] Iterations
		{
			get { return this.iterations; }
		}

		public int Iteration
		{
			get { return this.iteration; }
			set
			{
				if (this.iteration == value)
					return;

				int old = this.iteration;
				this.iteration = value;
				OnPropertyChanged ("Iteration");
				OnIterationChanged (new IterationChangedEventArgs (this.iterations[old - 1], this.iterations[value - 1]));
				this.adjustIteration.ChangeCanExecute();
			}
		}

		public int TotalIterations
		{
			get { return this.iterations.Length; }
		}

		private readonly DelegatedCommand adjustIteration;
		private LoopIteration[] iterations;
		private int iteration = 1;

		protected override void OnOperationChanged()
		{
			base.OnOperationChanged();

			LoopIteration piteration = (this.iterations != null && this.iteration > 0) ? this.iterations[this.iteration - 1] : null;
			this.iterations = Loop.Operations.OfType<LoopIteration>().ToArray();

			this.iteration = this.iterations.Length;
			OnPropertyChanged ("Iteration");

			LoopIteration niteration = (this.iterations.Length > 0) ? this.iterations[this.iterations.Length - 1] : null;
			OnIterationChanged (new IterationChangedEventArgs (piteration, niteration));

			OnPropertyChanged ("TotalIterations");
			this.adjustIteration.ChangeCanExecute();
		}

		private bool CanAdjust (object o)
		{
			int amount = (int)o;
			int realized = Iteration + amount;

			return (realized > 0 && realized <= this.iterations.Length);
		}

		private void Adjust (object o)
		{
			int amount = (int)o;
			Iteration += amount;
		}

		private void OnIterationChanged (IterationChangedEventArgs e)
		{
			var handler = this.IterationChanged;
			if (handler != null)
				handler (this, e);
		}
	}
}
