// Copyright 2013-2015 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using SourceGeneratorAttributes;
using System.Diagnostics;

namespace Serilog;

/// <summary>
/// The core Serilog logging API, used for writing log events.
/// </summary>
/// <example><code>
/// var log = new LoggerConfiguration()
///     .WriteTo.Console()
///     .CreateLogger();
///
/// var thing = "World";
/// log.Information("Hello, {Thing}!", thing);
/// </code></example>
/// <remarks>
/// The methods on <see cref="ILogger"/> (and its static sibling <see cref="Log"/>) are guaranteed
/// never to throw exceptions. Methods on all other types may.
/// </remarks>
[LoggerGenerate(
    genericOverrideCount: 3,
    $"{nameof(LogEventLevel.Verbose)},{nameof(LogEventLevel.Debug)},{nameof(LogEventLevel.Information)},{nameof(LogEventLevel.Warning)},{nameof(LogEventLevel.Error)},{nameof(LogEventLevel.Fatal)}"
)]
public partial interface ILogger
{
#if FEATURE_DEFAULT_INTERFACE
    private static readonly object[] NoPropertyValues = Array.Empty<object>();
    private static readonly Logger DefaultLoggerImpl = new LoggerConfiguration().CreateLogger();
#endif

    /// <summary>
    /// Create a logger that enriches log events via the provided enrichers.
    /// </summary>
    /// <param name="enricher">Enricher that applies in the context.</param>
    /// <returns>A logger that will enrich log events as specified.</returns>
#if FEATURE_DEFAULT_INTERFACE
    [CustomDefaultMethodImplementation]
#endif
    ILogger ForContext(ILogEventEnricher enricher)
#if FEATURE_DEFAULT_INTERFACE
        => new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Logger(this)
            .CreateLogger()
            .ForContext(enricher)
#endif
    ;

    /// <summary>
    /// Create a logger that enriches log events via the provided enrichers.
    /// </summary>
    /// <param name="enrichers">Enrichers that apply in the context.</param>
    /// <returns>A logger that will enrich log events as specified.</returns>
    ILogger ForContext(IEnumerable<ILogEventEnricher> enrichers)
#if FEATURE_DEFAULT_INTERFACE
    {
        if (enrichers == null!)
            return this; // No context here, so little point writing to SelfLog.

        return ForContext(new SafeAggregateEnricher(enrichers));
    }
#else
        ;
#endif

    /// <summary>
    /// Create a logger that enriches log events with the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property. Must be non-empty.</param>
    /// <param name="value">The property value.</param>
    /// <param name="destructureObjects">If <see langword="true"/>, the value will be serialized as a structured
    /// object if possible; if <see langword="false"/>, the object will be recorded as a scalar or simple array.</param>
    /// <returns>A logger that will enrich log events as specified.</returns>
#if FEATURE_DEFAULT_INTERFACE
    [CustomDefaultMethodImplementation]
#endif
    ILogger ForContext(string propertyName, object? value, bool destructureObjects = false)
#if FEATURE_DEFAULT_INTERFACE
        => new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Logger(this)
            .CreateLogger()
            .ForContext(propertyName, value, destructureObjects)
#endif
    ;

    /// <summary>
    /// Create a logger that marks log events as being from the specified
    /// source type.
    /// </summary>
    /// <typeparam name="TSource">Type generating log messages in the context.</typeparam>
    /// <returns>A logger that will enrich log events as specified.</returns>
    ILogger ForContext<TSource>()
#if FEATURE_DEFAULT_INTERFACE
        => ForContext(typeof(TSource))
#endif
    ;

    /// <summary>
    /// Create a logger that marks log events as being from the specified
    /// source type.
    /// </summary>
    /// <param name="source">Type generating log messages in the context.</param>
    /// <returns>A logger that will enrich log events as specified.</returns>
    ILogger ForContext(Type source)
#if FEATURE_DEFAULT_INTERFACE
    {
        if (source == null!)
            return this; // Little point in writing to SelfLog here because we don't have any contextual information

        return ForContext(Constants.SourceContextPropertyName, source.FullName);
    }
#else
        ;
#endif

    /// <summary>
    /// Write an event to the log.
    /// </summary>
    /// <param name="logEvent">The event to write.</param>
    void Write(LogEvent logEvent);

    /// <summary>
    /// Write a log event with the specified level.
    /// </summary>
    /// <param name="level">The level of the event.</param>
    /// <param name="messageTemplate"></param>
    /// <param name="propertyValues"></param>
    [MessageTemplateFormatMethod("messageTemplate")]
    void Write(LogEventLevel level, string messageTemplate, params object?[]? propertyValues)
#if FEATURE_DEFAULT_INTERFACE
        => Write(level, (Exception?)null, messageTemplate, propertyValues)
#endif
    ;

