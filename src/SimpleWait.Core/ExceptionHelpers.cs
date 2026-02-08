using System;

namespace SimpleWait.Core
{
    /// <summary>
    /// Internal helpers for creating and detecting timeout-related exceptions
    /// used by RetryPolicy implementations.
    /// </summary>
    internal static class ExceptionHelpers
    {
        private static readonly Type DefaultException = typeof(TimeoutException);

        /// <summary>
        /// Returns true when <paramref name="ex"/> is a <see cref="TimeoutException"/>
        /// or is assignable to the configured exception type.
        /// </summary>
        internal static bool IsTimeoutOrConfiguredTimeoutException(Exception ex, Type configuredExceptionType)
        {
            if (ex is TimeoutException) return true;
            if (configuredExceptionType != DefaultException)
            {
                return configuredExceptionType.IsAssignableFrom(ex.GetType());
            }
            return false;
        }

        /// <summary>
        /// Throws the configured exception type (preserving inner exception when possible)
        /// or rethrows a <see cref="TimeoutException"/> when the default is configured.
        /// </summary>
        internal static void ThrowConfiguredOrDefault(Type configuredExceptionType, TimeoutException timeoutEx, string overrideMessage = null)
        {
            var message = overrideMessage ?? timeoutEx.Message;

            if (configuredExceptionType != DefaultException)
            {
                Exception created = null;

                // Try (string, Exception)
                try
                {
                    created = (Exception)Activator.CreateInstance(configuredExceptionType, message, timeoutEx);
                }
                catch { /* ignore and try other ctors */ }

                // Try (string)
                if (created == null)
                {
                    try
                    {
                        created = (Exception)Activator.CreateInstance(configuredExceptionType, message);
                    }
                    catch { }
                }

                // Try parameterless
                if (created == null)
                {
                    try
                    {
                        created = (Exception)Activator.CreateInstance(configuredExceptionType);
                    }
                    catch { }
                }

                if (created != null)
                {
                    throw created;
                }

                // Couldn't construct configured exception -> fail loudly
                throw new InvalidOperationException($"Failed to create exception of type {configuredExceptionType.FullName}", timeoutEx);
            }
            else
            {
                throw new TimeoutException(message, timeoutEx);
            }
        }
    }
}