﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace Xenial.Delicious.Execution
{
    public delegate Task TestDelegate(TestExecutionContext context);

    public delegate Task TestGroupDelegate(TestGroupContext context);
}
