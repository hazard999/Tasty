﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Xenial.Delicious.Metadata;
using Xenial.Delicious.Utils;

using static Xenial.Delicious.Utils.Actions;

namespace Xenial.Delicious.Reporters
{
    public static class ConsoleReporter
    {
        public static ColorScheme Scheme = ColorScheme.Default;

        static ConsoleReporter()
            => SetupConsoleEncoding();

        private static void SetupConsoleEncoding()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //Register additional code pages for windows
                //cause we deal directly with process streams
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }

            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        public static void Register()
            => Tasty.RegisterReporter(Report)
                    .RegisterReporter(ReportSummary);

        static Lazy<int> SeparatorSize = new Lazy<int>(() =>
        {
            const int fallBackSize = 100;
            try
            {
                return Console.BufferWidth;
            }
            catch (IOException) { /* Handle is invalid */ }
            return fallBackSize;
        });


        public static Task ReportSummary(TestSummary summary)
        {
            var totalTests = summary.Count;
            var failedTests = summary[TestOutcome.Failed].Count;
            var ignoredTests = summary[TestOutcome.Ignored].Count;
            var notRunTests = summary[TestOutcome.NotRun].Count;
            var successTests = summary[TestOutcome.Success].Count;
            var outcome = summary.OutCome;

            var totalTime = summary.Duration;
            var failedTime = summary[TestOutcome.Failed].Duration;
            var ignoredTime = summary[TestOutcome.Ignored].Duration;
            var notRunTime = summary[TestOutcome.NotRun].Duration;
            var successTime = summary[TestOutcome.Success].Duration;

            var totalTimeString = totalTime.AsDuration();

            Console.WriteLine();
            Console.WriteLine(new string('=', SeparatorSize.Value));

            Write(Scheme.DefaultColor, $"Summary: ");
            Write(failedTests > 0 ? Scheme.ErrorColor : Scheme.DefaultColor, $"F{failedTests}".PadLeft(totalTimeString.Length));
            Write(Scheme.DefaultColor, $" | ");
            Write(ignoredTests > 0 ? Scheme.WarningColor : Scheme.DefaultColor, $"I{ignoredTests}".PadLeft(totalTimeString.Length));
            Write(Scheme.DefaultColor, $" | ");
            Write(notRunTests > 0 ? Scheme.NotifyColor : Scheme.DefaultColor, $"NR{notRunTests}".PadLeft(totalTimeString.Length));
            Write(Scheme.DefaultColor, $" | ");
            Write(successTests > 0 ? Scheme.SuccessColor : Scheme.DefaultColor, $"S{successTests}".PadLeft(totalTimeString.Length));
            Write(Scheme.DefaultColor, $" | ");
            Write(Scheme.DefaultColor, $"T{totalTests}");

            Console.WriteLine();
            Write(Scheme.DefaultColor, $"Time:    ");
            Write(failedTests > 0 ? Scheme.ErrorColor : Scheme.DefaultColor, failedTime.AsDuration());
            Write(Scheme.DefaultColor, $" | ");
            Write(ignoredTests > 0 ? Scheme.WarningColor : Scheme.DefaultColor, ignoredTime.AsDuration());
            Write(Scheme.DefaultColor, $" | ");
            Write(notRunTests > 0 ? Scheme.NotifyColor : Scheme.DefaultColor, notRunTime.AsDuration());
            Write(Scheme.DefaultColor, $" | ");
            Write(successTests > 0 ? Scheme.SuccessColor : Scheme.DefaultColor, successTime.AsDuration());
            Write(Scheme.DefaultColor, $" | ");
            Write(Scheme.DefaultColor, totalTimeString);

            Console.WriteLine();
            Write(Scheme.DefaultColor, $"Outcome: ");
            Write(
                failedTests > 0
                    ? Scheme.ErrorColor
                    : ignoredTests > 0
                        ? Scheme.WarningColor
                        : notRunTests > 0
                        ? Scheme.NotifyColor
                        : Scheme.SuccessColor
                        , outcome.ToString().PadLeft(totalTimeString.Length));

            Console.WriteLine();
            Console.WriteLine(new string('=', SeparatorSize.Value));
            Console.WriteLine();

            return Task.CompletedTask;
        }

        public static Task Report(TestCase test)
            => test.TestOutcome switch
            {
                TestOutcome.Success => Success(test),
                TestOutcome.NotRun => NotRun(test),
                TestOutcome.Ignored => Ignored(test),
                TestOutcome.Failed => Failed(test),
                _ => throw new NotImplementedException($"{nameof(ConsoleReporter)}.{nameof(Report)}.{nameof(TestOutcome)}={test.TestOutcome}")
            };

        static string GetTestName(TestCase test)
            => test.FullName;

        private static Task Success(TestCase test)
        {
            WriteLine(Scheme.SuccessColor, $"{Scheme.SuccessIcon} {Duration(test)} {GetTestName(test)}");
            if (!string.IsNullOrEmpty(test.AdditionalMessage))
            {
                WriteLine(Scheme.SuccessColor, $"\t{test.AdditionalMessage}");
            }
            return Task.CompletedTask;
        }

        private static Task NotRun(TestCase test)
        {
            WriteLine(Scheme.NotifyColor, $"{Scheme.NotRunIcon} {Duration(test)} {GetTestName(test)}");
            return Task.CompletedTask;
        }

        private static Task Ignored(TestCase test)
        {
            WriteLine(Scheme.WarningColor, $"{Scheme.IgnoredIcon} {Duration(test)} {GetTestName(test)}");
            if (!string.IsNullOrEmpty(test.IgnoredReason))
            {
                WriteLine(Scheme.WarningColor, $"\t{test.IgnoredReason}");
            }
            return Task.CompletedTask;
        }

        private static Task Failed(TestCase test)
        {
            WriteLine(Scheme.ErrorColor, $"{Scheme.ErrorIcon} {Duration(test)} {GetTestName(test)}");
            if (test.Exception != null)
            {
                WriteLine(Scheme.ErrorColor, $"\t{test.Exception}");
            }
            if (!string.IsNullOrEmpty(test.AdditionalMessage))
            {
                WriteLine(Scheme.ErrorColor, $"\t{test.AdditionalMessage}");
            }
            return Task.CompletedTask;
        }

        private static string Duration(TestCase test)
            => test.Duration.AsDuration();

        private static void WriteLine(ConsoleColor color, string formattableString)
            => Finally(() =>
            {
                Console.ForegroundColor = color;
                Console.WriteLine(formattableString);
            }, ResetColor);

        private static void Write(ConsoleColor color, string formattableString)
            => Finally(() =>
            {
                Console.ForegroundColor = color;
                Console.Write(formattableString);
            }, ResetColor);

        private static void ResetColor()
            => Console.ResetColor();
    }
}
