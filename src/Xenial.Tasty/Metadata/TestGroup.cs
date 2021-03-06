﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xenial.Delicious.Execution;

namespace Xenial.Delicious.Metadata
{
    [System.Diagnostics.DebuggerDisplay("Name: {Name} Count: {Executors.Count}")]
    public class TestGroup : IExecutable, IForceAble
    {
        public string Name { get; internal set; } = string.Empty;
        internal List<IExecutable> Executors { get; } = new List<IExecutable>();
        public Executable Executor { get; internal set; } = () => Task.FromResult(true);
        public TestOutcome TestOutcome { get; internal set; }
        public TestGroup? ParentGroup { get; internal set; }
        public TimeSpan Duration { get; internal set; }
        internal List<IExecutable> BeforeEachHooks { get; } = new List<IExecutable>();
        internal List<IExecutable> AfterEachHooks { get; } = new List<IExecutable>();
        public Func<bool>? IsForced { get; set; }
        public Exception? Exception { get; internal set; }

        public string FullName =>
            ParentGroup == null
            ? Name
            : $"{ParentGroup.FullName} {Name}";
    }
}
