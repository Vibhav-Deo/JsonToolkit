using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Tracks property paths during JSON serialization and deserialization operations.
    /// Provides a stack-based approach to building property paths for error reporting.
    /// </summary>
    public class PropertyPathTracker
    {
        private readonly Stack<string> _pathSegments = new();
        private readonly StringBuilder _pathBuilder = new();

        /// <summary>
        /// Gets the current property path.
        /// </summary>
        public string CurrentPath => BuildPath();

        /// <summary>
        /// Gets the depth of the current path.
        /// </summary>
        public int Depth => _pathSegments.Count;

        /// <summary>
        /// Pushes a property name onto the path stack.
        /// </summary>
        /// <param name="propertyName">The property name to add.</param>
        /// <returns>A disposable that will pop the property when disposed.</returns>
        public IDisposable PushProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            _pathSegments.Push(propertyName);
            return new PathSegmentScope(this);
        }

        /// <summary>
        /// Pushes an array index onto the path stack.
        /// </summary>
        /// <param name="index">The array index to add.</param>
        /// <returns>A disposable that will pop the index when disposed.</returns>
        public IDisposable PushIndex(int index)
        {
            if (index < 0)
                throw new ArgumentException("Array index cannot be negative.", nameof(index));

            _pathSegments.Push($"[{index}]");
            return new PathSegmentScope(this);
        }

        /// <summary>
        /// Pushes a dictionary key onto the path stack.
        /// </summary>
        /// <param name="key">The dictionary key to add.</param>
        /// <returns>A disposable that will pop the key when disposed.</returns>
        public IDisposable PushKey(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            // Escape the key if it contains special characters
            var escapedKey = EscapeKey(key);
            _pathSegments.Push($"['{escapedKey}']");
            return new PathSegmentScope(this);
        }

        /// <summary>
        /// Pops the most recent path segment.
        /// </summary>
        internal void Pop()
        {
            if (_pathSegments.Count > 0)
                _pathSegments.Pop();
        }

        /// <summary>
        /// Clears all path segments.
        /// </summary>
        public void Clear()
        {
            _pathSegments.Clear();
        }

        /// <summary>
        /// Creates a snapshot of the current path.
        /// </summary>
        /// <returns>The current path as a string.</returns>
        public string CreateSnapshot()
        {
            return BuildPath();
        }

        /// <summary>
        /// Builds the current path from the stack segments.
        /// </summary>
        /// <returns>The formatted property path.</returns>
        private string BuildPath()
        {
            if (_pathSegments.Count == 0)
                return "$"; // Root path

            _pathBuilder.Clear();
            _pathBuilder.Append("$");

            // Reverse the stack to get the correct order
            var segments = _pathSegments.Reverse().ToArray();
            
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                
                if (segment.StartsWith("["))
                {
                    // Array index or dictionary key - append directly
                    _pathBuilder.Append(segment);
                }
                else
                {
                    // Property name - add dot separator
                    _pathBuilder.Append('.');
                    _pathBuilder.Append(segment);
                }
            }

            return _pathBuilder.ToString();
        }

        /// <summary>
        /// Escapes special characters in dictionary keys.
        /// </summary>
        /// <param name="key">The key to escape.</param>
        /// <returns>The escaped key.</returns>
        private static string EscapeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;

            // Escape single quotes by doubling them
            return key.Replace("'", "''");
        }

        /// <summary>
        /// Disposable scope for managing path segments.
        /// </summary>
        private class PathSegmentScope : IDisposable
        {
            private readonly PropertyPathTracker _tracker;
            private bool _disposed;

            public PathSegmentScope(PropertyPathTracker tracker)
            {
                _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _tracker.Pop();
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for PropertyPathTracker to provide convenient usage patterns.
    /// </summary>
    public static class PropertyPathTrackerExtensions
    {
        /// <summary>
        /// Executes an action within a property scope.
        /// </summary>
        /// <param name="tracker">The path tracker.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="action">The action to execute.</param>
        public static void WithProperty(this PropertyPathTracker tracker, string propertyName, Action action)
        {
            using (tracker.PushProperty(propertyName))
            {
                action();
            }
        }

        /// <summary>
        /// Executes a function within a property scope.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="tracker">The path tracker.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="func">The function to execute.</param>
        /// <returns>The result of the function.</returns>
        public static T WithProperty<T>(this PropertyPathTracker tracker, string propertyName, Func<T> func)
        {
            using (tracker.PushProperty(propertyName))
            {
                return func();
            }
        }

        /// <summary>
        /// Executes an action within an array index scope.
        /// </summary>
        /// <param name="tracker">The path tracker.</param>
        /// <param name="index">The array index.</param>
        /// <param name="action">The action to execute.</param>
        public static void WithIndex(this PropertyPathTracker tracker, int index, Action action)
        {
            using (tracker.PushIndex(index))
            {
                action();
            }
        }

        /// <summary>
        /// Executes a function within an array index scope.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="tracker">The path tracker.</param>
        /// <param name="index">The array index.</param>
        /// <param name="func">The function to execute.</param>
        /// <returns>The result of the function.</returns>
        public static T WithIndex<T>(this PropertyPathTracker tracker, int index, Func<T> func)
        {
            using (tracker.PushIndex(index))
            {
                return func();
            }
        }

        /// <summary>
        /// Executes an action within a dictionary key scope.
        /// </summary>
        /// <param name="tracker">The path tracker.</param>
        /// <param name="key">The dictionary key.</param>
        /// <param name="action">The action to execute.</param>
        public static void WithKey(this PropertyPathTracker tracker, string key, Action action)
        {
            using (tracker.PushKey(key))
            {
                action();
            }
        }

        /// <summary>
        /// Executes a function within a dictionary key scope.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="tracker">The path tracker.</param>
        /// <param name="key">The dictionary key.</param>
        /// <param name="func">The function to execute.</param>
        /// <returns>The result of the function.</returns>
        public static T WithKey<T>(this PropertyPathTracker tracker, string key, Func<T> func)
        {
            using (tracker.PushKey(key))
            {
                return func();
            }
        }
    }
}