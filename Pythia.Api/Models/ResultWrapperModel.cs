namespace Pythia.Api.Models
{
    /// <summary>
    /// A wrapper for a result value or an error message.
    /// </summary>
    /// <typeparam name="T">The type of wrapped value.</typeparam>
    public class ResultWrapperModel<T>
    {
        /// <summary>
        /// The error. Null if success.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The wrapped value. Null if error.
        /// </summary>
        public T Value { get; set; }
    }
}