    /// <summary>
    /// Write a log event with the specified level and associated exception.
    /// </summary>
    /// <param name="level">The level of the event.</param>
    /// <param name="exception">Exception related to the event.</param>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
#if FEATURE_DEFAULT_INTERFACE
    [CustomDefaultMethodImplementation]
#endif
    void Write(LogEventLevel level, Exception? exception, string messageTemplate, params object?[]? propertyValues)
#if FEATURE_DEFAULT_INTERFACE
    {
        if (!IsEnabled(level)) return;
        if (messageTemplate == null!) return;

        // Catch a common pitfall when a single non-object array is cast to object[]
        if (propertyValues != null &&
            propertyValues.GetType() != typeof(object[]))
            propertyValues = new object[] { propertyValues };

        var logTimestamp = DateTimeOffset.Now;
        if (BindMessageTemplate(messageTemplate, propertyValues, out var parsedTemplate, out var boundProperties))
        {
            var currentActivity = Activity.Current;
            var logEvent = new LogEvent(logTimestamp, level, exception, parsedTemplate, boundProperties, currentActivity?.TraceId ?? default, currentActivity?.SpanId ?? default);
            Write(logEvent);
        }
    }
#else
        ;
#endif

    /// <summary>
    /// Determine if events at the specified level will be passed through
    /// to the log sinks.
    /// </summary>
    /// <param name="level">Level to check.</param>
    /// <returns><see langword="true"/> if the level is enabled; otherwise, <see langword="false"/>.</returns>
#if FEATURE_DEFAULT_INTERFACE
    [CustomDefaultMethodImplementation]
#endif
    bool IsEnabled(LogEventLevel level)
#if FEATURE_DEFAULT_INTERFACE
        => true
#endif
    ;

    /// <summary>
    /// Uses configured scalar conversion and destructuring rules to bind a set of properties to a
    /// message template. Returns false if the template or values are invalid (<c>ILogger</c>
    /// methods never throw exceptions).
    /// </summary>
    /// <param name="messageTemplate">Message template describing an event.</param>
    /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
    /// <param name="parsedTemplate">The internal representation of the template, which may be used to
    /// render the <paramref name="boundProperties"/> as text.</param>
    /// <param name="boundProperties">Captured properties from the template and <paramref name="propertyValues"/>.</param>
    /// <example><code>
    /// MessageTemplate template;
    /// IEnumerable&lt;LogEventProperty&gt; properties;
    /// if (Log.BindMessageTemplate("Hello, {Name}!", new[] { "World" }, out template, out properties)
    /// {
    ///     var propsByName = properties.ToDictionary(p => p.Name, p => p.Value);
    ///     Console.WriteLine(template.Render(propsByName, null));
    ///     // -> "Hello, World!"
    /// }
    /// </code></example>
    [MessageTemplateFormatMethod("messageTemplate")]
#if FEATURE_DEFAULT_INTERFACE
    [CustomDefaultMethodImplementation]
#endif
    bool BindMessageTemplate(string messageTemplate, object?[]? propertyValues,
        [NotNullWhen(true)] out MessageTemplate? parsedTemplate,
        [NotNullWhen(true)] out IEnumerable<LogEventProperty>? boundProperties)
#if FEATURE_DEFAULT_INTERFACE
        => DefaultLoggerImpl.BindMessageTemplate(messageTemplate, propertyValues, out parsedTemplate, out boundProperties)
#endif
    ;

    /// <summary>
    /// Uses configured scalar conversion and destructuring rules to bind a property value to its captured
    /// representation.
    /// </summary>
    /// <param name="propertyName">The name of the property. Must be non-empty.</param>
    /// <param name="value">The property value.</param>
    /// <param name="destructureObjects">If <see langword="true"/>, the value will be serialized as a structured
    /// object if possible; if <see langword="false"/>, the object will be recorded as a scalar or simple array.</param>
    /// <param name="property">The resulting property.</param>
    /// <returns>True if the property could be bound, otherwise false (<summary>ILogger</summary>
    /// methods never throw exceptions).</returns>
#if FEATURE_DEFAULT_INTERFACE
    [CustomDefaultMethodImplementation]
#endif
    bool BindProperty(string? propertyName, object? value, bool destructureObjects, [NotNullWhen(true)] out LogEventProperty? property)
#if FEATURE_DEFAULT_INTERFACE
        => DefaultLoggerImpl.BindProperty(propertyName, value, destructureObjects, out property)
#endif
    ;
}
