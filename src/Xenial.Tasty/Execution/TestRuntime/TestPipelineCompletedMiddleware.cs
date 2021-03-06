﻿
namespace Xenial.Delicious.Execution.TestRuntime
{
    public static class TestPipelineCompletedMiddleware
    {
        public static TestExecutor UseTestPipelineCompleted(this TestExecutor executor)
            => executor.UseRuntime(async (context, next) =>
            {
                try
                {
                    await next();
                }
                finally
                {
                    if (context.Remote != null && !context.EndPipeLine)
                    {
                        await context.Remote.SignalTestPipelineCompleted();
                    }
                }
            });
    }
}
