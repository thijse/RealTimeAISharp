using System.Diagnostics;

namespace RealtimeInteractiveConsole.Utilities
{
    /// <summary>
    /// Custom output handler that writes to a file
    /// </summary>

    /* Examples

     // Adding the custom handler
        var fileHandler = new FileOutputHandler("log.txt");
        Output.AddHandler(fileHandler);

        // Using Output class
        Output.WriteLine("This will be written to console and file.");

        // Removing the custom handler
        Output.RemoveHandler(fileHandler);


     // Using the default handler
        Output.WriteLine("This will be written to Output.");

     // Adding a handler with two callback actions
        Output.AddHandler(new OutputHandler(
           Output.Write,
           Output.WriteLine
       ));

     // Clearing all handlers
        Output.ClearHandlers();
        Output.WriteLine("This will use the default console handler.");

     // Using an instance of OutputInstance
        var outputInstance = new OutputInstance();
        outputInstance.AddHandler(new FileOutputHandler("instance_log.txt"));
        outputInstance.WriteLine("This will be written to instance_log.txt.");

     // Adding a debug handler
        var debugHandler = new DebugOutputHandler();
        Output.AddHandler(debugHandler);
        Output.WriteLine("This will be written to the debug output.");
    */



    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    namespace RealtimeInteractiveConsole.Utilities
    {
        // IOutputHandler Interface
        public interface IOutputHandler
        {
            void Write(string? message);
            void WriteLine(string? message);
        }

        // OutputHandler Class
        public class OutputHandler : IOutputHandler
        {
            private readonly Action<string?> _write;
            private readonly Action<string?> _writeLine;

            public OutputHandler(Action<string?> write)
            {
                _write = write ?? throw new ArgumentNullException(nameof(write));
                _writeLine = message => _write(message + Environment.NewLine);
            }

            public OutputHandler(Action<string?> write, Action<string?> writeLine)
            {
                _write = write ?? throw new ArgumentNullException(nameof(write));
                _writeLine = writeLine ?? throw new ArgumentNullException(nameof(writeLine));
            }

            public void Write(string? message) => _write(message);
            public void WriteLine(string? message) => _writeLine(message);
        }

        // FileOutputHandler Class
        public class FileOutputHandler : IOutputHandler
        {
            private readonly string _filePath;

            public FileOutputHandler(string filePath)
            {
                _filePath = filePath;
            }

            public void Write(string? message) => File.AppendAllText(_filePath, message);
            public void WriteLine(string? message) => File.AppendAllText(_filePath, message + Environment.NewLine);
        }

        // ConsoleOutputHandler Class
        public class ConsoleOutputHandler : IOutputHandler
        {
            public void Write(string? message) => Console.Write(message);
            public void WriteLine(string? message) => Console.WriteLine(message);
        }

        // DebugOutputHandler Class
        public class DebugOutputHandler : IOutputHandler
        {
            public void Write(string? message) => Debug.Write(message);
            public void WriteLine(string? message) => Debug.WriteLine(message);
        }

        // OutputInstance Class
        public class OutputInstance
        {
            private readonly List<IOutputHandler> _handlers = new List<IOutputHandler>();
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

            public void AddHandler(IOutputHandler handler)
            {
                if (handler == null) throw new ArgumentNullException(nameof(handler));

                _lock.EnterWriteLock();
                try
                {
                    _handlers.Add(handler);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void RemoveHandler(IOutputHandler handler)
            {
                if (handler == null) throw new ArgumentNullException(nameof(handler));

                _lock.EnterWriteLock();
                try
                {
                    _handlers.Remove(handler);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void ClearHandlers()
            {
                _lock.EnterWriteLock();
                try
                {
                    _handlers.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void Write(string? message)
            {
                EnsureDefaultHandler();

                _lock.EnterReadLock();
                try
                {
                    foreach (var handler in _handlers)
                    {
                        handler.Write(message);
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            public void WriteLine()
            {
                WriteLine(String.Empty);
            }

            public void WriteLine(string? message)
            {
                EnsureDefaultHandler();

                _lock.EnterReadLock();
                try
                {
                    foreach (var handler in _handlers)
                    {
                        handler.WriteLine(message);
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            private void EnsureDefaultHandler()
            {
                _lock.EnterWriteLock();
                try
                {
                    if (_handlers.Count == 0)
                    {
                        _handlers.Add(new ConsoleOutputHandler());
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        // Static Output Class
        public static class Output
        {
            private static readonly OutputInstance _instance = new OutputInstance();

            public static void AddHandler(IOutputHandler handler) => _instance.AddHandler(handler);
            public static void RemoveHandler(IOutputHandler handler) => _instance.RemoveHandler(handler);
            public static void ClearHandlers() => _instance.ClearHandlers();
            public static void Write(string? message) => _instance.Write(message);
            public static void WriteLine(string? message) => _instance.WriteLine(message);
            public static void WriteLine() => _instance.WriteLine();
        }
    }
}