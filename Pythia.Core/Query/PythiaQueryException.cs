using System;
using System.Runtime.Serialization;

namespace Pythia.Core.Query;

/// <summary>
/// Pythia query exception.
/// </summary>
[Serializable]
public class PythiaQueryException  : Exception
{
    /// <summary>
    /// Gets or sets the query text's line number.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the query text's column number.
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the query text's index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the query text's length.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PythiaQueryException"/>
    /// class.
    /// </summary>
    public PythiaQueryException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PythiaQueryException"/>
    /// class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PythiaQueryException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PythiaQueryException"/>
    /// class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="inner">The inner.</param>
    public PythiaQueryException(string message, Exception inner) :
        base(message, inner) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PythiaQueryException"/>
    /// class.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"></see> that
    /// holds the serialized object data about the exception being thrown.
    /// </param>
    /// <param name="context">The <see cref="StreamingContext"></see> that
    /// contains contextual information about the source or destination.</param>
    protected PythiaQueryException(
      SerializationInfo info,
      StreamingContext context) : base(info, context) { }
}